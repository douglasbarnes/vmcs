using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Primitives;
using static debugger.ControlUnit;
namespace debugger
{
    class Disassembler
    {
        public ulong DisassemblerPointer = 0;
        private Dictionary<ulong, byte> Memory;
        public Disassembler(Dictionary<ulong, byte> _memory)
        {
            Memory = _memory;
            DisassemblerPointer = 0x100000;
        }

        public Dictionary<ulong, string> Step(int Count)
        {
            ulong SavedBytePointer = BytePointer;
            DisassemblerPointer = BytePointer;
            Dictionary<ulong, string> AddressMnemonicPairs = new Dictionary<ulong, string>();          
            for (int i = 0; i < Count; i++)
            {
                ulong Address = DisassemblerPointer;
                byte bFetched = (Memory.ContainsKey(DisassemblerPointer)) ? Memory[DisassemblerPointer] : (byte)0x90;
                BytePointer++;
                if(Enum.IsDefined(typeof(PrefixByte), (int)bFetched))
                {
                    Prefixes.Add((PrefixByte)bFetched); i++;
                }
                else
                {
                    AddressMnemonicPairs.Add(Address, DisassembleLine(bFetched));
                    Prefixes = new List<PrefixByte>();
                }
                DisassemblerPointer = BytePointer;
            }
            BytePointer = SavedBytePointer;
            return AddressMnemonicPairs;
        }

        public string DisassembleLine(byte bFetched)
        {
            ulong First = BytePointer;
            Opcodes.MyOpcode OpcodeInst = OpcodeLookup.OpcodeTable[1][bFetched].Invoke();
            
            //ModRM DestSrc = OpcodeInst.Input;

            /*  string SourceMnemonic = (OpcodeInst.IsImmediate) ? BitConverter.ToUInt64(OpcodeInst.ImmediateBuffer,0).ToString("X") : "";
              string DestMnemonic = "";

              switch(OpcodeInst.Input.Mod)
              {
                  case 0:
                      DestMnemonic = $"[{DisasRegister((ByteCode)DestSrc.RM, CurrentCapacity)}]";
                      break;
                  case 1:
                  case 2:
                      DestMnemonic = $"[{DisasRegister((ByteCode)DestSrc.RM, CurrentCapacity)} {((DestSrc.Offset < 0) ? "- " : "+ ")}{Math.Abs(DestSrc.Offset).ToString("X")}]";
                      break;
                  case 3:
                      DestMnemonic = DisasRegister((ByteCode)DestSrc.RM, CurrentCapacity);
                      break;
                  case 4:
                      DestMnemonic = $"${((DestSrc.Offset < 0) ? " -" : " +")}0x{Math.Abs(DestSrc.Offset).ToString("X")} (0x{((long)BytePointer + DestSrc.Offset).ToString("X")})";
                      break;
              }
              //  +192/mod*/

            //  return $"{OpcodeInst.ToString()} {DestMnemonic} {((SourceMnemonic == "") ? " " : SourceMnemonic)}";
            OpcodeInst.Disassemble(out string Assembly);
            return $"{Assembly}";
        }

    }

}
