using System;
using System.Collections.Generic;
using System.Linq;
using debugger.Util;
namespace debugger.Emulator
{
    public enum ByteCode
    {
        A = 0x00,
        C = 0x01,
        D = 0x02,
        B = 0x03,
        SP = 0x04,
        BP = 0x05,
        SI = 0x06,
        DI = 0x07,
        AH = 0x04,
        CH = 0x05,
        DH = 0x06,
        BH = 0x07
    }
    public enum RegisterCapacity
    {
        BYTE = 1,
        WORD = 2,
        DWORD = 4,
        QWORD = 8
    }
    public class RegisterGroup
    {
        public struct Register
        {
            private byte[] Data;
            public Register(byte[] Input)
            {
                if(Input.Length != 8)
                {
                    throw new Exception("Register: Invalid register size");
                }
                Data = Input;
            }
            public Register(ulong Input)
            {
                Data = BitConverter.GetBytes(Input);
            }
    
            public static implicit operator byte[](Register R)
            {
                return R.Data;
            }
    
            public static implicit operator Register(byte[] Input)
            {
                return new Register(Input);
            }
            private Register(Register toCopy)
            {
                Data = toCopy.Data.DeepCopy();
            }
            public Register Clone()
            {
                return new Register(this);
            }
        }
        public RegisterGroup()
        {
            Registers = new List<Register>();
            for (int i = 0; i < 8; i++)
            {
                Registers.Add(new Register(new byte[8]));
            }
        }
        public RegisterGroup(Dictionary<ByteCode, Register> Input)
        { 
            for (int RegValue = 0; RegValue < 8; RegValue++)
            {
                if (Input.TryGetValue((ByteCode)RegValue, out Register Current))
                {
                    Registers.Add(Current);
                }
                else
                {
                    Registers.Add(new Register(new byte[8]));
                }
            }
        }
        private RegisterGroup(RegisterGroup toClone)
        {
            Registers.AddRange(toClone.Registers);
            //for (int i = 0; i < toClone.Registers.Count(); i++)
            //{
            //    Registers.Add(toClone.Registers[i].Clone());
            //}
        }
        private readonly List<Register> Registers = new List<Register>();
        public byte[] this[ByteCode register, RegisterCapacity size]
        {
            get => Bitwise.Cut(Registers[(int)register], (int)size);
            set => Array.Copy(value, 0, Registers[(int)register], 0, (int)size);
        }
        public RegisterGroup DeepCopy()
        {
            return new RegisterGroup(this);
        }
        public Dictionary<ByteCode, byte[]> FetchAll()
        {
            RegisterGroup Cloned = DeepCopy();
            Dictionary<ByteCode, byte[]> Output = new Dictionary<ByteCode, byte[]>();
            for (int register = 0; register < Cloned.Registers.Count(); register++)
            {
                Output.Add((ByteCode)register, Cloned.Registers[register]);
            }
            return Output;
        }
    
    }
    
}
