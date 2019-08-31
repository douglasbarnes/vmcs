using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public enum StringOpSettings
    {
        NONE=0,
        BYTEMODE=1,
        COMPARE=2,
        A_SRC=4,
        A_DEST=8,
    }
    public abstract class StringOperation : IMyOpcode
    {
        private string Mnemonic;
        private ulong SrcPtr;
        private ulong DestPtr;
        private RegisterCapacity PtrSize; // for in the rare rare rare case that esi/edi could carry out
        protected RegisterCapacity Capacity; 
        protected StringOpSettings Settings;
        
        public StringOperation(string mnemonic, StringOpSettings settings)
        {            
            Settings = settings;
            if ((Settings | StringOpSettings.BYTEMODE) == Settings)
            {
                Capacity = RegisterCapacity.GP_BYTE;
                mnemonic += 'B';
            }
            else if ((ControlUnit.RexByte | REX.W) == ControlUnit.RexByte)
            {
                Capacity = RegisterCapacity.GP_QWORD;
                mnemonic += 'W';
            }
            else if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR))
            {
                Capacity = RegisterCapacity.GP_WORD;
                mnemonic += 'D';
            }
            else
            {
                Capacity = RegisterCapacity.GP_DWORD;
                mnemonic += 'Q';
            }
            Mnemonic = mnemonic;
            PtrSize = ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.GP_DWORD : RegisterCapacity.GP_QWORD;
            SrcPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), 8), 0);
            DestPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), 8), 0);
            OnInitialise();
        }
        public void AdjustDI(RegisterCapacity size)
        {
            byte[] NewSI;
            byte[] Constant = new byte[] { (byte)size };
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                DestPtr -= (byte)size;
                Bitwise.Subtract(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), Constant, (int)size, out NewSI);
            }
            else
            {
                DestPtr += (byte)size;
                Bitwise.Add(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), Constant, (int)size, out NewSI);                
            }
            ControlUnit.SetRegister(XRegCode.DI, NewSI);
        }
        public void AdjustSI(RegisterCapacity size)
        {
            byte[] NewSI;
            byte[] Constant = new byte[] { (byte)size };
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                SrcPtr -= (byte)size;
                Bitwise.Subtract(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), Constant, (int)size, out NewSI);
            }
            else
            {
                SrcPtr += (byte)size;
                Bitwise.Add(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), Constant, (int)size, out NewSI);                
            }
            ControlUnit.SetRegister(XRegCode.SI, NewSI);
        }
        public virtual List<string> Disassemble()
        {
            
            List<string> Output = new List<string>(3) { Mnemonic };
            if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ))
            {
                if((Settings | StringOpSettings.COMPARE) == Settings)
                {
                    Output[0] = Mnemonic.Insert(0, "REPZ ");
                }
                else
                {
                    Output[0] = Mnemonic.Insert(0, "REP ");
                }
            }
            if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ) && (Settings | StringOpSettings.COMPARE) == Settings)
            {
                Output[0] = Mnemonic.Insert(0, "REPNZ ");
            }
            if ((Settings | StringOpSettings.A_DEST) == Settings)
            {
                Output.Add($"{Disassembly.DisassembleRegister(XRegCode.A, Capacity, REX.NONE)}");
            }
            else
            {
                Output.Add($"[{Disassembly.DisassembleRegister(XRegCode.DI, PtrSize, REX.NONE)}]");
            }
            if ((Settings | StringOpSettings.A_SRC) == Settings)
            {
                Output.Add($"{Disassembly.DisassembleRegister(XRegCode.A, Capacity, REX.NONE)}");
            }
            else
            {
                Output.Add($"[{Disassembly.DisassembleRegister(XRegCode.SI, PtrSize, REX.NONE)}]");
            }
            return Output;
        }
        public void Execute()
        {
            if(ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ) || ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ) && (ControlUnit.CurrentHandle.HandleSettings | HandleParameters.NOJMP) != ControlUnit.CurrentHandle.HandleSettings)
            {
                
                for (uint Count = BitConverter.ToUInt32(ControlUnit.FetchRegister(XRegCode.C, RegisterCapacity.GP_DWORD), 0); Count > 0; Count--)
                {
                    if((Settings | StringOpSettings.COMPARE) == Settings
                        && ((ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ) && ControlUnit.Flags.Zero == FlagState.ON) // repz and repnz act as normal rep if the opcode isnt cmps or scas
                           || (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ) && ControlUnit.Flags.Zero == FlagState.OFF))) 
                    {
                        break;
                    }                    
                    OnExecute();
                    OnInitialise();
                }
                ControlUnit.SetRegister(XRegCode.C, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            }
            else
            {                
                OnExecute();
            }            
        }
        protected abstract void OnInitialise();
        protected abstract void OnExecute();
        public List<byte[]> Fetch()
        {
            List<byte[]> Output = new List<byte[]>(2);
            if((Settings | StringOpSettings.A_DEST) == Settings)
            {
                Output.Add(ControlUnit.FetchRegister(XRegCode.A, Capacity, true));                
            }
            else
            {
                Output.Add(ControlUnit.Fetch(DestPtr, (int)Capacity));
                AdjustDI(Capacity);
            }
            if ((Settings | StringOpSettings.A_SRC) == Settings)
            {
                Output.Add(ControlUnit.FetchRegister(XRegCode.A, Capacity, true));
            }
            else
            {
                Output.Add(ControlUnit.Fetch(SrcPtr, (int)Capacity));
                AdjustSI(Capacity);
            }
            return Output;
        }
        public void Set(byte[] data) {
            data = Bitwise.Cut(data, (int)Capacity);
            if ((Settings | StringOpSettings.A_SRC) == Settings)
            {
                ControlUnit.SetRegister(XRegCode.A, data);
            }
            else
            {
                ControlUnit.SetMemory(DestPtr, data);
            }
        } 
    }
}
