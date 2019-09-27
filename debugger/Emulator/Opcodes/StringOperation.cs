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
    }
    public abstract class StringOperation : IMyOpcode
    {      
        private string Mnemonic;
        protected ulong SrcPtr;
        protected ulong DestPtr;
        private RegisterCapacity PtrSize; // for in the rare rare rare case that esi/edi could carry out
        protected RegisterCapacity Capacity; 
        protected StringOpSettings Settings;
        // INITIALISED TO $PTRSIZE NOT $CAPACITY
        private ControlUnit.RegisterHandle Destination;
        private ControlUnit.RegisterHandle Source;
        private readonly List<byte[]> Operands;        
        public StringOperation(string mnemonic, XRegCode destination, XRegCode source, StringOpSettings settings)
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
            Destination = new ControlUnit.RegisterHandle(destination, RegisterTable.GP, PtrSize);
            Source = new ControlUnit.RegisterHandle(source, RegisterTable.GP, PtrSize);
            Operands = new List<byte[]> { Destination.FetchAs(Capacity), Source.FetchAs(Capacity) };
            DestPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(Destination.FetchOnce(), 8), 0);
            SrcPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(Source.FetchOnce(), 8), 0);
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
            Output.Add(Destination.DisassembleOnce());
            Output.Add(Source.DisassembleOnce());
            if (Source.Code == XRegCode.SI)
            {
                Output[2] = $"[{Output[2]}]";
            }
            if (Destination.Code == XRegCode.DI)
            {
                Output[1] = $"[{Output[1]}]";
            }
            return Output;
        }
        public void Execute()
        {
            if(ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ) || ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ) && (ControlUnit.CurrentHandle.HandleSettings | HandleParameters.NOJMP) != ControlUnit.CurrentHandle.HandleSettings)
            {
                ControlUnit.RegisterHandle CountHandle = new ControlUnit.RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.DWORD);
                for (uint Count = BitConverter.ToUInt32(CountHandle.FetchOnce(), 0); Count > 0; Count--)
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
                CountHandle.Set(new byte[4]);
            }
            else
            {                
                OnExecute();
            }
            if (Destination.Code == XRegCode.DI)
            {
                new ControlUnit.RegisterHandle(XRegCode.DI, RegisterTable.GP, PtrSize).Set(BitConverter.GetBytes(PtrSize == RegisterCapacity.QWORD ? DestPtr : (uint)DestPtr));
            }
            if (Source.Code == XRegCode.SI)
            {
                new ControlUnit.RegisterHandle(XRegCode.SI, RegisterTable.GP, PtrSize).Set(BitConverter.GetBytes(PtrSize == RegisterCapacity.QWORD ? SrcPtr : (uint)DestPtr));
            }                     
        }
        protected abstract void OnInitialise();
        protected abstract void OnExecute();
        public List<byte[]> Fetch()
        {
            List<byte[]> Output = new List<byte[]>(2);
            if(Destination.Code == XRegCode.DI)
            {
                Output.Add(ControlUnit.Fetch(DestPtr, (int)Capacity));                
            }
            else
            {
                Output.Add(Operands[0]);
            }
            if (Source.Code == XRegCode.SI)
            {
                Output.Add(ControlUnit.Fetch(SrcPtr, (int)Capacity));
            }
            else
            {
                Output.Add(Operands[1]);
            }
            return Output;
        }
        public void Set(byte[] data)
        {
            data = Bitwise.Cut(data, (int)Capacity);
            if (Destination.Code == XRegCode.DI)
            {
                ControlUnit.SetMemory(DestPtr, data);                
            }
            else
            {
                new ControlUnit.RegisterHandle(XRegCode.A, RegisterTable.GP, Capacity).Set(data);
            }
        } 
    }
}
