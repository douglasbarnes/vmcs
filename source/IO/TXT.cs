// TXT IMyExecutable allows a file to read that contains UTF-8 encoded text that can be interpreted as hex bytes. Behaviour
// when a different encoding could work, see Core.Htoi(), but is undefined nevertheless.
// The use case of TXT would be when the user has copied text representing bytes from the output of objdump or alike. This
// saves them having to use other programs to convert that output into actual bytes(that BIN would parse).
// If invalid data is found, said byte is ignored. 
// This is a rare case where working around invalid input is better than assuming it to be evil because,
//  -It is too common that a trailing whitespace will be left or newline when creating a file. 
//  -Other byte representations can be used, for example C style hex bytes, \x00\xC0\x90
//  -The scope of invalid bytes poisoning the valid data is low, as only 0-9 and A-F will be considered valid.
// Exact details of what happens in the case of invalid input can be found in Parse()
using System.IO;
using debugger.Logging;
namespace debugger.IO
{
    public class TXT : IMyExecutable
    {
        public byte[] Instructions { get; private set; }
        public static TXT Parse(FileStream reader)
        {
            // Read all of the file into FileBytes.
            reader.Seek(0, SeekOrigin.Begin);                      

            // Make sure FileBytes[] has an even length, but not necessarily $reader.Length aswell
            byte[] FileBytes = new byte[reader.Length % 2 == 0 ? reader.Length : reader.Length + 1];
            reader.Read(FileBytes, 0, (int)reader.Length);

            // An array to store the bytes in the file once parsed. As said earlier, two bytes in the file really represents a single byte, so this 2:1 ratio can
            // be used to save memory in the array. It also depicts a worst(memory-wise) case scenario, that every byte is valid hex, e.g no line feeds etc.
            byte[] ParsedBytes = new byte[reader.Length / 2];

            
            // The following is an method of parsing the UTF-8 encoded bytes in the file as actual hex bytes.
            // Throughout explanation of this particular algorithm I will use "character" to describe a character in the file, e.g A, B, 1, G, ]
            // and use byte to refer to an actual byte in memory. This is not technically accurate but extremely necessary to explain the logic of this algorithm,
            // so forget about this definition afterwards.
            // Understand that a byte written in hex as text is two bytes in the file. E.g, writing B8 is two characters B and 8. The numerical value of "B8" is not 0xB8.
            // This is the premise of the algorithm, converting the characters "B8" into the byte 0xB8.
            // A similar method Core.TryParseHex() does the same job in a different context. Due to the nature of how the data is recieved, the two cannot be combined
            // without significant and unnecessary intermediate conversion procedures.
            // Despite not looking so at first glance, the algorithm is linear as you would expect given the problem as the method always exits after $reader.Length iterations.
            int BytesParsed = 0;

            // Firstly, every byte in the file should be considered as a potential hex byte, so intend to iterate the whole file.
            // Note that $BytesParsed is incremented here not $FileCaret as it is done elsewhere. Every completed for loop is a two characters
            // concatenated into a byte.
            for (int FileCaret = 0; FileCaret < reader.Length; BytesParsed++)
            {
                // $CharsLeft holds the number of characters left to complete the current byte. Since two characters are concatenated into a byte, the value starts at 2.
                byte CharsLeft = 2;

                // $ParsedByteBuffer holds the current byte that is the result of concatenation.
                byte ParsedByteBuffer = 0x00;

                // Loop until two parsable characters have been found, or the eof is reached.
                while (CharsLeft != 0 && FileCaret < reader.Length)
                {
                    // Here the fortunate fact that each character in a hex byte is a nibble can be used.
                    // For example if I had two hex values,
                    //  0xD - [00001101]
                    //  0x7 - [00000111]
                    // 0xD7 would just be,
                    //  0xD7 - [11010111]
                    // This is an cool relationship between bases that are powers of 2, but some maths can be used to implement it.
                    // In the code I take a little bit of a performance shortcut because single byte values are being dealt with, such
                    // that there will only ever be two columns, e.g 0xFF not 0x100. Conversely, the single digit value will either be
                    // shifted by 4 or none at all. This is because as shown before, one digit represents a nibble.
                    // So, if it is the first character in the byte being parsed, it is shifted by 4 because it would be the D in D7 in
                    // the earlier example. Otherwise it's just ORed normally.
                    byte ParsedNibble;

                    // Core.Htoi() will convert a char to the hex byte it visually represented, e.g B -> 0xB.
                    if(Util.Core.Htoi((char)FileBytes[FileCaret], out ParsedNibble))
                    {
                        // Don't change this order without changing ther ternary condition to == 1.
                        CharsLeft--;
                        ParsedByteBuffer |= CharsLeft == 0 ? ParsedNibble : (byte)(ParsedNibble << 4);                        
                    }

                    // Whether the byte could be parsed or not, the next character is next to be parsed.
                    FileCaret++;
                }

                // Commit the new byte into the output buffer.
                ParsedBytes[BytesParsed] = ParsedByteBuffer;
            }

            // The file must be invalid if the number of parsed bytes is not evenly divisible by 2 because every hex byte must be two characters e.g FF, B8, 0A.
            if (BytesParsed % 2 != 0 && BytesParsed == 0)
            {
                Logger.Log(LogCode.IO_INVALIDFILE, "Input file did not contain a valid number of hex-parsable characters.");
                return null;
            }

            // Resize the array to the number of bytes parsed, as the worst case scenario was assumed earlier. Otherwise a whole host of "add    byte ptr [rax], al" would be added to the
            // end of the program because the unused array elements would be [00] [00] [00] [00] ...
            System.Array.Resize(ref ParsedBytes, BytesParsed);

            return new TXT()
            {
                Instructions = ParsedBytes
            };
        }
    }
}
