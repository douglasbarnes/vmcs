// The StringOperation provides an ad hoc solution for string operators. String operations act somewhat differently
// to opcodes in general. Every set of inputs is implied by the opcode, furthermore, only the A, SI, and DI registers
// are used. The string operation opcodes are a small subset of the opcode map, and all of which are included in the
// program, but it became very messy handling the same things over and over in each class, which really called for this
// class to be created. It could also be seen as an instance of the DRY(Do not repeat yourself) principle. All string
// operations follow the the capacity rules stated in Opcode.SetRegCap(), such that this has been hard-coded into
// the constructor.
// The direction flag in the control unit dictates whether string operations work forwards or backwards, i.e is the string
// in memory stored like "MYSTRING" or "GNIRTSYM". In the first case it will start at the beginning M and work towards G,
// otherwise it will start at the end M and work towards G at the beginning.
// Only create string operand opcodes that use at least one of: SI, DI, or one of those replaced with the A register.
// Behaviour without complying to this convention is undefined. 
// The REP prefix must be understood when dealing with string operations.
// REP simply means "repeat this instruction until ECX=0, and decrement ECX every time"
// Some pseudocode could be,
//  while($ECX > 0)
//  {
//   Execute();
//   $ECX--;
//  }
// $ECX will stop at zero, i.e never become -1.
// CMPS and SCAS handle REP prefixes slightly differently, such that there are two forms of the prefix,
// each with an extra condition. The two forms are REPZ and REPNZ. Both of these still follow the above,
// but have a different additional condition. REPZ will repeat whilst the zero flag is set. This is checked
// after the instruction is ran, the flag does not have to be set beforehand.
// Pseudo,
//  while($ECX > 0)
//  {
//    Execute();
//    $ECX--;
//    if(ZF == 0) 
//    {
//       break;
//    }
//  }
// The second, REPNZ will do the opposite, and exit afterwards if the zero flag is not set. This is due to the nature
// of the SCAS and CMPS opcodes. SCAS compares a string of bytes at *SI with $A. The derivation of length of these bytes
// can be fonud in the constructor. CMPS compares a string of bytes at *SI with *DI. If two strings are being compared,
// and one character is found to be different, it is immediately apparent that the two strings are not equal, so the whole
// string does not have to be iterated, only until the different. If they are the same, afterwards I would recommend using
// JZ or JE to avoid having to use two lines,(TEST $ECX, $ECX; JZ) because this information is already provided by the result
// of the last comparison. This is concept is generalised by setting the COMPARE bit of the StringOpSettings for an opcode.
// Derived classes must have a different design to Opcode derived classes. There must be no non-constant information assigned
// in the constructor. This must be done in the derived OnInitialise(), and execution in the OnExecute(). This is due to REP
// prefixes, as information must be updated with each execution, such that each execution is treat as a new instruction, but
// without having to re-instance the class. Execute() in the base class will make sure that rep prefies are compatible, calling
// the derived OnInitialise(), then OnExecute() when ready.
using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public enum StringOpSettings
    {
        NONE = 0,
        BYTEMODE = 1,
        COMPARE = 2,
    }
    public abstract class StringOperation : IMyOpcode
    {
        private string Mnemonic;

        // A pointer that holds the final value that $SI and $DI will be set to. This avoids having to set
        // the registers every time.
        protected ulong SrcPtr;
        protected ulong DestPtr;

        // This needs to be stored in the rare event that the register carries out.
        private RegisterCapacity PtrSize;

        // The length of data that will be set at the pointer
        protected RegisterCapacity Capacity;

        // Additional settings
        protected StringOpSettings Settings;

        // These register handles hold the source and destination. If they are handles to DI/SI respectively, they will have the capacity
        // of $PtrSize, otherwise $Capacity.
        private ControlUnit.RegisterHandle Destination;
        private ControlUnit.RegisterHandle Source;

        // Store the value of the A register if present, otherwise the SI register. See constructor
        private readonly byte[] ValueOperand;
        public StringOperation(string mnemonic, XRegCode destination, XRegCode source, StringOpSettings settings)
        {
            Settings = settings;

            // Determine capacity as well as an informal mnemonic convention. I haven't seen it defined anywhere, but
            // in many programs such as GDB and GCC, this is used/accepted.
            // BYTE => append "B" 
            // WORD => append "W"
            // DWORD => append "D"
            // QWORD => append "Q"
            // E.g,
            // SCAS RAX, QWORD PTR [RSI] => SCASQ
            // CMPS BYTE PTR [RDI], BYTE PTR[RSI] => CMPSB
            // However in my program(as with numerous others), the operands will remain but
            // without BYTE PTR or equivalent. This does slightly contradict the purpose of
            // this convention, but I don't expect everyone using the program to know the
            // operands of every string operation off by heart(as each has a constant set of operands).
            if ((Settings | StringOpSettings.BYTEMODE) == Settings)
            {
                Capacity = RegisterCapacity.BYTE;
                mnemonic += 'B';
            }
            else if ((ControlUnit.RexByte | REX.W) == ControlUnit.RexByte)
            {
                Capacity = RegisterCapacity.QWORD;
                mnemonic += 'Q';
            }
            else if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR))
            {
                Capacity = RegisterCapacity.WORD;
                mnemonic += 'W';
            }
            else
            {
                Capacity = RegisterCapacity.DWORD;
                mnemonic += 'D';
            }

            Mnemonic = mnemonic;

            // Determine and store the pointer size(There will always be at least one). E.g [RSI] or [ESI].
            PtrSize = ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD;

            // Create a handle to the destination. If the destination is DI, treat it as a pointer, as DI is always a pointer in a
            // string operation. destination == XRegCode.DI will also be checked later to mimic the same behaviour.
            Destination = new ControlUnit.RegisterHandle(destination, RegisterTable.GP, (destination == XRegCode.DI) ? PtrSize : Capacity);

            // Same as the above 
            Source = new ControlUnit.RegisterHandle(source, RegisterTable.GP, (source == XRegCode.SI) ? PtrSize : Capacity);

            // Convert the two operands into pointers. These will only be used if their XRegCode is SI or DI, but stored regardless.
            // Saving these to a variable means that the registers themselves do not have to be operated on until the execution of
            // the opcode has finished.
            DestPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(Destination.FetchOnce(), 8), 0);
            SrcPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(Source.FetchOnce(), 8), 0);

            // Fetch and store the value operand. This will only be ever used if one operand is the A register. This is because
            // the value of the register is only needed to form the pointers above if the register is SI/DI. There can only
            // be one operand that is the A register, therefore only the value of that operand needs to be stored. If there
            // is no A register, SI will be stored in ValueOperand.
            ValueOperand = (Destination.Code == XRegCode.A) ? Destination.FetchAs(Capacity) : Source.FetchAs(Capacity);

            OnInitialise();
        }
        public void AdjustDI()
        {
            // See summary
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                DestPtr -= (byte)Capacity;
            }
            else
            {
                DestPtr += (byte)Capacity;
            }
        }
        public void AdjustSI()
        {
            // See summary
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                SrcPtr -= (byte)Capacity;
            }
            else
            {
                SrcPtr += (byte)Capacity;
            }
        }
        public virtual List<string> Disassemble()
        {
            // Begin with the mnemonic
            List<string> Output = new List<string>(3) { Mnemonic };

            // If there is a repeat prefix, insert it before the mncmonic.
            if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ))
            {
                // The REP prefix becomes REPZ in a comparison operation because they
                // have the same byte value.

                if ((Settings | StringOpSettings.COMPARE) == Settings)
                {
                    Output[0] = Mnemonic.Insert(0, "REPZ ");
                }
                else
                {
                    Output[0] = Mnemonic.Insert(0, "REP ");
                }
            }

            // See summary 
            if ((Settings | StringOpSettings.COMPARE) == Settings)
            {
                if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ))
                {
                    Output[0] = Mnemonic.Insert(0, "REPZ ");
                }
                else if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ))
                {
                    Output[0] = Mnemonic.Insert(0, "REPNZ ");
                }
            }

            // If not a comparison operation, both REPNZ and REPZ are parsed as REP. REPZ should always be used in this case
            // by convention, but REPNZ is accepted regardless. This is a very hardware-specific thing, it is neither incorrect
            // to ignore it nor accept it.
            else if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ) || ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ))
            {
                Output[0] = Mnemonic.Insert(0, "REP ");
            }

            // Add the operands onto the disassembly.
            Output.Add(Destination.DisassembleOnce());
            Output.Add(Source.DisassembleOnce());

            // Convert to pointers where necessary.
            if (Source.Code == XRegCode.SI)
            {
                Output[2] = $"[{Output[2]}]";
            }
            if (Destination.Code == XRegCode.DI)
            {
                Output[1] = $"[{Output[1]}]";
            }

            return Output;
        }
        public void Execute()
        {
            // Handle a REP prefix.  If the handle has NOJMP set, this is ignored.
            // See summary before reading.
            if (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ) || ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ) && (ControlUnit.CurrentHandle.HandleSettings | HandleParameters.NOJMP) != ControlUnit.CurrentHandle.HandleSettings)
            {
                // Create a handle to ECX
                ControlUnit.RegisterHandle CountHandle = new ControlUnit.RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.DWORD);

                // Initialise $Count to $ECX.               
                uint Count = BitConverter.ToUInt32(CountHandle.FetchOnce(), 0);
                for (; Count > 0; Count--)
                {
                    OnExecute();
                    OnInitialise();

                    // If the operation is a comparison, extra checks have to be done against the zero flag.
                    // This will be ignored when not a comparison.
                    // The conditions can be summarised as
                    // If (REPZ && ZF != 1) break;
                    // If (REPNZ && ZF != 0) break;
                    if ((Settings | StringOpSettings.COMPARE) == Settings
                        && ((ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPZ) && ControlUnit.Flags.Zero != FlagState.ON) // repz and repnz act as normal rep if the opcode isnt cmps or scas=
                           || (ControlUnit.LPrefixBuffer.Contains(PrefixByte.REPNZ) && ControlUnit.Flags.Zero != FlagState.OFF)))
                    {
                        break;
                    }
                }

                // Set $ECX to the new count. Once the instruction is completely exected(all REPs handled), $ECX will either be
                // zero, or $ECX_Before_Instruction - $Number_Of_OnExecute()s
                CountHandle.Set(BitConverter.GetBytes(Count));
            }

            // If no REP prefix, the instruction can be executed as normal.
            else
            {
                OnExecute();
            }

            // If the destination or source were DI/SI, commit the value of their stored pointers to the value of the register.
            if (Destination.Code == XRegCode.DI)
            {
                Destination.Set(BitConverter.GetBytes(PtrSize == RegisterCapacity.QWORD ? DestPtr : (uint)DestPtr));
            }
            if (Source.Code == XRegCode.SI)
            {
                Source.Set(BitConverter.GetBytes(PtrSize == RegisterCapacity.QWORD ? SrcPtr : (uint)SrcPtr));
            }
        }
        protected abstract void OnInitialise();
        protected abstract void OnExecute();
        public List<byte[]> Fetch()
        {
            // There will be two values output.
            List<byte[]> Output = new List<byte[]>(2);

            // If the destination is DI, use it as an effective address.
            if (Destination.Code == XRegCode.DI)
            {
                Output.Add(ControlUnit.Fetch(DestPtr, (int)Capacity));
            }

            // Otherwise add its value.
            else
            {
                Output.Add(ValueOperand);
            }

            // IF the source is SI, use its as an address/
            if (Source.Code == XRegCode.SI)
            {
                Output.Add(ControlUnit.Fetch(SrcPtr, (int)Capacity));
            }

            // Otherwise add its value.            
            else
            {
                Output.Add(ValueOperand);
            }
            return Output;
        }
        public void Set(byte[] data)
        {
            // There are flaws in the caller if this is not already the same size.
            data = Bitwise.Cut(data, (int)Capacity);

            // If the destination handle is to DI, it would be a pointer not the actual value of DI            
            if (Destination.Code == XRegCode.DI)
            {
                ControlUnit.SetMemory(DestPtr, data);
            }

            // Otherwise set the value of the register
            else
            {
                Destination.Set(data);
            }
        }
    }
}
