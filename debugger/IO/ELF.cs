using System;
using System.IO;
using debugger.Logging;
using debugger.Util;
namespace debugger.IO
{
    public class ELF : IMyExecutable
    {
        private byte Class;
        public ulong EntryPoint;
        private ulong TextLength;
        private ulong SHOffset;
        private ushort SHLength;
        private ushort SHCount;
        private ushort SHStrIndex;
        public byte[] Instructions { get; private set; }
        public static ELF Parse(FileStream reader)
        {                        
            ELF ParsedElf;

            // Check if the length is less than the minimum acceptable.
            if (reader.Length < 0x34)
            {
                Logger.Log(LogCode.IO_INVALIDFILE, "Incomplete ELF header");
                return null;
            }

            // Determine the class of the elf. If the class byte is equal to 1, the ELF is 32 bit, 2 for 64-bit. Otherwise assume an invalid elf.
            // The class byte is the 4th byte(0,1,2,3,4<-) so make sure the stream is at position 4. This is required to know the length of the header.
            reader.Seek(4, SeekOrigin.Begin);
            byte[] Elf_class = new byte[1];
            reader.Read(Elf_class, 0, 1);
          
            // Read and parse the rest of the header.
            // All bytes are offset slightly because 5 bytes(magic bytes + class) have already been parsed.
            // The offsets differ between elf32 and elf64 because addresses in elf64 must be 8 bytes; 4 bytes in elf32.
            byte[] FileHeader;
            switch (Elf_class[0])
            {
                case 0x01:
                    FileHeader = new byte[0x34 - 0x05];
                    reader.Read(FileHeader, 0, FileHeader.Length);
                    ParsedElf = new ELF()
                    {
                        Class = 32,
                        
                        // The address of the section header table.
                        SHOffset = BitConverter.ToUInt32(FileHeader, 0x20 - 0x05),

                        // The length of each header in the section header table.
                        SHLength = BitConverter.ToUInt16(FileHeader, 0x2E - 0x05),

                        // The number of section headers in the section header table.
                        SHCount = BitConverter.ToUInt16(FileHeader, 0x30 - 0x05),

                        // The indeex of the section header containing the string names of all other sections in the section header table.
                        SHStrIndex = BitConverter.ToUInt16(FileHeader, 0x32 - 0x05)
                    }; 
                    break;
                case 0x02:
                    FileHeader = new byte[0x40 - 0x05];
                    reader.Read(FileHeader, 0, FileHeader.Length);
                    ParsedElf = new ELF()
                    {
                        Class = 64,                        
                        SHOffset = BitConverter.ToUInt64(FileHeader, 0x28 - 0x05),
                        SHLength = BitConverter.ToUInt16(FileHeader, 0x3A - 0x05),
                        SHCount = BitConverter.ToUInt16(FileHeader, 0x3C - 0x05),
                        SHStrIndex = BitConverter.ToUInt16(FileHeader, 0x3E - 0x05)
                    };                    
                    break;
                default:
                    Logger.Log(LogCode.IO_INVALIDFILE, "Invalid class byte in ELF header");
                    return null;
            };

            // Make sure ELF uses x86-x
            if(FileHeader[0xD] != 0x03 && FileHeader[0xD] != 0x3E)
            {
                Logger.Log(LogCode.IO_INVALIDFILE, "Input elf does not use the x86 instruction set");
                return null;
            }

            // Create a byte array with enough space to store the entire sh table.
            byte[] SHTable = new byte[ParsedElf.SHLength * ParsedElf.SHCount];

            // Seek the file to the offset of the SH table.
            reader.Seek((long)ParsedElf.SHOffset, SeekOrigin.Begin);

            // Read the entire SH table into $SHTable.
            reader.Read(SHTable, 0, ParsedElf.SHLength * ParsedElf.SHCount);

         
            // Use cut and subarray to cut out the shstrtab(section header string table; section that points to text names) from the SH table.
            // Subarray cuts out the preceeding tables. The length of which is the number of tables before shstrtab(which happens to be the index of shstrtab) multiplied by
            // the length of each table.
            byte[] SHstrtab = Bitwise.Cut(Bitwise.Subarray(SHTable, ParsedElf.SHLength * ParsedElf.SHStrIndex), ParsedElf.SHLength);

            // Get the size of strtab stored at offset 0x20 (elf32:0x14) of the section header and
            // seek to the position in the elf file of the actual strtab(not the shstrtab section header)
            int StrtabSize;
            if (ParsedElf.Class == 32)
            {
                StrtabSize = BitConverter.ToInt32(SHstrtab, 0x14);
                reader.Seek(BitConverter.ToUInt32(SHstrtab, 0x10), SeekOrigin.Begin);
            }
            else
            {
                StrtabSize = BitConverter.ToInt32(SHstrtab, 0x20);
                reader.Seek(BitConverter.ToUInt32(SHstrtab, 0x18), SeekOrigin.Begin);
            }
            
            
            byte[] strtab = new byte[StrtabSize];
            
            reader.Read(strtab, 0, StrtabSize);
            // Read sh_name from every section in shtab and check if the offset($shstrtab + $offset = target) is the string ".text".
            // .text is holds the user code and therefore is the only section this program cares about.
            // If there isn't one, which most likely means this wasn't an elf file, so handle it because this file cannot be used.
            for (int i = 0; i < ParsedElf.SHCount; i++)
            {
                if (ReadString(strtab, BitConverter.ToUInt32(SHTable, i * ParsedElf.SHLength)) == ".text")
                {
                    if(ParsedElf.Class == 32)
                    {
                        // Store the EntryPoint(in the file not when in memory) and TextLength(length of the
                        // code in bytes) in the elf object.
                        ParsedElf.EntryPoint = BitConverter.ToUInt32(SHTable, i * ParsedElf.SHLength + 0x10);
                        ParsedElf.TextLength = BitConverter.ToUInt32(SHTable, i * ParsedElf.SHLength + 0x14);
                    }
                    else
                    {
                        ParsedElf.EntryPoint = BitConverter.ToUInt64(SHTable, i * ParsedElf.SHLength + 0x18);
                        ParsedElf.TextLength = BitConverter.ToUInt64(SHTable, i * ParsedElf.SHLength + 0x20);
                    }
                    break;
                }
                else if(i + 1 == ParsedElf.SHCount)
                {
                    Logger.Log(LogCode.IO_INVALIDFILE, "ELF File had no .text section");
                    return null;
                }
            }

            // Finally copy all the bytes from the .text segment into $Instructions.
            reader.Seek((long)ParsedElf.EntryPoint, SeekOrigin.Begin);
            ParsedElf.Instructions = new byte[(int)ParsedElf.TextLength];
            reader.Read(ParsedElf.Instructions, 0, (int)ParsedElf.TextLength);

            return ParsedElf;
        }
        private static string ReadString(byte[] strtab, uint offset)
        {
            string Output = "";
            for (uint i = offset; i < strtab.Length; i++)
            {
                // Check if the current byte is a null(the end of a string) and break if it is
                if(strtab[i] == 0x00)
                {
                    break;
                }

                // Char makes sure that the byte is taken as-is when appended to the string. There is no atoi() or any annoying .NET stuff in the way,
                // purely the byte will be parsed as a utf-8 character. As a side effect, if for some reason the strtab has some weird section names,
                // it's not going to look right. That is however an ELF thing and the user would be doing something insane if that was the case regardless.
                Output += (char)strtab[i];
            }
            return Output;
        }
    }
}
