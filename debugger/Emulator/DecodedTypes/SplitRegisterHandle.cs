using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    public enum SplitRegisterHandleSettings
    {
        NONE=0,
        FETCH_UPPER=1,
    }
    public class SplitRegisterHandle : IMyDecoded
    {
        public RegisterCapacity Size { get; private set; }
        public SplitRegisterHandleSettings Settings;
        private readonly ControlUnit.RegisterHandle Upper;
        private readonly ControlUnit.RegisterHandle Lower;
        public SplitRegisterHandle(XRegCode lower, XRegCode upper, SplitRegisterHandleSettings settings = SplitRegisterHandleSettings.NONE)
        {
            Upper = new ControlUnit.RegisterHandle(upper, RegisterTable.GP);
            Lower = new ControlUnit.RegisterHandle(lower, RegisterTable.GP);
            Settings = settings;
        }
        public void Initialise(RegisterCapacity size)
        {
            Size = size;
            Upper.Initialise(size);
            Lower.Initialise(size);                    
        }
        public List<string> Disassemble()
            => new List<string>() { $"{Upper.Disassemble()[0]}:{Lower.Disassemble()[0]}" };

        public List<byte[]> Fetch()
        {
            if((Settings | SplitRegisterHandleSettings.FETCH_UPPER) == Settings)
            {
                byte[] Output = new byte[(int)Size * 2];
                System.Array.Copy(Lower.FetchOnce(), Output, (int)Size);
                System.Array.Copy(Upper.FetchOnce(), 0, Output, (int)Size, (int)Size);
                return new List<byte[]> { Output };
            }
            else
            {
                return new List<byte[]> { Lower.FetchOnce() };
            }
        }
             
        public void Set(byte[] data)
        {
            Upper.Set(Bitwise.Subarray(data, (int)Size));
            Lower.Set(Bitwise.Cut(data, (int)Size));
        }
    }
}
