// MainForm is the centerpiece of the program that pulls all aspects together. Without this class you only have modules.
// It mostly contains procedures/routines that ensure the order of certain methods. For example, FlashProcedure() will
// take a byte array of instructions, but the byte array must be turned into a MemorySpace first, and afterwards breakpoints
// must be reset. By using these routines it is certain that there will be no problems with the order of executed. There are
// also necessary tasks that must be performed after the VM finishes execution. To allow this to work asynchrously, events and
// callbacks are used to handle the completion of another thread.
using debugger.Emulator;
using debugger.Forms;
using debugger.Hypervisor;
using debugger.IO;
using debugger.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
namespace debugger
{
    public partial class MainForm : Form
    {
        private VM VMInstance;
        private MemorySpace ROM;
        private Disassembler DisassemblerInstance;

        public MainForm()
        {
            // Beware removing this line. Very odd things will happen to the layout.
            Font = FormSettings.BaseUI.BaseFont;

            // These initialisations must be kept before InitialiseCustom() as the nested methods called there make
            // use of these two instances.
            VMInstance = new VM();
            DisassemblerInstance = new Disassembler(VMInstance);

            // Draw the form.
            SuspendLayout();
            InitializeComponent();
            InitialiseCustom();
            ResumeLayout();

            // Window title.
            Text = "vmcs";

            // A nice little debug feature when making forms. The coordinates of every click on the form will be
            // written to the output somewhere(debugger dependent).
#if DEBUG
            MouseDoubleClick += (s, e) => Trace.WriteLine($"X: {e.X} Y: {e.Y}");
#endif

            // I let windows forms draw the form rather than draw it myself. All that is really required here is a rectangle.
            ForeColor = FormSettings.BaseUI.SurfaceColour;
            BackColor = FormSettings.BaseUI.BackgroundColour;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // When the VM finishes running, refresh.
            VMInstance.RunComplete += (context) => RefreshCallback();
        }
        private void ReflashVM()
        {
            // Flash the new ROM.
            VMInstance.FlashMemory(ROM);

            // Refresh to show the new memory.
            RefreshCallback();
        }
        private void FlashFromFile(string path)
        {
            // Try to open the file pointed to by the path. This could fail for many reasons, most likely
            // because of a typo in a path name.
            FileParser parser;
            try
            {
                parser = new FileParser(new FileInfo(path));
            }
            catch (ArgumentException)
            {
                Logger.Log(LogCode.IO_INVALIDFILE, "Bad path");
                return;
            }


            byte[] Instructions;

            // Opening a file will by default use auto parse mode.
            if (parser.Parse(ParseMode.AUTO, out Instructions) != ParseResult.SUCCESS)
            {
                // If the file could not be parsed automatically, show some options to the user about how they want to go about this.
                //  1. Parse it as a BIN file
                //  2. Parse it as a TXT file.
                //  3. Cancel
                switch (MessageBox.Show("Press yes to parse as a BIN file, no to parse as a TXT file, or cancel to go back to the program.", "File type cannot be inferred", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        parser.Parse(ParseMode.BIN, out Instructions);
                        break;
                    case DialogResult.No:
                        if (parser.Parse(ParseMode.TXT, out Instructions) != ParseResult.SUCCESS)
                        {
                            // This is already handled in the TXT class, but don't commit the changes to the VM if it was unsuccessful.
                            return;
                        }
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }

            // Flash the successfully obtained instructions. The method would have returned early if this was not possible.
            FlashProcedure(Instructions);
        }
        public void FlashProcedure(byte[] Instructions)
        {
            // Create a new ROM with the instructions.            
            ROM = new MemorySpace(Instructions);

            // Reflash the VM
            ReflashVM();

            // Reset the breakpoints because the old ones will be useless in a new program. They
            // will likely even point to non existent addresses.
            VMInstance.Breakpoints.Clear();

        }
        private void VMContinue_ButtonEvent(object sender, EventArgs e)
        {
            // Check if the VMInstance has been flashed yet.
            if (VMInstance.Ready)
            {
                // Check the text of the sender to see if it Step and tell continue to step if so.
                VMContinue(((Control)sender).Text == "Step");
            }
        }
        private void VMContinue(bool Step)
        {
            // Run the VM asynchrously. The callback set up in the constructor will handle the result
            // once execution has finished.
            VMInstance.RunAsync(Step);
        }
        private void RefreshRegisters(int size)
        {
            // Use the VM instance to fetch the registers, then apply these to PanelRegisters.
            PanelRegisters.Invoke(new Action(() => PanelRegisters.UpdateRegisters(VMInstance.GetRegisters((RegisterCapacity)size))));
        }
        private void RefreshFlags()
        {
            // Fetch flags from the VM and apply them to PanelFlags.
            PanelFlags.Invoke(new Action(() => PanelFlags.UpdateFlags(VMInstance.GetFlags())));
        }
        private void RefreshMemory()
        {
            // Load the memory into the memory list view.
            MemoryViewer.LoadMemory(VMInstance.GetMemory());
        }

        private void RefreshCallback()
        {
            // All the necessary tasks that need to be done in order to update the user interface with updated information
            // after the VM has finished running.  The order of these does not matter, as such they are run asynchrously.
            // The disassembly list view will handle the disassembly on its own.
            List<Task> RefreshTasks = new List<Task>
            {
                new Task(() => RefreshRegisters(PanelRegisters.RegSize)),
                new Task(() => RefreshMemory()),
                new Task(() => RefreshFlags())
            };

            // Start all the tasks concurrently.
            RefreshTasks.ForEach(x => x.Start());
        }
        private const string ResultOutputPath = "Results\\";
        private void OnTestcaseSelected(string name)
        {
            // Create the path if it does not exist
            if (!Directory.Exists(ResultOutputPath))
            {
                Directory.CreateDirectory(ResultOutputPath);
            }

            XElement Result;

            // The All testcases feature needs to be handled a little differently.
            if (name == "all")
            {
                // Running testcases can be a processor intensive job, so it is best done concurrently such that the
                // user can still interact with the ui.
                Task.Run(async () =>
                {
                    // Run the testcases.
                    Result = await TestHandler.ExecuteAll();

                    // Add AllTestcases.xml on to the end of the path and use XElement to save the output xml file to the path.
                    Result.Save(ResultOutputPath + "AllTestcases.xml");

                    // Tell the user where the testcases were written to and set the message box title to whether the testcases passed or not
                    // (The value of result will be Passed or Failed).
                    MessageBox.Show("Results written to " + ResultOutputPath + "AllTestcases.xml", Result.Attribute("result").Value);
                });
            }
            else
            {
                // See above
                Task.Run(async () =>
                {
                    // Execute the testcase by the given name.
                    Result = await TestHandler.ExecuteTestcase(name);

                    // Add the testcase name + Testcase.xml on to the end of the path and use XElement to save the output xml file to the path.
                    Result.Save(ResultOutputPath + name + "Testcase.xml");

                    // Set the message box title to the result of the testcase("Passed" or "Failed") and give them the option to see the exact result of the
                    // testcase in a window rather than having to open up the file themselves.
                    if (MessageBox.Show($"Click Yes to see full results", name + " " + Result.Attribute("result").Value.ToString().ToLower(), MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        MessageBox.Show(Result.ToString());
                    }
                });
            }

        }
        private void Reset_Click(object sender, EventArgs e) => ReflashVM();
    }
}
