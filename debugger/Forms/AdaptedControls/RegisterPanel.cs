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
                    FormattedValue = ReplaceAtNonZero(Core.FormatNumber(Register.Value, FormatType.Hex), "$").Insert(2, "$").Insert(2,"%").Insert(0, "%");
                }
                else if(RegSize == 1 && RegisterLabels[RegLabelIndex].ShowUpper)
                {
                    ulong TargetValue = inputRegs[Disassembly.DisassembleRegister((XRegCode)RegLabelIndex - 5, (RegisterCapacity)RegSize, REX.NONE)];
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
                    FormattedValue = Core.FormatNumber(Register.Value, FormatType.Hex);
                    if (Register.Value >  (ulong)Math.Pow(2, RegSize*8)-1)
                    {
                        FormattedValue = FormattedValue.Insert(2 + 16 - (RegSize * 2), "%$").Insert(0, "%");
                    }
                    else
                    {
                        FormattedValue = ReplaceAtNonZero(FormattedValue,"$%").Insert(2 + 16 - (RegSize * 2), "$").Insert(0, "%");
                    }
                }
                RegisterLabels[RegLabelIndex].Text = $"{Register.Key.PadRight(5)} : {FormattedValue}";
                RegisterLabels[RegLabelIndex].Update();
                RegLabelIndex++;
            }
        }
        public static string ReplaceAtNonZero(string input, string toInsert)
        {
            string ReadCopy = input;
            for (int i = 1; i < (input.Length / 2); i++)
            {
                if (ReadCopy[i * 2] != '0' || ReadCopy[i * 2 + 1] != '0')
                {
                    input = input.Insert(i * 2, toInsert);
                    break;
                }
            }
            return input;
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
