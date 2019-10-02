using System.IO;
namespace debugger.IO
{
    public class BIN : IMyExecutable
    {
        public byte[] Instructions { get; private set; }
        public static BIN Parse(FileStream reader)
        {
            reader.Seek(0, SeekOrigin.Begin);
            BIN Output = new BIN()
            {
                Instructions = new byte[reader.Length]
            };       
            reader.Read(Output.Instructions, 0, (int)reader.Length);
            return Output;
        }
    }
}
