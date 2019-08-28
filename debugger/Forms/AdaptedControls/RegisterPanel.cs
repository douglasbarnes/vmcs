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
        private readonly RegisterLabel[] RegisterLabels = new RegisterLabel[RegCount]
        {
            new RegisterLabel(noFormat:true),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
            new RegisterLabel(),
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
        
        public int RegSize { get; private set; } = 8;
        public delegate void RegSizeChangedDelegate(int newSize);
        public event RegSizeChangedDelegate OnRegSizeChanged;
        public RegisterPanel() : base(Layer.Imminent, Emphasis.High)
        {
            Size = new Size(368, 400);
            Controls.AddRange(RegisterLabels);
            for (int i = 0; i < RegCount; i++)
            {
                RegisterLabels[i].Location = new Point(4, 30 + i * 20);
                RegisterLabels[i].MouseDoubleClick += (s,a) => NextRegSize();
            }
        }        
        public void UpdateRegisters(Dictionary<string, ulong> inputRegs)
        {
            int RegLabelIndex = 0;
            foreach (var Register in inputRegs)
            {
                string FormattedValue;
                if(RegisterLabels[RegLabelIndex].NoFormat)
                {
                    FormattedValue = Core.FormatNumber(Register.Value, FormatType.Hex).Insert(2, "%").Insert(0, "%");
                }
                else if(RegSize == 1 && RegisterLabels[RegLabelIndex].ShowUpper)
                {
                    FormattedValue = 
                        Core.FormatNumber(inputRegs[Disassembly.DisassembleRegister((XRegCode)RegLabelIndex - 5, (RegisterCapacity)RegSize, REX.NONE)], FormatType.Hex).Insert(16, "%").Insert(14, "%").Insert(0, "%");
                }
                else
                {
                    FormattedValue = Core.FormatNumber(Register.Value, FormatType.Hex).Insert(2 + 16 - (RegSize * 2), "%").Insert(0, "%");
                }
                RegisterLabels[RegLabelIndex].Text = $"{Register.Key.PadRight(5)} : {FormattedValue}";
                RegisterLabels[RegLabelIndex].Update();
                RegLabelIndex++;
            }
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            NextRegSize();
        }
        public void NextRegSize()
        {
            RegSize = RegSize == 1 ? 8 : RegSize / 2;
            OnRegSizeChanged.Invoke(RegSize);
        }

    }
}
