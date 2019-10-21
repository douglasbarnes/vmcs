// BIN provides a simple way to load bytes in a file as instructions. Bytes are read as-is, there is no extra translation.
// The use case of this would be when the user's assembler just outputs executable code rather than an object file.
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
