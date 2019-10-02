using System.Collections.Generic;
using System.IO;
using debugger.Logging;
namespace debugger.IO
{
    public enum FileType
    {
        BIN=0,
        ELF=1,
    }
    public class FileParser
    {
        private delegate IMyExecutable MagicDelegate(FileStream inputReader);
        private static readonly Dictionary<int, MagicDelegate> SignatureTable = new Dictionary<int, MagicDelegate>()
        {
            { 0x7F454C46, ELF.Parse }
        };
        private readonly IMyExecutable Target;
        public byte[] Instructions => Target.Instructions;
        public FileParser(FileInfo inputFile)
        {
            CheckExists(inputFile);
            Target = DetermineMagic(inputFile);
        }
        private static void CheckExists(FileInfo inputFile)
        {
            if (!inputFile.Exists)
            {
                throw new LoggedException(LogCode.IO_FILENOTFOUND, inputFile.FullName);
            }
        }
        private static IMyExecutable DetermineMagic(FileInfo inputFile)
        {
            byte[] MagicBytes = new byte[4];
            using(FileStream Reader = inputFile.Open(FileMode.Open))
            {
                if(Reader.Read(MagicBytes, 0, 4) != 4)
                {
                    throw new LoggedException(LogCode.IO_INVALIDFILE, "Too few bytes in file to determine file type.");
                }
                int Signature = MagicBytes[3] + (MagicBytes[2] << 8) + (MagicBytes[1] << 16) + (MagicBytes[0] << 24);
                return SignatureTable.TryGetValue(Signature, out MagicDelegate Result) ? Result(Reader) : BIN.Parse(Reader);
            }            
        }
    }
}
