using System.Collections.Generic;

namespace debugger.Emulator.DecodedTypes
{
    public class DecodedCompound : IMyMultiDecoded
    {
        public RegisterCapacity Size { get; private set; }
        private readonly IMyDecoded[] InternalArray;
        public DecodedCompound(IMyDecoded[] input)
        {
            InternalArray = input;
        }
        public List<string> Disassemble()
        {
            List<string> Output = new List<string>();            
            for (int i = 0; i < InternalArray.Length; i++)
            {
                Output.AddRange(InternalArray[i].Disassemble());
            }
            return Output;
        }
        public List<byte[]> Fetch()
        {
            List<byte[]> Output = new List<byte[]>();
            for (int i = 0; i < InternalArray.Length; i++)
            {
                Output.AddRange(InternalArray[i].Fetch());
            }
            return Output;
        }
        public void Initialise(RegisterCapacity size)
        {
            for (int i = 0; i < InternalArray.Length; i++)
            {
                InternalArray[i].Initialise(size);
            }
        }
        public void Set(byte[] data) => InternalArray[0].Set(data);
        public void SetSource(byte[] data) => InternalArray[1].Set(data);
    }
}
