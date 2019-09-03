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
        protected ulong SrcPtr;
        protected ulong DestPtr;
        private RegisterCapacity PtrSize; // for in the rare rare rare case that esi/edi could carry out
        protected RegisterCapacity Capacity; 
        protected StringOpSettings Settings;
        
        public StringOperation(string mnemonic, StringOpSettings settings)
        {            
            Settings = settings;
            if ((Settings | StringOpSettings.BYTEMODE) == Settings)
            {
                Capacity = RegisterCapacity.BYTE;
                mnemonic += 'B';
            }
            else if ((ControlUnit.RexByte | REX.W) == ControlUnit.RexByte)
            {
                Capacity = RegisterCapacity.QWORD;
                mnemonic += 'Q';
            }
            else if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR))
            {
                Capacity = RegisterCapacity.WORD;
                mnemonic += 'W';
            }
            else
            {
                Capacity = RegisterCapacity.DWORD;
                mnemonic += 'D';
            }
            Mnemonic = mnemonic;
            PtrSize = ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD;
            SrcPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), 8), 0);
            DestPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), 8), 0);
            OnInitialise();
        }
        public void AdjustDI()
        {
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                DestPtr -= (byte)Capacity;
            }
            else
            {
                DestPtr += (byte)Capacity;             
            }
        }
        public void AdjustSI()
        {
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                SrcPtr -= (byte)Capacity;
            }
            else
            {
                SrcPtr += (byte)Capacity;         
            }
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
                
                for (uint Count = BitConverter.ToUInt32(ControlUnit.FetchRegister(XRegCode.C, RegisterCapacity.DWORD), 0); Count > 0; Count--)
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
            ControlUnit.SetRegister(XRegCode.DI, BitConverter.GetBytes(PtrSize == RegisterCapacity.QWORD ? DestPtr : (uint)DestPtr));
            ControlUnit.SetRegister(XRegCode.SI, BitConverter.GetBytes(PtrSize == RegisterCapacity.QWORD ? SrcPtr : (uint)SrcPtr));
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
            }
            if ((Settings | StringOpSettings.A_SRC) == Settings)
            {
                Output.Add(ControlUnit.FetchRegister(XRegCode.A, Capacity, true));
            }
            else
            {
                Output.Add(ControlUnit.Fetch(SrcPtr, (int)Capacity));
            }
            return Output;
        }
        public void Set(byte[] data) {
            data = Bitwise.Cut(data, (int)Capacity);
            if ((Settings | StringOpSettings.A_DEST) == Settings)
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
