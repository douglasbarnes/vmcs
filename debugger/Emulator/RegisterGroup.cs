using System;
using System.Collections.Generic;
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
    public enum RegisterTable
    {
        GP=0,
        MMX=0x10,
        SSE=0x20,
    }
    public enum RegisterCapacity
    {
        NONE = 0,
        BYTE = 1,
        WORD = 2,
        DWORD = 4,
        QWORD = 8,
        M128 = 16,
        M256 = 32,
    }
    public class RegisterGroup
    {        
        private readonly byte[][] Registers = new byte[][] {
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8], //A,C,D,B,SP,BP,SI,DI
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8], //8,9,10,11,12,13,14,15
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8], //mm0,1,2,3,4,5,6,7
            null,       null,       null,       null,       null,       null,       null,       null,        // will point to ^
            new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32], // ymm
            new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],
        }; 
        public RegisterGroup()
        {
            for (int i = 0; i < 8; i++)
            {
                Registers[0x18 + i] = Registers[0x10 + i];
            }
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
        public byte[] this[RegisterTable table, RegisterCapacity cap, XRegCode register]
        {
            get {
                byte[] Output = new byte[(int)cap];
                for (int i = 0; i < Output.Length; i++)
                {
                   Output[i] = Registers[(int)register+(int)table][i];
                }
                return Output;
            }
            set
            {
                if(value.Length > ((int)cap))
                {
                    throw new Exception("RegisterGroup.cs Attempt to overflow register in base class");
                }
                for (int i = 0; i < value.Length; i++)
                {
                    Registers[(int)register+(int)table][i] = value[i];
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
