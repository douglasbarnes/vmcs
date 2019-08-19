using System;
using System.Collections.Generic;
using System.Linq;
using debugger.Util;
namespace debugger.Emulator
{
    public enum XRegCode
    {
        A = 0x00,
        C = 0x01,
        D = 0x02,
        B = 0x03,
        SP = 0x04,
        BP = 0x05,
        SI = 0x06,
        DI = 0x07,
        R8 = 0x08,
        R9 = 0x09,
        R10 = 0xa,
        R11 = 0xb,
        R12 = 0xc,
        R13 = 0xd,
        R14 = 0xe,
        R15 = 0xf
    }
    public enum RegisterCapacity
    {
        GP_BYTE = 1,
        GP_WORD = 2,
        GP_DWORD = 4,
        GP_QWORD = 8
    }
    public class RegisterGroup
    {        
        private byte[][] Registers = new byte[16][] {
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8], //A,C,D,B,SP,BP,SI,DI
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8], //8,9,10,11,12,13,14,15
        }; 
        public RegisterGroup()
        {
        }
        public RegisterGroup(Dictionary<XRegCode, byte[]> registers)
        {
            foreach (var Register in registers)
            {
                for (int i = 0; i < 8; i++)
                {
                    Registers[(int)Register.Key][i] = Register.Value[i];
                }                
            }
        }
        public RegisterGroup(Dictionary<XRegCode, ulong> inputRegs)
        {
            foreach (var Register in inputRegs)
            {
                byte[] RegisterBytes = BitConverter.GetBytes(Register.Value);
                for (int i = 0; i < 8; i++)
                {
                    Registers[(int)Register.Key][i] = RegisterBytes[i];
                }
            }
        }
        private RegisterGroup(RegisterGroup toClone)
        {
            for (int i = 0; i < toClone.Registers.Length; i++)
            {
                Registers[i] = toClone.Registers[i].DeepCopy();
            }            
        }
        public RegisterGroup DeepCopy() => new RegisterGroup(this);
        public byte[] this[RegisterCapacity group, XRegCode register]
        {
            get {
                byte[] Output = new byte[(int)group];
                for (int i = 0; i < Output.Length; i++)
                {
                   Output[i] = Registers[(int)register][i];
                }
                return Output;
            }
            set
            {
                if(value.Length > ((int)group))
                {
                    throw new Exception("RegisterGroup.cs Attempt to overflow register in base class");
                }
                for (int i = 0; i < value.Length; i++)
                {
                    Registers[(int)register][i] = value[i];
                }
            }
        }
        public Dictionary<XRegCode, byte[]> FetchAll()
        {
            RegisterGroup Cloned = DeepCopy();
            Dictionary<XRegCode, byte[]> Output = new Dictionary<XRegCode, byte[]>();
            for (int register = 0; register < Cloned.Registers.Length; register++)
            {
                Output.Add((XRegCode)register, Cloned.Registers[register]);
            }
            return Output;
        }
    }
}
