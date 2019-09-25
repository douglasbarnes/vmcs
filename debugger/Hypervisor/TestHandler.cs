using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using debugger.Emulator;
using debugger.Util;
using debugger.Logging;
namespace debugger.Hypervisor
{
    static class TestHandler
    {
        private readonly static Dictionary<string, (RegisterTable, XRegCode)> RegisterDecodeTable = new Dictionary<string, (RegisterTable, XRegCode)>() {
            {"A"  ,(RegisterTable.GP, XRegCode.A) },     {"B" ,(RegisterTable.GP, XRegCode.B) },    {"C"  ,(RegisterTable.GP, XRegCode.C) },   {"D",  (RegisterTable.GP, XRegCode.D) },
            {"SP" ,(RegisterTable.GP, XRegCode.SP) },    {"BP",(RegisterTable.GP, XRegCode.BP) },   {"SI" ,(RegisterTable.GP, XRegCode.SI) },  {"DI", (RegisterTable.GP, XRegCode.DI) },
            {"R8" ,(RegisterTable.GP, XRegCode.R8) },    {"R9",(RegisterTable.GP, XRegCode.R9) },   {"R10",(RegisterTable.GP, XRegCode.R10) }, {"R11",(RegisterTable.GP, XRegCode.R11) },
            {"R12",(RegisterTable.GP, XRegCode.R12) },   {"R13",(RegisterTable.GP, XRegCode.R13) }, {"R14",(RegisterTable.GP, XRegCode.R14) }, {"R15",(RegisterTable.GP, XRegCode.R15) },
            {"MM0",(RegisterTable.MMX, XRegCode.A) },    {"MM1",(RegisterTable.MMX, XRegCode.C) },  {"MM2",(RegisterTable.MMX, XRegCode.D) },  {"MM3",(RegisterTable.MMX, XRegCode.B) },
            {"MM4",(RegisterTable.MMX, XRegCode.SP) },   {"MM5",(RegisterTable.MMX, XRegCode.BP) }, {"MM6",(RegisterTable.MMX, XRegCode.SI) }, {"MM7",(RegisterTable.MMX, XRegCode.DI) },
            {"YMM0",(RegisterTable.SSE, XRegCode.A) },   {"YMM1",(RegisterTable.SSE, XRegCode.C) }, {"YMM2",(RegisterTable.SSE, XRegCode.D) }, {"YMM3",(RegisterTable.SSE, XRegCode.B) },
            {"YMM4",(RegisterTable.SSE, XRegCode.SP) },  {"YMM5",(RegisterTable.SSE, XRegCode.BP) },{"YMM6",(RegisterTable.SSE, XRegCode.SI) },{"YMM7",(RegisterTable.SSE, XRegCode.DI) },
        };
        protected struct CheckpointSubresult
        {
            public bool Passed;
            public string Expected;
            public string Found;
        }
        protected class TestcaseResult
        {
            public bool Passed = true;
            public Dictionary<Checkpoint, List<CheckpointSubresult>> CheckpointOutputs = new Dictionary<Checkpoint, List<CheckpointSubresult>>();
            public XElement ToXML(string name)
            {
                XElement Output = new XElement("TestcaseResult", new XAttribute("name", name));
                Output.SetAttributeValue("result", Passed ? "Passed" : "Failed");
                foreach (var CheckpointOutput in CheckpointOutputs)
                {
                    XElement CheckpointResult = new XElement("CheckpointResult", new XAttribute("Tag", CheckpointOutput.Key.Tag));
                    foreach (var SubCheckpointOutput in CheckpointOutput.Value)
                    {
                        CheckpointResult.Add(
                            new XElement("SubCheckpointResult", new XAttribute("result", SubCheckpointOutput.Passed ? "passed" : "failed")
                                ,new XElement("Expected", SubCheckpointOutput.Expected)
                                ,new XElement("Found", SubCheckpointOutput.Found)));
                    }
                    Output.Add(CheckpointResult);
                }
                return Output;
            }
        }
        protected class TestcaseObject
        {
            public MemorySpace Memory;
            public Dictionary<ulong, Checkpoint> Checkpoints = new Dictionary<ulong, Checkpoint>();
        }
        protected class Checkpoint
        {
            public string Tag;
            public List<TestRegister> ExpectedRegisters = new List<TestRegister>();
            public List<TestMemoryAddress> ExpectedMemory = new List<TestMemoryAddress>();
            public FlagSet ExpectedFlags = new FlagSet();
            public bool TestFlags = false;
        }
        protected struct TestRegister
        {
            public ControlUnit.RegisterHandle Register;
            public ulong ExpectedValue;
            public bool TryParse(XElement testCriteria)
            {                
                try
                {
                    (RegisterTable Table, XRegCode Code) = RegisterDecodeTable[testCriteria.Attribute("id").Value];
                    Register = new ControlUnit.RegisterHandle(Code, Table, (RegisterCapacity)(int)testCriteria.Attribute("size"));
                    if (testCriteria.Value.Length > (int)Register.Size * 2)
                    {
                        return false;
                    }
                    ExpectedValue = Convert.ToUInt64(testCriteria.Value, 16);                    
                    return true;
                }
                catch
                {
                    return false;                    
                }
            }
        }
        protected struct TestMemoryAddress
        {
            public ControlUnit.RegisterHandle OffsetRegister;
            public long Offset;
            public byte[] ExpectedBytes;
            public bool TryParse(XElement input, out (LogCode, string) ErrorInfo)
            {
                if (!TryParseHex(input.Value, out ExpectedBytes))
                {
                    ErrorInfo = (LogCode.TESTCASE_PARSEFAIL, "Invalid value of expected memory.");
                    return false;
                }
                if (input.Attribute("offset_register") != null)
                {
                    (RegisterTable, XRegCode) Result;
                    if (!RegisterDecodeTable.TryGetValue(input.Attribute("offset_register").Value, out Result))
                    {
                        ErrorInfo = (LogCode.TESTCASE_PARSEFAIL, "Invalid value of offset register.");
                        return false;
                    }
                    OffsetRegister = new ControlUnit.RegisterHandle(Result.Item2, Result.Item1, RegisterCapacity.QWORD);
                }
                if (input.Attribute("offset") != null)
                {
                    byte[] OffsetBytes;
                    if (!TryParseHex(input.Attribute("offset").Value, out OffsetBytes))
                    {
                        ErrorInfo = (LogCode.TESTCASE_PARSEFAIL, "Invalid hex bytes in memory offset attribute");
                        return false;
                    }
                    Offset = BitConverter.ToInt64(Bitwise.SignExtend(Bitwise.ReverseEndian(OffsetBytes), 8), 0);
                    if (Offset < 0 && OffsetRegister == null)
                    {
                        ErrorInfo = (LogCode.TESTCASE_BADCHECKPOINT, "Cannot have a negative memory offset without a register to offset it");
                        return false;
                    }
                }
                else if (OffsetRegister == null)
                {
                    ErrorInfo = (LogCode.TESTCASE_BADCHECKPOINT, "Must have an offset attribute, offset_register attribute, or both");
                    return false;
                }
                ErrorInfo = (LogCode.NONE, null);
                return true;
            }
        }
        private static readonly Dictionary<string, TestcaseObject> Testcases = new Dictionary<string, TestcaseObject>();
        private class TestingEmulator : HypervisorBase
        {
            readonly TestcaseObject CurrentTestcase;
            public TestingEmulator(TestcaseObject testcase) :  //crazy that we even have to deep copy here.. otherwise the same instance of new context is used....
                base("TestingEmulator", 
                    (new Context(testcase.Memory) { Breakpoints = testcase.Checkpoints.Keys.ToList(), Flags = new FlagSet(FlagState.OFF)}).DeepCopy()) 
            {
                CurrentTestcase = testcase;
            }
            public async Task<TestcaseResult> RunTestcase()
            {
                TestcaseResult Result = new TestcaseResult();                
                for (int CheckpointNum = 0; CheckpointNum < CurrentTestcase.Checkpoints.Count; CheckpointNum++)
                {
                    await RunAsync();    
                    Context Snapshot = Handle.ShallowCopy();
                    Checkpoint CurrentCheckpoint;
                    if (!CurrentTestcase.Checkpoints.TryGetValue(Snapshot.InstructionPointer, out CurrentCheckpoint))
                    {
                        Logger.Log(LogCode.TESTCASE_RUNTIME, "");
                        break;
                    }
                    List<CheckpointSubresult> CurrentSubresults = new List<CheckpointSubresult>();                    
                    foreach (TestRegister testReg in CurrentCheckpoint.ExpectedRegisters)
                    {
                        string Mnemonic = testReg.Register.DisassembleOnce();
                        byte[] ActualValue = testReg.Register.FetchOnce();
                        bool RegistersEqual = ActualValue.CompareTo(Bitwise.Cut(BitConverter.GetBytes(testReg.ExpectedValue), (int)testReg.Register.Size), false) == 0;
                        Result.Passed &= RegistersEqual;
                        CurrentSubresults.Add(
                            new CheckpointSubresult
                            {
                                Passed = RegistersEqual,
                                Expected = $"${Mnemonic}=0x{testReg.ExpectedValue.ToString("X")}",
                                Found = $"${Mnemonic}=0x{BitConverter.ToUInt64(Bitwise.ZeroExtend(testReg.Register.FetchOnce(),8),0).ToString("X")}"
                            });
                    }
                    foreach (TestMemoryAddress testMem in CurrentCheckpoint.ExpectedMemory)
                    {
                        ulong ExactAddress = 0;;
                        string Mnemonic = "";
                        if (testMem.OffsetRegister != null)
                        {
                            Mnemonic = testMem.OffsetRegister.DisassembleOnce();
                            if(testMem.Offset != 0)
                            {
                                Mnemonic += (testMem.Offset > 0) ? "+" : "-";
                            } 
                            ExactAddress += BitConverter.ToUInt64(testMem.OffsetRegister.FetchOnce(), 0);
                        }
                        if(testMem.Offset != 0)
                        {
                            Mnemonic += $"0x{Math.Abs(testMem.Offset).ToString("X")}";
                        }
                        ExactAddress += (ulong)testMem.Offset;
                        bool MemoryEqual = true;
                        byte[] FoundBytes = new byte[testMem.ExpectedBytes.Length];
                        for (uint ByteOffset = 0; ByteOffset < testMem.ExpectedBytes.Length; ByteOffset++)
                        {
                            FoundBytes[ByteOffset] = Snapshot.Memory[ExactAddress + ByteOffset];
                            if (Snapshot.Memory[ExactAddress + ByteOffset] != testMem.ExpectedBytes[ByteOffset])
                            {
                                MemoryEqual = false;
                            }                            
                        }
                        Result.Passed &= MemoryEqual;
                        CurrentSubresults.Add(
                            new CheckpointSubresult
                            {
                                Passed = MemoryEqual,
                                Expected = $"[{Mnemonic}]={{{ParseBytes(testMem.ExpectedBytes)}}}",
                                Found = $"[{Mnemonic}]={{{ParseBytes(FoundBytes)}}}"
                            });
                    }
                    if(CurrentCheckpoint.TestFlags)
                    {
                        bool FlagsEqual = CurrentCheckpoint.ExpectedFlags.EqualsOrUndefined(ControlUnit.Flags);
                        Result.Passed &= FlagsEqual;
                        CurrentSubresults.Add(
                        new CheckpointSubresult
                        {
                            Passed = FlagsEqual,
                            Expected = CurrentCheckpoint.ExpectedFlags.ToString(),
                            Found = ControlUnit.Flags.And(CurrentCheckpoint.ExpectedFlags).ToString()
                        });
                    }         
                    Result.CheckpointOutputs.Add(CurrentCheckpoint, CurrentSubresults);
                }
                return Result;
            } 
        }
        static TestHandler()
        {
            foreach (string FilePath in Directory.GetFiles("Testcases")) //each testcase in file
            {
                if (FilePath.Substring(FilePath.Length - 4) == ".xml") //if its xml file(light filter, invalid xml files still cause errors)
                {
                    XDocument TestcaseFile;
                    try
                    {
                        TestcaseFile = XDocument.Load(FilePath);
                    }
                    catch(Exception e)
                    {
                        Logger.Log(LogCode.TESTCASE_IOERROR, new string[] { FilePath, e.Message });
                        continue;
                    }
                    foreach (XElement InputTestcase in TestcaseFile.Elements("Testcase"))//for each <testcase> in the file
                    {
                        if (InputTestcase.Attribute("noparse") != null && InputTestcase.Attribute("noparse").Value == "true") continue;
                        string TestcaseName = InputTestcase.Attribute("name").Value.ToLower();
                        //Testcase elements validation
                        if (Testcases.ContainsKey(TestcaseName))
                        {
                            Logger.Log(LogCode.TESTCASE_DUPLICATE, TestcaseName);
                            continue;
                        }
                        TestcaseObject ParsedTestcase = new TestcaseObject();
                        if (InputTestcase.Element("Hex") == null)
                        {
                            Logger.Log(LogCode.TESTCASE_NOHEX, TestcaseName);
                            continue;
                        }
                        else if(TryParseHex(InputTestcase.Element("Hex").Value, out byte[] TestcaseMemory))
                        {
                            ParsedTestcase.Memory = new MemorySpace(TestcaseMemory); // memory = <hex>x</hex>
                        }
                        else
                        {
                            Logger.Log(LogCode.TESTCASE_BADHEX, TestcaseName);
                            continue;
                        }
                        // Checkpoint validation
                        foreach (var InputCheckpoint in InputTestcase.Elements("Checkpoint"))//for each <checkpoint> in file
                        {
                            ulong BreakpointAddr = Convert.ToUInt64(InputCheckpoint.Attribute("position_hex").Value, 16);//what the instruction pointer will be when the checkpoint is tested
                            Checkpoint ParsedCheckpoint = new Checkpoint()
                            {
                                Tag = InputCheckpoint.Attribute("tag") == null ? "Unnamed" : InputCheckpoint.Attribute("tag").Value,
                            };
                            foreach (XElement TestCriteria in InputCheckpoint.Elements())//for each criteria in the checkpoint
                            {
                                if (TestCriteria.Name == "Register") //<register>
                                {
                                    TestRegister Reg = new TestRegister();
                                    if(Reg.TryParse(TestCriteria))
                                    {
                                        ParsedCheckpoint.ExpectedRegisters.Add(Reg);
                                    } else
                                    {
                                        Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Invalid syntax of register element" });
                                    }                                        
                                }
                                else if (TestCriteria.Name == "Memory") //<memory>
                                {

                                    TestMemoryAddress Mem = new TestMemoryAddress();
                                    (LogCode, string) ErrorInfo;
                                    if (!Mem.TryParse(TestCriteria, out ErrorInfo))
                                    {
                                        Logger.Log(ErrorInfo.Item1, new string[] { TestcaseName, ErrorInfo.Item2 });
                                        continue;
                                    }                                    
                                    ParsedCheckpoint.ExpectedMemory.Add(Mem);
                                }
                                else if (TestCriteria.Name == "Flag")
                                {
                                    if (TestCriteria.Attribute("name") == null)
                                    {
                                        Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Missing flag name attribute" });
                                    }
                                    else if(!FlagSet.ValidateString(TestCriteria.Attribute("name").Value))
                                    {
                                        Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Flag name attribute invalid" });
                                    } else
                                    {
                                        ParsedCheckpoint.TestFlags = true;
                                        ParsedCheckpoint.ExpectedFlags[TestCriteria.Attribute("name").Value] = TestCriteria.Value == "1" ? FlagState.ON : FlagState.OFF;
                                    }
                                }
                            }
                            ParsedTestcase.Checkpoints.Add(BreakpointAddr, ParsedCheckpoint);
                        }                                                       
                        Testcases.Add(TestcaseName, ParsedTestcase);
                    }                               
                }
            }
        }
        private static bool TryParseHex(string hex, out byte[] Output)
        {
            hex = hex.Trim();
            if (hex.Length % 2 != 0) { hex = hex.Insert(0, "0"); }
            Output = new byte[hex.Length / 2];
            for (int ByteIndex = 0; ByteIndex < hex.Length / 2; ByteIndex++)
            {
                byte NextByte;
                if(byte.TryParse(hex.Substring(ByteIndex * 2, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out NextByte))
                {
                    Output[ByteIndex] = NextByte;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        private static string ParseBytes(byte[] bytes)
        {
            string[] Output = new string[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                Output[i] += bytes[i].ToString("X");
            }
            return string.Join(", ", Output);
        }    
        public static async Task<XElement> ExecuteTestcase(string name)
        {
            name = name.ToLower();
            XElement Output;
            if(Testcases.ContainsKey(name))
            {
                TestingEmulator Emulator = new TestingEmulator(Testcases[name]);
                TestcaseResult Result = await Emulator.RunTestcase();
                Output = Result.ToXML(name);
            }
            else
            {
                Logger.Log(LogCode.TESTCASE_NOT_FOUND, "name");
                Output = new XElement(name, new XAttribute("result", "error: testcase not found"));
            }
            return Output;
        }   
        public static async Task<XElement> ExecuteAll()
        {
            XElement Base = new XElement("all");
            bool Passed = true;
            foreach (var Testcase in Testcases)
            {
                TestingEmulator Emulator = new TestingEmulator(Testcase.Value);
                TestcaseResult Result = await Emulator.RunTestcase();
                Base.Add(Result.ToXML(Testcase.Key));
                Passed &= Result.Passed; //once result.passed==false, base.passed will never be true again
            }
            Base.SetAttributeValue("result", Passed ? "passed" : "failed");
            return Base;
        }
        public static string[] GetTestcases() => Testcases.Keys.ToArray();
        
    }    
}
