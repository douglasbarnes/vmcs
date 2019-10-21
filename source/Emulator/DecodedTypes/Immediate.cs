// Immediate provides a robust and IMyDecoded comformant method of handling immediate data. At first thought, you would think it super simple to deal with immediates,
// it's just ControlUnit.FetchNext($x). However, in terms of generalising disassembly, different immediate settings, etc, having a class to handle it all goes a long way. 
// In older versions of the code, immediates were handled in the Opcode base class, which became an absolute eye sore. In this sense, the IMyDecoded interface is probably
// the best thing that happened to this program.
using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    [Flags]
    public enum ImmediateSettings
    {
        NONE = 0,
        SXTBYTE = 1,
        ALLOWIMM64 = 2,
        RELATIVE = 4,
    }
    public class Immediate : IMyDecoded
    {
        public RegisterCapacity Size { get; private set; }
        private readonly ImmediateSettings Settings;
        private byte[] Buffer = null;
        public Immediate(ImmediateSettings settings = ImmediateSettings.NONE)
        {
            Settings = settings;
        }
        public void Initialise(RegisterCapacity size)
        {
            // The immediate is fetched only once per instance. I don't see it unrealistic for Initialise() to be called more than once.
            // Obviously if this check was not in place, the immediate buffer would change every time and most likely gobble the next instruction.
            if (Buffer == null)
            {
                Size = size;

                // The default length of an immediate is 4 bytes.
                byte MaxVal = 0b0100;

                // If the immediate is a sign extended byte, only the next byte will be fetched(then sign extended later)
                if ((Settings | ImmediateSettings.SXTBYTE) == Settings)
                {
                    MaxVal = 0b0001;
                }

                // Qword immediates are only allowed if the immediate settings has the ALLOWIMM64 bit set. In practice, this is only for mov instructions 0xb8-0xbf
                else if ((Settings | ImmediateSettings.ALLOWIMM64) == Settings)
                {
                    MaxVal = 0b1000;
                }

                // If $size is less than or equal to $MaxVal, just fetch $size bytes. That is how many the caller expects.
                if ((int)size <= MaxVal)
                {
                    Buffer = ControlUnit.FetchNext((int)size);
                }

                // Otherwise, fetch $MaxVal bytes and sign extend it to $size(to preserve its value)
                else
                {
                    Buffer = Bitwise.SignExtend(ControlUnit.FetchNext(MaxVal), (byte)size);
                }

                // If the immediate settings have the RELATIVE bit set, add the instruction pointer to the buffer. This is used in jumps and calls for example.
                if ((Settings | ImmediateSettings.RELATIVE) == Settings)
                {
                    Bitwise.Add(Buffer, BitConverter.GetBytes(ControlUnit.InstructionPointer), 8, out Buffer);
                }
            }
        }
        public List<byte[]> Fetch() => new List<byte[]>() { Buffer };
        public void Set(byte[] data) => throw new Exception("Attempt to set value of immediate");
        public List<string> Disassemble() => new List<string>() { $"0x{Core.Itoa(Buffer)}" };
    }
}
