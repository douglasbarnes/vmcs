// FileParser provides the basis for loading and parsing external files as instructions.
// It can be used in a number of different ways,
// - Automatically detecting the file type through magic byte signatures
// - Parsing as a text file
// - Parsing as a binary file
// The last two are necessary when the first bytes of a txt/bin file are the same as anothers signature by coincidence.
using System.Collections.Generic;
using System.IO;
using debugger.Logging;
namespace debugger.IO
{
    public enum ParseMode
    {
        AUTO=0,
        BIN=1,
        TXT=2,
    }
    public enum ParseResult
    {
        SUCCESS=0,
        NOT_INFERRED=1,
        INVALID=2,
    }
    public class FileParser
    {
        private delegate IMyExecutable MagicDelegate(FileStream inputReader);
        private static readonly Dictionary<int, MagicDelegate> SignatureTable = new Dictionary<int, MagicDelegate>()
        {
            { 0x7F454C46, ELF.Parse }
        };
        private readonly FileInfo TargetFile;
        public FileParser(FileInfo inputFile)
        {
            if (!inputFile.Exists)
            {
                throw new LoggedException(LogCode.IO_FILENOTFOUND, inputFile.FullName);
            }
            TargetFile = inputFile;
        }
        public ParseResult Parse(ParseMode mode, out byte[] Instructions)
        {
            byte[] MagicBytes = new byte[4];
            
            // Open the file as read only.
            using(FileStream Reader = TargetFile.Open(FileMode.Open))
            {
                if(mode == ParseMode.AUTO)
                {
                    // There must be no less than 4 bytes in the file in order to determine its magic bytes. This is not necessarily true for
                    // every file type. If there are only 3 bytes in the read file, the user probably chose the wrong file anyway.
                    // Reader.Read() returns the number of bytes read.
                    if (Reader.Read(MagicBytes, 0, 4) != 4)
                    {
                        Logger.Log(LogCode.IO_INVALIDFILE, "File must be no less than 4 bytes in length");
                        Instructions = null;
                        return ParseResult.INVALID;
                    }

                    // Order the bytes in big endian(because was read from a file) to form the signature of magic bytes.
                    // Most magic byte signatures are 4 bytes, especially for the purposes of this program.
                    int Signature = MagicBytes[3] + (MagicBytes[2] << 8) + (MagicBytes[1] << 16) + (MagicBytes[0] << 24);

                    // Check if the file type can be inferred from the magic byte signature from the signatures registered in the SignatureTable.
                    MagicDelegate ResultDel;
                    if (SignatureTable.TryGetValue(Signature, out ResultDel))
                    {
                        IMyExecutable Result = ResultDel(Reader);
                        // If $Result is null after parsing, there was an error in doing so.
                        if (Result == null)
                        {
                            Instructions = null;
                            return ParseResult.INVALID;
                        }
                        else
                        {
                            Instructions = ResultDel(Reader).Instructions;
                            return ParseResult.SUCCESS;                            
                        }  
                    }
                    else
                    {
                        // If the file type cannot be inferred, 
                        Instructions = null;
                        return ParseResult.NOT_INFERRED;
                    }
                }

                // Other parse modes for forcing files to be interpreted as a particular format.
                else if(mode == ParseMode.BIN)
                {
                    Instructions = BIN.Parse(Reader).Instructions;
                    return ParseResult.SUCCESS;
                }
                else if(mode == ParseMode.TXT)
                {
                    TXT Result = TXT.Parse(Reader);
                    // If $Result is null after parsing, there was an error in doing so.
                    if (Result == null)
                    {
                        Instructions = null;
                        return ParseResult.INVALID;
                    }
                    else
                    {
                        Instructions = Result.Instructions;
                        return ParseResult.SUCCESS;
                    }                    
                }                
            }

            // This should never be reached.
            throw new System.Exception();
        }
    }
}
