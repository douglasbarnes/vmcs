// Jmp; Jump. Change RIP. Takes relative(jump to $RIP+$x) and absolute(jump to $x) operands.
using debugger.Util;
using System;

namespace debugger.Emulator.Opcodes
{
    public class Jmp : Opcode
    {
        private readonly Condition JmpCondition;
        public Jmp(DecodedTypes.IMyDecoded input, Condition condition = Condition.NONE, OpcodeSettings settings = OpcodeSettings.NONE, bool dwordOnly = false)

            // Determine the mnemonic. If there is a condition it needs to be disassembled, otherwise just JMP
            : base(condition == Condition.NONE ? "JMP" : "J" + Disassembly.DisassembleCondition(condition)
                  , input
                  , settings

                  // BYTEMODE will always indicate that the capacity should be a byte, otherwise requires more work to derive.
                  , (settings | OpcodeSettings.BYTEMODE) == settings ? RegisterCapacity.BYTE : JmpRegCap(dwordOnly))
        {
            // Store the condition
            JmpCondition = condition;
        }
        public override void Execute()
        {
            // Test if the condition is met and jump if so.
            if (TestCondition(JmpCondition))
            {
                // There will only be one operand returned from Fetch().
                ControlUnit.Jump(BitConverter.ToUInt64(Fetch()[0], 0));
            }
        }

        private static RegisterCapacity JmpRegCap(bool dwordOnly)
        {
            // If there is a SIZEOVR prefix present, and the caller did not specifically state that
            // the output would be a dword, it is a WORD value. In any other case it is a DWORD.
            if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR) && !dwordOnly)
            {
                return RegisterCapacity.WORD;
            }
            return RegisterCapacity.DWORD;
        }
    }
}
