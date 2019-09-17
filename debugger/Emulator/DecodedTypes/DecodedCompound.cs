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
        public DecodedCompound(IMyDecoded input1, IMyDecoded input2)
        {
            InternalArray = new IMyDecoded[] { input1, input2 };
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
        public List<byte[]> Fetch() => _Fetch(true);
        private List<byte[]> _Fetch(bool discriminative)
        {
            List<byte[]> Output = new List<byte[]>();
            for (int i = 0; i < InternalArray.Length; i++)
            {
                if (discriminative)
                {
                    Output.AddRange(InternalArray[i].Fetch());
                }
                else
                {
                    Output.Add(InternalArray[i].Fetch()[0]);
                }
            }
            return Output;
        }
        public List<byte[]> FetchIndiscriminative() => _Fetch(false);
        //public byte[] FetchIndiscriminative(int index) => InternalArray[index].Fetch()[0];
        public void Initialise(RegisterCapacity size)
        {
            for (int i = 0; i < InternalArray.Length; i++)
            {
                InternalArray[i].Initialise(size);
            }
        }
        public void Set(byte[] data) => InternalArray[0].Set(data);
        public void Set(byte[] data, int index)
        {
            for (int i = 0; i < InternalArray.Length; i++)
            {
                if(InternalArray[i] is IMyMultiDecoded Cursor)
                {
                    if(i+1 == index)
                    {
                        Cursor.SetSource(data);
                        return;
                    }
                    i++;
                }
                if(i == index)
                {
                    InternalArray[index].Set(data);
                }
            }            
        }
        public void SetIndiscriminative(byte[] data, int index) => InternalArray[index].Set(data);
        public void SetSource(byte[] data)
        {
            if(InternalArray[0] is IMyMultiDecoded Cursor)
            {
                Cursor.SetSource(data);
            }
            else
            {
                InternalArray[1].Set(data);
            }            
        }
    }
}
