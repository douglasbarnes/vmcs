// RegisterPanel provides a super easy way to create a panel of registers(hence the name). Given an input dictionary in the UpdateRegisters() method
// will do all the hard work from thereon. The total count of registers is hard coded to 17. All that is required to increase this is to
// add another register label in $RegisterLabels and change the $RegCount variable. This assumes the new register will be parsed the same as one currently.
// The class uses a $RegSize variable to manage how registers are displayed. This creates a useful quality of life feature for the user that
// I call "Capacity emphasis". When debugging, there is an abundance of information available. Whilst this can be very useful, sometimes it can be
// overwheling and confusing. For example, take x32dbg, http://prntscr.com/pk8kep . With the significant amount of experience I have using debuggers,
// I have never known what that information represents or needed to make use of it. Many concepts are used throughout the user interface to mitigate
// this happening. It could be seen as a user level abstraction. In the context of this class, capacity emphasis is used to highlight and de-emphasise
// certain information about displayed registers.
// For example, 
//  - Trailing zeros are demphasised in the output in the direction of the MSB.
//  - As a result of the above, empty registers are demphasised
//  - The conventional prefix for a hexadecimal represented number 0x is de-emphasised. It is still necessary to know that a number is in hex,
//    however it probably only needs to be seen once or twice before it becomes obvious to the user. Also, in the context of an already-experienced
//    user, this would be a given, so this is a happy medium.
//  - Register capacities that the current register could not hold are de-emphasised. E.g, when looking at $ECX, the bytes that are exclusive to 
//    $RAX would not need to be seen. The user would be able to interpret the value of this number as it is easy to get confused when looking at
//    a screen full of numbers. (This is better explained through the upcoming demonstration)
// Here are some annotated examples, http://prntscr.com/pk8trk http://prntscr.com/pk94n3
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using debugger.Emulator;
using debugger.Util;
namespace debugger.Forms
{
    public class RegisterPanel : BorderedPanel
    {
        private const int RegCount = 17;

        // There is no prettier way to initialise the array than this.
        private readonly RegisterLabel[] RegisterLabels = new RegisterLabel[RegCount]
        {
            // Keep RIP as-is i.e no extra formatting except for trailing zeros.
            new RegisterLabel(noFormat:true),

            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),

            // These will become AH,CH etc when of byte size.
            new RegisterLabel(showUpper:true),
            new RegisterLabel(showUpper:true),
            new RegisterLabel(showUpper:true),
            new RegisterLabel(showUpper:true),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel()
        };
        
        // $RegSize dictates the size of register to be displayed
        public int RegSize { get; private set; } = 8;
        public delegate void RegSizeChangedDelegate(int newSize);
        public event RegSizeChangedDelegate OnRegSizeChanged;
        public RegisterPanel() : base(Layer.Imminent, Emphasis.High)
        {
            // The size is hard coded to a known working size. In theory any size greater than that of the contained controls should suffice.
            Size = new Size(368, 400);

            Controls.AddRange(RegisterLabels);

            for (int i = 0; i < RegCount; i++)
            {
                // All registers have an X coordinate of 4. The Y coordinates follow an arithmetic series, such that each is initially offset by
                // 30 pixels downwards, then 20 more for each register before it. The default size for a register label is 15, so a small amount of
                // extra space is left.
                RegisterLabels[i].Location = new Point(4, 30 + i * 20);

                // Add this event to all registers. This is because the on click events for the panel will not be called if the click was on a child control.
                // The intention is to have any double click in the area of the panel to call NextRegSize().
                RegisterLabels[i].MouseDoubleClick += (s,a) => NextRegSize();
            }
        }        
        public void UpdateRegisters(Dictionary<string, ulong> inputRegs)
        {
            // A necessary precondition of the input is that the keys were added in order of register, RIP being the first.
            // This is because foreach() uses this order.

            // For indexing the next register in $RegisterLabels
            int RegLabelIndex = 0;

            foreach (var Register in inputRegs)
            {                
                // A string that will hold the value to set the register's text to.
                string FormattedValue;

                
                if(RegSize == 1 && RegisterLabels[RegLabelIndex].ShowUpper)
                {
                    ulong TargetValue = inputRegs[new ControlUnit.RegisterHandle((XRegCode)RegLabelIndex - 5, RegisterTable.GP, (RegisterCapacity)RegSize).DisassembleOnce()];
                    FormattedValue = Core.FormatNumber(TargetValue, FormatType.Hex);
                    if (((ushort)TargetValue >> 8) > 0) { // get lower short, then rsh to remove lower byte
                        FormattedValue = FormattedValue.Insert(16, "%").Insert(14, "%").Insert(0, "%"); 
                    }
                    else
                    {
                        FormattedValue = FormattedValue.Insert(16, "$").Insert(14, "$").Insert(0, "%");
                    }
                }
                else
                {
                    // Format the ulong value of the register into a hexadecimal string representation.
                    FormattedValue = Core.FormatNumber(Register.Value, FormatType.Hex);

                    // .Insert(2,"%").Insert(0,"%") is the process of greatly de-emphasising the "0x".
                    // .Insert(2,"$") will make every character after the 0x de-emphasised.
                    // ReplaceAtNonZero(,"$") will insert a "$" at the first non "00" series of characters, ending the above markdown block. Hereon is normally emphasised.

                    // If the register has $NoFormat true(RIP in practice),
                    if (RegisterLabels[RegLabelIndex].NoFormat)
                    {
                        // Start at offset 2 to ignore the "0x"
                        FormattedValue = Drawing.InsertAtNonZero(FormattedValue, "$", 2).Insert(2, "$").Insert(2, "%").Insert(0, "%");
                    }

                    // Otherwise format as normal
                    else
                    {
                        // If more bytes are present than the current reg size would hold, cut the emphasis to the length of $RegSize*2;(because 2 characters per byte in hex)
                        if (Register.Value > (ulong)Math.Pow(2, RegSize * 8) - 1)
                        {
                            FormattedValue = FormattedValue.Insert(2 + 16 - (RegSize * 2), "%$").Insert(0, "%");
                        }
                        else
                        {
                            // Start at offset 2 to ignore the "0x"
                            FormattedValue = Drawing.InsertAtNonZero(FormattedValue, "$%", 2).Insert(2 + 16 - (RegSize * 2), "$").Insert(0, "%");
                        }
                    }                    
                }

                // Pad the mnemonic such that all registers are visually in line with each other.
                RegisterLabels[RegLabelIndex].Text = $"{Register.Key.PadRight(5)} : {FormattedValue}";

                // Increment to the next $RegisterLabels index.
                RegLabelIndex++;
            }
        }
        
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            NextRegSize();
        }
        public void NextRegSize()
        {
            // If the register size is one, loop back round to 8. Otherwise half it. This creates a looped series of powers of two.
            // E.g, 8 -> 4 -> 2 -> 1 -> 8 -> 4 ...
            // These are the values of register sizes used by the program.
            RegSize = RegSize == 1 ? 8 : RegSize / 2;

            // Raise the event. This will likely call UpdateRegisters() with the newly sized registers.
            OnRegSizeChanged.Invoke(RegSize);
        }

    }
}
