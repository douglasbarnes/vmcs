using System;
using System.Collections.Generic;
using System.Linq;
using static debugger.Registers;
using System.Threading;
using System.Collections.Concurrent;
using static debugger.Opcodes;

namespace debugger
{
    public class VM
    {
        public VM()
        {
        }
        public void Run(MemorySpace InputMemory)
        {
            

            OpcodeLookup.Refresh();
            Thread t = new Thread(() => ControlUnit.ClockStart(InputMemory));
            t.Start();
        }


    }

    public static class ControlUnit
    {

        public static MemorySpace Memory = new MemorySpace();


        public static ulong BytePointer;
        public static List<PrefixBytes> Prefixes = new List<PrefixBytes>();
        private static byte _opbytes = 1;
        public static void ClockStart(MemorySpace _memory)
        {
            BytePointer = _memory.EntryPoint;
            Memory = _memory;
            RSP = _memory.SegmentMap[".stack"].StartAddr;
            RBP = RSP;
            RIP = Memory.SegmentMap[".main"].StartAddr;
            while (true)
            {
                _step();
            }
        }
        private static void _step()
        {
            BytePointer = RIP;
            byte bFetched = FetchNext();
            if (!_decode(bFetched)) // no op
            {
                _execute(bFetched);
                Prefixes = new List<PrefixBytes>();
            }

            RIP = BytePointer;
        }
        public static byte[] Fetch(ulong _addr, byte _length=1)
        {
            byte[] baOutput = new byte[_length];
            for (byte i = 0; i < _length; i++)
            {
                try
                {
                    baOutput[i] = Memory[_addr+i];
                }
                catch
                {
                    baOutput[i] = 0x00;
                }
            }
            return baOutput;
        }

        public static byte[] Fetch(ulong _addr, RegisterCapacity _regcap)
        {
            return Fetch(_addr, (byte)((int)_regcap / 8));
        }

        public static byte[] FetchRegister(ByteCode bcByteCode, RegisterCapacity WorkingBits)
        {
            switch (WorkingBits)
            {
                case RegisterCapacity.X:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return BitConverter.GetBytes(AX).Take(2).ToArray(); // why this? because c# sucks, Bitconverter.GetBytes(AX) != BitConverter.GetBytes([[whatever is in the get accessor]]) for some reason
                        case ByteCode.B:
                            return BitConverter.GetBytes(BX).Take(2).ToArray();
                        case ByteCode.C:
                            return BitConverter.GetBytes(CX).Take(2).ToArray();
                        case ByteCode.D:
                            return BitConverter.GetBytes(DX).Take(2).ToArray();
                        case ByteCode.AH:
                            return BitConverter.GetBytes(SP).Take(2).ToArray();
                        case ByteCode.BH:
                            return BitConverter.GetBytes(BP).Take(2).ToArray();
                        case ByteCode.CH:
                            return BitConverter.GetBytes(DI).Take(2).ToArray();
                        case ByteCode.DH:
                            return BitConverter.GetBytes(SI).Take(2).ToArray();
                    }
                    break;
                case RegisterCapacity.E:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return BitConverter.GetBytes(EAX);
                        case ByteCode.B:
                            return BitConverter.GetBytes(EBX);
                        case ByteCode.C:
                            return BitConverter.GetBytes(ECX);
                        case ByteCode.D:
                            return BitConverter.GetBytes(EDX);
                        case ByteCode.AH:
                            return BitConverter.GetBytes(ESP);
                        case ByteCode.BH:
                            return BitConverter.GetBytes(EBP);
                        case ByteCode.CH:
                            return BitConverter.GetBytes(ESI);
                        case ByteCode.DH:
                            return BitConverter.GetBytes(EDI);

                    }
                    break;
                case RegisterCapacity.R:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return BitConverter.GetBytes(RAX);
                        case ByteCode.B:
                            return BitConverter.GetBytes(RBX);
                        case ByteCode.C:
                            return BitConverter.GetBytes(RCX);
                        case ByteCode.D:
                            return BitConverter.GetBytes(RDX);
                        case ByteCode.AH:
                            return BitConverter.GetBytes(RSP);
                        case ByteCode.BH:
                            return BitConverter.GetBytes(RBP);
                        case ByteCode.CH:
                            return BitConverter.GetBytes(RSI);
                        case ByteCode.DH:
                            return BitConverter.GetBytes(RDI);

                    }
                    break;
                case RegisterCapacity.B:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return (AL);
                        case ByteCode.B:
                            return (BL);
                        case ByteCode.C:
                            return (CL);
                        case ByteCode.D:
                            return (DL);
                        case ByteCode.AH:
                            return (AH);
                        case ByteCode.BH:
                            return (BH);
                        case ByteCode.CH:
                            return (CH);
                        case ByteCode.DH:
                            return (DH);
                        default:
                            throw new Exception();
                    }
            }
            throw new Exception();
        }

        public static void SetRegister(ByteCode bcByteCode, byte[] baData, RegisterCapacity WorkingBits)
        {
            switch (WorkingBits)
            {
                case RegisterCapacity.X:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            AX = BitConverter.ToUInt16(baData, 0);
                            break;
                        case ByteCode.B:
                            BX = BitConverter.ToUInt16(baData, 0);
                            break;
                        case ByteCode.C:
                            CX = BitConverter.ToUInt16(baData, 0);
                            break;
                        case ByteCode.D:
                            DX = BitConverter.ToUInt16(baData, 0);
                            break;
                    }
                    break;
                case RegisterCapacity.E:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            EAX = BitConverter.ToUInt32(baData, 0);
                            break;
                        case ByteCode.B:
                            EBX = BitConverter.ToUInt32(baData, 0);
                            break;
                        case ByteCode.C:
                            ECX = BitConverter.ToUInt32(baData, 0);
                            break;
                        case ByteCode.D:
                            EDX = BitConverter.ToUInt32(baData, 0);
                            break;
                    }
                    break;
                case RegisterCapacity.R:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            RAX = BitConverter.ToUInt64(baData, 0);
                            break;
                        case ByteCode.B:
                            RBX = BitConverter.ToUInt64(baData, 0);
                            break;
                        case ByteCode.C:
                            RCX = BitConverter.ToUInt64(baData, 0);
                            break;
                        case ByteCode.D:
                            RDX = BitConverter.ToUInt64(baData, 0);
                            break;
                    }
                    break;
                case RegisterCapacity.B:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            AL = baData[0];
                            break;
                        case ByteCode.B:
                            BL = baData[0];
                            break;
                        case ByteCode.C:
                            CL = baData[0];
                            break;
                        case ByteCode.D:
                            DL = baData[0];
                            break;
                        case ByteCode.AH:
                            AH = baData[0];
                            break;
                        case ByteCode.BH:
                            BH = baData[0];
                            break;
                        case ByteCode.CH:
                            CH = baData[0];
                            break;
                        case ByteCode.DH:
                            DH = baData[0];
                            break;
                        default:
                            throw new Exception();
                    }
                    break;
            } 
        }

        public static void SetRegister(ByteCode bcByteCode, byte bData)
        {
            SetRegister(bcByteCode, new byte[] { bData }, RegisterCapacity.B);
        }

        public static void SetRegister(ByteCode bcByteCode, ulong lData)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(lData), RegisterCapacity.R);
        }

        public static void SetRegister(ByteCode bcByteCode, uint iData)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(iData), RegisterCapacity.E);
        }

        public static void SetRegister(ByteCode bcByteCode, ushort sData)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(sData), RegisterCapacity.X);
        }

        public static void SetMemory(ulong lAddress, byte[] baData)
        {
            for (uint iOffset = 0; iOffset < baData.Length; iOffset++)
            {
                if (Memory.ContainsAddr(lAddress + iOffset))
                {
                    Memory[lAddress + iOffset] = baData[iOffset];
                } else
                {
                    Memory.Set(lAddress+iOffset, baData[iOffset]);
                }
            }
            
        }

        public static void SetMemory(ulong lAddress, byte bData)
        {
            if (Memory.ContainsAddr(lAddress))
            {
                Memory[lAddress] = bData;
            }
            else
            {
                Memory.Set(lAddress, bData);
            }
        }

        public static void SetMemory(ulong lDestAddr, ulong lSrcAddr, byte bLength = 1)
        {
            SetMemory(lDestAddr, Fetch(lSrcAddr, bLength));
        }

        public static byte FetchNext(bool Increase=true)
        {
            byte bFetched = Fetch(BytePointer, 1)[0];
            if (Increase)
            {
                BytePointer++;
            }
            return bFetched;
            
        }

        public static byte[] FetchNext(byte bLength)
        {
            byte[] baOutput = new byte[bLength];
            for (int i = 0; i < bLength; i++)
            {
                baOutput[i] = FetchNext();
            }
            return baOutput;
        }

        public static byte[] FetchNext(RegisterCapacity _regcap)
        {

            return FetchNext((byte)((int)(_regcap) / 8));
        }
        private static bool _decode(byte bFetched)
        {
            if (bFetched == 0x0F)
            {
                _opbytes = 2;
            }
            else if (Enum.IsDefined(typeof(PrefixBytes), (int)bFetched)) {
                Prefixes.Add((PrefixBytes)bFetched);
                return true;
            }
            return false;
        }
        private static void _execute(byte bFetched)
        {
            OpcodeLookup.OpcodeTable[_opbytes][bFetched].Invoke();                   
        }
        public static ModRM ModRMDecode(byte bModRM, bool MultiDef=false)
        {
            ModRM Output = new ModRM();
            string sBits = Util.Bitwise.GetBits(bModRM);
            string sMod = sBits.Substring(0,2); // pointer, offset pointer, or reg
            string sReg = sBits.Substring(2, 3);
            string sRM = sBits.Substring(5, 3);

            if(MultiDef) // reg diff meaning in multi def
            {
                switch (sReg)
                {
                    case "000":
                        Output.lReg = 0;
                        break;
                    case "001":
                        Output.lReg = 1;
                        break;
                    case "010":
                        Output.lReg = 2;
                        break;
                    case "011":
                        Output.lReg = 3;
                        break;
                    case "100":
                        Output.lReg = 4;
                        break;
                    case "101":
                        Output.lReg = 5;
                        break;
                    case "110":
                        Output.lReg = 6;
                        break;
                    case "111":
                        Output.lReg = 7;
                        break;
                }
            } else
            {
                switch (sReg)
                {
                    case "000":
                        Output.lReg = (ulong)ByteCode.A;
                        break;
                    case "001":
                        Output.lReg = (ulong)ByteCode.C;
                        break;
                    case "010":
                        Output.lReg = (ulong)ByteCode.D;
                        break;
                    case "011":
                        Output.lReg = (ulong)ByteCode.B;
                        break;
                    case "100":
                        Output.lReg = (ulong)ByteCode.AH;
                        break;
                    case "101":
                        Output.lReg = (ulong)ByteCode.CH;
                        break;
                    case "110":
                        Output.lReg = (ulong)ByteCode.DH;
                        break;
                    case "111":
                        Output.lReg = (ulong)ByteCode.BH;
                        break;
                }
            }
            

            if (sMod == "11")
            {
                // direct register
                Output.IsAddress = false;
                switch (sRM)
                {
                    case "000":
                        Output.lMod = (ulong)ByteCode.A;
                        break;
                    case "001":
                        Output.lMod = (ulong)ByteCode.C;
                        break;
                    case "010":
                        Output.lMod = (ulong)ByteCode.D;
                        break;
                    case "011":
                        Output.lMod = (ulong)ByteCode.B;
                        break;
                    case "100":
                        Output.lMod = (ulong)ByteCode.AH;
                        break;
                    case "101":
                        Output.lMod = (ulong)ByteCode.CH;
                        break;
                    case "110":
                        Output.lMod = (ulong)ByteCode.DH;
                        break;
                    case "111":
                        Output.lMod = (ulong)ByteCode.BH;
                        break;

                }
            }
            else
            {
                uint iOffset = 0;
                Output.IsAddress = true;
                if (sMod == "01") //1B
                {
                    iOffset = FetchNext();
                }
                else if (sMod == "10") // 1W
                {
                    iOffset = BitConverter.ToUInt32(FetchNext(4), 0);
                }

                switch (sRM)
                {
                    case "000":
                        Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.A, RegisterCapacity.R), 0) + iOffset;
                        break;
                    case "001":
                        Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.C, RegisterCapacity.R), 0) + iOffset;
                        break;
                    case "010":
                        Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.D, RegisterCapacity.R), 0) + iOffset;
                        break;
                    case "011":
                        Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.B, RegisterCapacity.R), 0) + iOffset;
                        break;
                    case "100":
                        //SIB! //if (sRM == "100" && sMod != "11") { Output.bSIB = FetchNext(); BytePointer++; }
                        Output.lMod = SIBDecode();
                        break;
                    case "110":
                        Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.DH, RegisterCapacity.R), 0) + iOffset;
                        break;
                    case "111":
                        Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.BH, RegisterCapacity.R), 0) + iOffset;
                        break;
                    default:
                        if (sMod == "00") // either displacement32 if it is just a pointer(mod=00)
                        {
                            Output.lMod = BitConverter.ToUInt32(Fetch(BytePointer, 4), 0);
                        }
                        else // or ebp + disp, need to use SIb to get ebp without a displacement(mod!=00)
                        {
                            Output.lMod = BitConverter.ToUInt64(FetchRegister(ByteCode.BH, RegisterCapacity.R), 0) + iOffset;
                        }
                        break;
                }
            }
            
            return Output;

        }

        public static ulong SIBDecode()
        {
            
            string sSIB = Util.Bitwise.GetBits(FetchNext());
            string sSSBits = sSIB.Substring(0, 2);
            string sIndexBits = sSIB.Substring(2, 3);
            string sBase = sSIB.Substring(5, 3);
            ulong lBase = 0;
            ulong lSrcVal = 0;
            switch(sBase)
            {
                case "000":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.A, RegisterCapacity.R),0);
                    break;
                case "001":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.C, RegisterCapacity.R), 0);
                    break;
                case "010":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.D, RegisterCapacity.R), 0);
                    break;
                case "011":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.B, RegisterCapacity.R), 0);
                    break;
                case "100":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.AH, RegisterCapacity.R), 0);
                    break;
                case "101":
                    lBase = BitConverter.ToUInt32(FetchNext(4),0); //4 bytes, int32
                    break;
                case "110":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.DH, RegisterCapacity.R), 0);
                    break;
                case "111":
                    lBase = BitConverter.ToUInt64(FetchRegister(ByteCode.BH, RegisterCapacity.R), 0);
                    break;
            }

            byte bScale = 0; // what to times the scaled index by(ssbits)
            switch(sSSBits)
            {
                case "00":
                    bScale = 1;
                    break;
                case "01":
                    bScale = 2;                    
                    break;
                case "10":
                    bScale = 4;
                    break;
                case "11":
                    bScale = 8;
                    break;
                default: throw new Exception();
            }

            switch (sIndexBits)
            {
                case "000":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.A, RegisterCapacity.R),0); // times it later!
                    break;
                case "001":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.C, RegisterCapacity.R), 0);
                    break;
                case "010":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.D, RegisterCapacity.R), 0);
                    break;
                case "011":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.B, RegisterCapacity.R), 0);
                    break;
                case "100":
                    // NOT !! lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.AH, RegisterCapacity.R), 0);
                    // none! do nothing
                    break;
                case "101":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.CH, RegisterCapacity.R), 0);
                    break;
                case "110":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.DH, RegisterCapacity.R), 0);
                    break;
                case "111":
                    lSrcVal = BitConverter.ToUInt64(FetchRegister(ByteCode.BH, RegisterCapacity.R), 0);
                    break;
                default: throw new Exception();
            }

            if(sSSBits != "00" && sIndexBits == "100") // special rule, add rbp 
            {
                lSrcVal += BitConverter.ToUInt64(FetchRegister(ByteCode.BH, RegisterCapacity.R),0);
            }

            return (lSrcVal * bScale) + lBase;

        }
    }
    public class MemorySpace
    {
        
        private Dictionary<ulong, byte> _memory = new Dictionary<ulong, byte>();
        public Dictionary<string, Segment> SegmentMap = new Dictionary<string, Segment>();
        public ulong Size;
        public ulong EntryPoint;
        public ulong LastAddr;

        public class Segment
        {
            public ulong StartAddr;
            public ulong LastAddr;
            public byte[] baData = null;
        }

        public MemorySpace() { }
        public MemorySpace(Dictionary<ulong, byte> _inputmemory)
        {
            _memory = _inputmemory;
            Size = (ulong)_inputmemory.LongCount();
            ulong _tmphighest = 0;
            ulong _tmplowest = ulong.MaxValue;
            foreach (ulong Address in _inputmemory.Keys)
            {
                if(Address > _tmphighest)
                {
                    _tmphighest = Address;
                }
                if(Address < _tmplowest)
                {
                    _tmplowest = Address;
                }
            }
            LastAddr = _tmphighest;
            EntryPoint = _tmplowest;
        } //NEEDS UPDATING DON TUSE

        public MemorySpace(byte[] _rawinputmemory)
        {
            EntryPoint = 0x100000;
            SegmentMap.Add(".main", new Segment() { StartAddr = EntryPoint, LastAddr = EntryPoint + (ulong)_rawinputmemory.LongLength, baData = _rawinputmemory });           
            SegmentMap.Add(".heap", new Segment() { StartAddr = 0x400001, LastAddr = 0x0 });
            SegmentMap.Add(".stack", new Segment() { StartAddr = 0x800000, LastAddr = 0x0 });

            foreach (Segment seg in SegmentMap.Values)
            {
                if (seg.baData == null)
                {
                    _memory.Add(seg.StartAddr, 0x0);
                }
                else
                {
                    for (ulong i = 0; i < (seg.LastAddr-seg.StartAddr); i++)
                    {
                        _memory.Add(i + seg.StartAddr, seg.baData[i]);
                    }
                }
                
            }            
        }


       

        public bool ContainsAddr(ulong lAddress)
        {
            return (_memory.ContainsKey(lAddress)) ? true : false;
        }

        public void Set(string sSegName, ulong lOffset, byte bData)
        {
            Set(SegmentMap[sSegName].StartAddr + lOffset, bData);
        }
        public void Set(ulong lAddress, byte bData)
        {
            if (ContainsAddr(lAddress))
            {
                _memory[lAddress] = bData;
            } else
            {
                _memory.Add(lAddress, bData);
            }
            
        }
        public byte this[ulong Address]
        {
            get
            {
                return _memory[Address];
            }

            set
            {
                _memory[Address] = value;
            }
        }

        public void Map(Dictionary<ulong, byte> _inputmemory)
        {
            _memory = _inputmemory;
        }
    }

    public struct ModRM
    {
        public ulong lMod;
        public ulong lReg;
        public bool IsAddress;
    }

    public static class Registers
    {
        public enum PrefixBytes
        {
            ADDR32 = 0x67,
            SIZEOVR=0x66,
            REXW = 0x48
        }
        public enum RegisterCapacity
        {
            B = 8,
            X = 16,
            E = 32,
            R = 64
        }

        public enum ByteCode
        {
            A = 0xC0,
            C = 0xC1,
            D = 0xC2,
            B = 0xC3,
            AH = 0xC4,
            DH = 0xC5,
            CH = 0xC6,
            BH = 0xC7
        }

        

        public static Register64 RIP = 0x0000000000000000;


        public static Register64 RAX = 0x0000000000000000;
        public static Register64 RBX = 0x0000000000000000;
        public static Register64 RCX = 0x0000000000000000;
        public static Register64 RDX = 0x0000000000000000;
        public static Register64 RSP = 0x0000000000000000;
        public static Register64 RBP = 0x0000000000000000;
        public static Register64 RSI = 0x0000000000000000;
        public static Register64 RDI = 0x0000000000000000;

        public static Register32 EAX { get { return BitConverter.ToUInt32(_getlowerbytes(RAX, 4),0); } set { RAX = _setlowerbytes(EAX, value, 4); } }
        public static Register32 EBX { get { return BitConverter.ToUInt32(_getlowerbytes(RBX, 4),0); } set { RBX = _setlowerbytes(EBX, value, 4); } }
        public static Register32 ECX { get { return BitConverter.ToUInt32(_getlowerbytes(RCX, 4),0); } set { RCX = _setlowerbytes(ECX, value, 4); } }
        public static Register32 EDX { get { return BitConverter.ToUInt32(_getlowerbytes(RDX, 4),0); } set { RDX =_setlowerbytes(EDX, value, 4); } }
        public static Register32 ESP { get { return BitConverter.ToUInt32(_getlowerbytes(RSP, 4), 0); } set { RSP = _setlowerbytes(RSP, value, 4); } }
        public static Register32 EBP { get { return BitConverter.ToUInt32(_getlowerbytes(RBP, 4), 0); } set { RBP = _setlowerbytes(RBP, value, 4); } }
        public static Register32 ESI { get { return BitConverter.ToUInt32(_getlowerbytes(RSI, 4), 0); } set { RSI = _setlowerbytes(RSI, value, 4); } }
        public static Register32 EDI { get { return BitConverter.ToUInt32(_getlowerbytes(RDI, 4), 0); } set { RDI = _setlowerbytes(RDI, value, 4); } }

        public static Register16 AX { get { return BitConverter.ToUInt16(_getlowerbytes(RAX, 2),0); } set { RAX = _setlowerbytes(RAX, value, 2); } }
        public static Register16 BX { get { return BitConverter.ToUInt16(_getlowerbytes(RBX, 2),0); } set { RBX = _setlowerbytes(RBX, value, 2); } }
        public static Register16 CX { get { return BitConverter.ToUInt16(_getlowerbytes(RCX, 2),0); } set { RCX = _setlowerbytes(RCX, value, 2); } }
        public static Register16 DX { get { return BitConverter.ToUInt16(_getlowerbytes(RDX, 2),0); } set { RDX = _setlowerbytes(RDX, value, 2); } }
        public static Register16 SP { get { return BitConverter.ToUInt16(_getlowerbytes(RSP, 2), 0); } set { RSP = _setlowerbytes(RSP, value, 2); } }
        public static Register16 BP { get { return BitConverter.ToUInt16(_getlowerbytes(RBP, 2), 0); } set { RBP = _setlowerbytes(RBP, value, 2); } }
        public static Register16 SI { get { return BitConverter.ToUInt16(_getlowerbytes(RSI, 2), 0); } set { RSI = _setlowerbytes(RSI, value, 2); } }
        public static Register16 DI { get { return BitConverter.ToUInt16(_getlowerbytes(RDI, 2), 0); } set { RDI = _setlowerbytes(RDI, value, 2); } }

        public static Register8 AH { get { return _getlowerbytes(RAX, 2)[1]; } set { RAX = _setlowerbytes(RAX, value, 1, 1); } }
        public static Register8 AL { get { return _getlowerbytes(RAX, 1)[0]; } set { RAX = _setlowerbytes(RAX, value, 1); } }
        public static Register8 BH { get { return _getlowerbytes(RBX, 2)[1]; } set { RBX = _setlowerbytes(RBX, value, 1, 1); } }
        public static Register8 BL { get { return _getlowerbytes(RBX, 1)[0]; } set { RBX = _setlowerbytes(RBX, value, 1); } }
        public static Register8 CH { get { return _getlowerbytes(RCX, 2)[1]; } set { RCX = _setlowerbytes(RCX, value, 1, 1); } }
        public static Register8 CL { get { return _getlowerbytes(RCX, 1)[0]; } set { RCX = _setlowerbytes(RCX, value, 1); } }
        public static Register8 DH { get { return _getlowerbytes(RDX, 2)[1]; } set { RDX = _setlowerbytes(RDX, value, 1, 1); } }
        public static Register8 DL { get { return _getlowerbytes(RDX, 1)[0]; } set { RDX = _setlowerbytes(RDX, value, 1); } }

        private static byte[] _getlowerbytes(Register64 lValue, int iCount)
        {
            byte[] baBytes = BitConverter.GetBytes(lValue);
            return baBytes.Take(iCount).ToArray();
        }


        private static Register64 _setlowerbytes(byte[] baInputBytes, byte[] baBytesToSet, int iCount, int iOffset=0)
        {
            if (baBytesToSet.Length > iCount)
            {
                throw new OverflowException();
            }
            byte[] baBuffer = new byte[8];
            Array.Copy(baInputBytes, baBuffer, iCount);           
            Array.Copy(baBytesToSet, 0, baBuffer, iOffset, iCount);
            return BitConverter.ToUInt64(baBuffer, 0);
        }

    } 

    public class Register64
    {
        private ulong lValue;
        public Register64(ulong R)
        {
            lValue = R;
        }

        public byte this[int i] => this[i];
        public static implicit operator ulong(Register64 R)
        {
            return R.lValue;
        }
        public static implicit operator byte[] (Register64 R)
        {
            return BitConverter.GetBytes(R.lValue);
        }

        public static implicit operator Register64(ulong lVal)
        {
            return new Register64(lVal);
        }
    }

    public class Register32
    {
        private uint iValue;
        public Register32(uint R)
        {
            iValue = R;
        }

        public byte this[int i] => this[i];
        public static implicit operator uint(Register32 R)
        {
            return R.iValue;
        }

        public static implicit operator byte[] (Register32 R)
        {
            return BitConverter.GetBytes(R.iValue);
        }

        public static implicit operator Register32(uint iVal)
        {
            return new Register32(iVal);
        }
    }

    public class Register16
    {
        private ushort sValue;
        public Register16(ushort R)
        {
            sValue = R;
        }

        public byte this[int i] => this[i];
        public static implicit operator uint(Register16 R)
        {
            return R.sValue;
        }

        public static implicit operator byte[] (Register16 R)
        {
            return BitConverter.GetBytes(R.sValue);
        }

        public static implicit operator Register16(ushort sVal)
        {
            return new Register16(sVal);
        }
    }
    public class Register8
    {
        private byte bValue;
        public Register8(byte R)
        {
            bValue = R;
        }

        public byte this[int i] => this[i];
        public static implicit operator byte(Register8 R)
        {
            return R.bValue;
        }

        public static implicit operator byte[] (Register8 R)
        {
            return new byte[] { R.bValue };
        }

        public static implicit operator Register8(byte bVal)
        {
            return new Register8(bVal);
        }
    }

}
