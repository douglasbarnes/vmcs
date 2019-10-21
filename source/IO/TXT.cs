// TXT IMyExecutable allows a file to read that contains UTF-8 encoded text that can be interpreted as hex bytes. Behaviour
// when a different encoding could work, see Core.Htoi(), but is undefined nevertheless.
// The use case of TXT would be when the user has copied text representing bytes from the output of objdump or alike. This
// saves them having to use other programs to convert that output into actual bytes(that BIN would parse).
// If invalid data is found, said byte is ignored. 
// This is a rare case where working around invalid input is better than assuming it to be evil because,
//  -It is too common that a trailing whitespace will be left or newline when creating a file. 
//  -Other byte representations can be used, for example C style hex bytes, \x00\xC0\x90
//  -The scope of invalid bytes poisoning the valid data is low, as only 0-9 and A-F will be considered valid.
// Exact details of what happens in the case of invalid input can be found in Core.TryParseHex()
using debugger.Logging;
using System.IO;
namespace debugger.IO
{
    public class TXT : IMyExecutable
    {
        public byte[] Instructions { get; private set; }
        public static TXT Parse(FileStream reader)
        {
            // Read all of the file into FileBytes.
            reader.Seek(0, SeekOrigin.Begin);

            // Make sure FileBytes[] has an even length, but not necessarily $reader.Length aswell. Essentially this rounds $FileBytes.Length
            // up to the nearest multiple of two.
            byte[] FileBytes = new byte[reader.Length / 2 + reader.Length % 2];
            reader.Read(FileBytes, 0, (int)reader.Length);
            return Parse(FileBytes);
        }

        public static TXT Parse(byte[] encoded_bytes)
        {
            byte[] ParsedBytes;

            // Try to parse the encoded bytes into their intended byte values. Returns false if none could.
            if (Util.Core.TryParseHex(encoded_bytes, out ParsedBytes))
            {
                return new TXT()
                {
                    Instructions = ParsedBytes
                };
            }
            else
            {
                Logger.Log(LogCode.IO_INVALIDFILE, "Input did not contain any hex-parsable characters.");
                return null;
            }
        }
    }
}
