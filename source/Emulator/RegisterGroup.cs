// RegisterGroup provides a consistent and simple way to segregate registers away from the rest of the program. Before separating RegisterGroup and MemorySpace from ControlUnit,
// there was always a problem somewhere down the line because of it. Now that they are thoroughly tested, they can be forgotten about as they serve their intended purpose.
// Internally it is just a cleverly handled jagged array, which is referenced using the numerical values of RegisterTable and XRegCode enum entries. RegisterCapacity is only
// used to be strict on the setting of registers. 
using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator
{
    public enum XRegCode
    {
        // An enum to hold all constants for accessing a register
        // In some cases, SP BP SI DI could refer to AH CH DH BH
        A = 0x00,
        C = 0x01,
        D = 0x02,
        B = 0x03,
        SP = 0x04,
        BP = 0x05,
        SI = 0x06,
        DI = 0x07,
        R8 = 0x08,
        R9 = 0x09,
        R10 = 0xa,
        R11 = 0xb,
        R12 = 0xc,
        R13 = 0xd,
        R14 = 0xe,
        R15 = 0xf
    }
    public enum RegisterTable
    {
        // There are 3 register tables considered in the program, one of which is implemented. 
        // Each table holds 16 different registers, or 0x10 in hex. This is why the each table has a
        // value increasing by 16 each time, so a register internally can be accessed just by
        // $RegisterTable + $XRegCode
        GP = 0,
        MMX = 0x10,
        SSE = 0x20,
    }
    public enum RegisterCapacity
    {
        // This enum defines all accepted register capacities in the program. 

        // NONE is used to indicate an error.
        NONE = 0,
        // AL, CL etc
        BYTE = 1,
        // AX, CX etc
        WORD = 2,
        // EAX, ECX etc
        DWORD = 4,
        // RAX, MM0 etc
        QWORD = 8,
        // XMM0-XMM7
        M128 = 16,
        // YMM0-YMM7
        M256 = 32,
    }
    public class RegisterGroup
    {
        private readonly byte[][] Registers = new byte[][] {
            //   A           C           D           B          SP          BP          SI          DI
            //  R8          R9          R10         R11         R12         R13         R14         R15 
            //  MM0         MM1         MM2         MM3         MM4         MM5         MM6         MM7            
            //  MM0         MM1         MM2         MM3         MM4         MM5         MM6         MM7 <-- These point to the MMX registers above them(Done in the constructor)
            //  YMM0        YMM1        YMM2        YMM3        YMM4        YMM5        YMM6        YMM6 <-- XMM registers are the lower half of these

            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],
            new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],new byte[8],
            null,       null,       null,       null,       null,       null,       null,       null,
            new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],
            new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],new byte[32],
        };
        private void InitialiseMMX()
        {
            // Point all registers in the MMX table above 8(MM7) to $i-8
            for (int i = 0; i < 8; i++)
            {
                // The expression is equivalent to,
                //  $Registers[(int)RegisterTable.MMX + 8 + i] = $Registers[(int)RegisterTable.MMX+i]
                Registers[0x18 + i] = Registers[0x10 + i];
            }
        }
        public RegisterGroup()
        {
            InitialiseMMX();
        }
        public RegisterGroup(Dictionary<XRegCode, byte[]> registers)
        {
            // Create a RegisterGroup from a Dictionary of XRegCodes and byte[]s.             
            // Currently only supports 64 bit GP registers due to other tables having no functional implementation.
            InitialiseMMX();
            foreach (var Register in registers)
            {
                // Copy either 8 bytes from the byte[] dictionary value, or the entire length, which ever is smaller.
                // Without $i < $Register.Value.Length, there would be an error if the value did not have atleast 8 bytes, because $Register.Value[i] would try to access that index.
                // Without $i < 8, there would be an error if the value had more than 8 bytes, because $Registers[(int)Register.Key)[i] only has 8 bytes.
                for (int i = 0; i < 8 && i < Register.Value.Length; i++)
                {
                    Registers[(int)Register.Key][i] = Register.Value[i];
                }
            }
        }
        public RegisterGroup(Dictionary<XRegCode, ulong> registers)
        {
            // Identical to the Dictionary<XRegCode, byte[]> constructor but takes ulong values instead.
            InitialiseMMX();
            foreach (var Register in registers)
            {
                Registers[(int)Register.Key] = BitConverter.GetBytes(Register.Value);
            }
        }
        private RegisterGroup(RegisterGroup toClone)
        {
            // Construct a new RegisterGroup from a $toClone by taking a deep copy of all the registers it contains.
            // If there was no deep copy, C# would shallow copy by default. This would mean that changing a register in $toClone
            // would change that register in $this, because they point to the same array in memory.
            // E.g
            //  Without DeepCopy(),
            //      $toClone = new byte[8]
            //      $toClone[0] = 0x1
            //      $myRegGroup = $toClone
            //      $myRegGroup[0] = 0x2
            //      Now $toClone[0] == 0x2
            //  With DeepCopy(),
            //      $toClone = new byte[8]
            //      $toClone[0] = 0x1
            //      $myRegGroup = $toClone.DeepCopy();
            //      $myRegGroup[0] = 0x2
            //      $toClone[0] == 0x1
            // To put this in context, the Disassembler class takes a DeepCopy of the VM context, if it took a ShallowCopy, (such as $myRegGroup = $toClone), if a register changed in the
            // disassembler context, it would change in the VM context, exactly like in the example above.
            for (int i = 0; i < toClone.Registers.Length; i++)
            {
                Registers[i] = toClone.Registers[i].DeepCopy();
            }
        }
        public RegisterGroup DeepCopy() => new RegisterGroup(this);
        public byte[] this[RegisterTable table, RegisterCapacity cap, XRegCode register]
        {
            // An easy way to fetch a register given its information.
            // Returns a byte[] of length $cap consisting of the bottom $cap bytes of $Registers[$table+$register]
            // Access to upper byte registers such as AH can be done artifically by fetching AX then using $AX[1] as the upper byte.
            get
            {
                byte[] Output = new byte[(int)cap];
                for (int i = 0; i < Output.Length; i++)
                {
                    Output[i] = Registers[(int)register + (int)table][i];
                }
                return Output;
            }
            set
            {
                // If the length of the input byte array is greater than the capacity, data would be lost. Something wrong here would most likely be on my part.
                if (value.Length > ((int)cap))
                {
                    throw new Exception("RegisterGroup.cs Attempt to overflow register in base class");
                }
                // Overwrite the bytes in $Registers[] with $value[]
                for (int i = 0; i < value.Length; i++)
                {
                    Registers[(int)register + (int)table][i] = value[i];
                }
            }
        }
    }
}
