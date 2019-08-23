using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using debugger.Emulator;
using debugger.Util;
namespace debugger.Hypervisor
{
    static class TestHandler
    {
        private readonly static Dictionary<string, XRegCode> RegisterDecodeTable = new Dictionary<string, XRegCode>() {
            {"A",XRegCode.A }, {"B",XRegCode.B },{"C",XRegCode.C },{"D",XRegCode.D },
            {"SP",XRegCode.SP }, {"BP",XRegCode.BP },{"SI",XRegCode.SI },{"DI",XRegCode.DI },
            {"R8",XRegCode.R8 }, {"R9",XRegCode.R9 },{"R10",XRegCode.R10 },{"R11",XRegCode.R11 },
            {"R12",XRegCode.R12 }, {"R13",XRegCode.R13 },{"R14",XRegCode.R14 },{"R15",XRegCode.R15 }
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
            public XRegCode RegisterCode;
            public RegisterCapacity Size;
            public ulong ExpectedValue;
        }
        protected struct TestMemoryAddress
        {
            public XRegCode? OffsetRegister;
            public long Offset;
            public byte[] ExpectedBytes;
        }
        private static Dictionary<string, TestcaseObject> Testcases = new Dictionary<string, TestcaseObject>();
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
                bool TestcasePassed = true;
                
                for (int CheckpointNum = 0; CheckpointNum < CurrentTestcase.Checkpoints.Count; CheckpointNum++)
                {
                    await RunAsync();    
                    Context Snapshot = Handle.ShallowCopy();
                    Checkpoint CurrentCheckpoint = CurrentTestcase.Checkpoints[Snapshot.InstructionPointer];
                    List<CheckpointSubresult> CurrentSubresults = new List<CheckpointSubresult>();                    
                    foreach (TestRegister testReg in CurrentCheckpoint.ExpectedRegisters)
                    {
                        string Mnemonic = Disassembly.DisassembleRegister(testReg.RegisterCode, testReg.Size, REX.NONE);
                        bool RegistersEqual = Snapshot.Registers[testReg.Size, testReg.RegisterCode].CompareTo(Bitwise.Cut(BitConverter.GetBytes(testReg.ExpectedValue), (int)testReg.Size), false) == 0;
                        TestcasePassed &= RegistersEqual;
                        CurrentSubresults.Add(
                            new CheckpointSubresult
                            {
                                Passed = RegistersEqual,
                                Expected = $"${Mnemonic}=0x{testReg.ExpectedValue.ToString("X")}",
                                Found = $"${Mnemonic}=0x{BitConverter.ToUInt64(Bitwise.ZeroExtend(Snapshot.Registers[testReg.Size, testReg.RegisterCode],8),0).ToString("X")}"
                            });
                    }
                    foreach (TestMemoryAddress testMem in CurrentCheckpoint.ExpectedMemory)
                    {
                        ulong ExactAddress = 0;;
                        string Mnemonic = "";
                        if (testMem.OffsetRegister.HasValue)
                        {
                            Mnemonic = Disassembly.DisassembleRegister(testMem.OffsetRegister.Value, RegisterCapacity.GP_QWORD, REX.NONE);
                            if(testMem.Offset != 0)
                            {
                                Mnemonic += (testMem.Offset > 0) ? "+" : "-";
                            } 
                            ExactAddress += BitConverter.ToUInt64(Snapshot.Registers[RegisterCapacity.GP_QWORD, testMem.OffsetRegister.Value], 0);
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
                        TestcasePassed &= MemoryEqual;
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
                        TestcasePassed &= FlagsEqual;
                        CurrentSubresults.Add(
                        new CheckpointSubresult
                        {
                            Passed = FlagsEqual,
                            Expected = CurrentCheckpoint.ExpectedFlags.ToString(),
                            Found = ControlUnit.Flags.And(CurrentCheckpoint.ExpectedFlags)
                        });
                    }                    
                    if(!CurrentSubresults.Last().Passed)
                    {
                        Result.Passed = false;
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
                    try
                    {
                        XDocument TestcaseFile = XDocument.Load(FilePath);
                        foreach (XElement InputTestcase in TestcaseFile.Elements("Testcase"))//for each <testcase> in the file
                        {
                            if (InputTestcase.Attribute("noparse") != null && InputTestcase.Attribute("noparse").Value == "true") continue;
                            if(InputTestcase.Element("Hex") == null)
                            {
                                throw new Exception("TestHandler.cs <Hex> element required with shellcode of testcase");
                            }
                            TestcaseObject ParsedTestcase = new TestcaseObject
                            {
                                Memory = new MemorySpace(ParseHex(InputTestcase.Element("Hex").Value)) // memory = <hex>x</hex>
                            };
                            foreach (var InputCheckpoint in InputTestcase.Elements("Checkpoint"))//for each <checkpoint> in file
                            {
                                ulong BreakpointAddr = Convert.ToUInt64(InputCheckpoint.Attribute("position_hex").Value, 16);//what the instruction pointer will be when the checkpoint is tested
                                Checkpoint ParsedCheckpoint = new Checkpoint()
                                {
                                    Tag = InputCheckpoint.Attribute("tag").Value,
                                };
                                foreach (XElement TestCritera in InputCheckpoint.Elements())//for each criteria in the checkpoint
                                {
                                    if (TestCritera.Name == "Register") //<register>
                                    {
                                        ParsedCheckpoint.ExpectedRegisters.Add(
                                            new TestRegister()
                                            {
                                                RegisterCode = RegisterDecodeTable[TestCritera.Attribute("id").Value],
                                                Size = (RegisterCapacity)(int)TestCritera.Attribute("size"),
                                                ExpectedValue = Convert.ToUInt64(TestCritera.Value, 16)
                                            });
                                    }
                                    else if (TestCritera.Name == "Memory") //<memory>
                                    {
                                        long Offset = 0;
                                        XRegCode? RegOffset = null;
                                        if (TestCritera.Attribute("offset_register") != null)
                                        {
                                            RegOffset = (XRegCode?)RegisterDecodeTable[TestCritera.Attribute("offset_register").Value];
                                        }
                                        if (TestCritera.Attribute("offset") != null)
                                        {
                                            Offset += BitConverter.ToInt64(Bitwise.SignExtend(Bitwise.ReverseEndian(ParseHex(TestCritera.Attribute("offset").Value)), 8), 0);  
                                            if(Offset < 0 && RegOffset == null)
                                            {
                                                throw new Exception("Cannot have a negative memory offset without a register to offset it" +
                                                                "\nRemember that the offset it in a testcase is a signed long value, so put a 0 before, or use a register");
                                            }
                                        }
                                        else if (RegOffset == null)
                                        {
                                            throw new Exception("Memory needs to have atleast an offset or offset_register attribute");
                                        }                                      
                                        
                                        ParsedCheckpoint.ExpectedMemory.Add(
                                            new TestMemoryAddress()
                                            {
                                                Offset = Offset,
                                                OffsetRegister = RegOffset,
                                                ExpectedBytes = ParseHex(TestCritera.Value)
                                            });
                                    }
                                    else if (TestCritera.Name == "Flag")
                                    {
                                        if(TestCritera.Attribute("name") == null)
                                        {
                                            throw new Exception("Flag must have a name");
                                        }
                                        ParsedCheckpoint.TestFlags = true;
                                        ParsedCheckpoint.ExpectedFlags[TestCritera.Attribute("name").Value] = TestCritera.Value == "1" ? FlagState.ON : FlagState.OFF;                                        
                                    }
                                    else
                                    {
                                        throw new Exception($"Unexpected element {TestCritera.Name}");//throw error if it wasnt memory of register
                                    }
                                }
                                ParsedTestcase.Checkpoints.Add(BreakpointAddr, ParsedCheckpoint);
                            }
                            string TestcaseName = InputTestcase.Attribute("name").Value.ToLower();
                            if(Testcases.ContainsKey(TestcaseName))
                            {
                                MessageBox.Show("Duplicate testcase entry " + TestcaseName);
                            } else
                            {
                                Testcases.Add(TestcaseName, ParsedTestcase);
                            }
                            
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error reading testcase file {FilePath}:\n{e.ToString()}");
                    }
                }
            }
        }
        private static byte[] ParseHex(string hex)
        {            
            if(hex.Length % 2 != 0) { hex = hex.Insert(0, "0"); }
            byte[] Output = new byte[hex.Length / 2];
            for (int ByteIndex = 0; ByteIndex < hex.Length/2; ByteIndex++)
            {
                Output[ByteIndex] = Convert.ToByte(hex.Substring(ByteIndex * 2, 2), 16);
            }
            return Output;
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
        public static event Action OnTestcaseNotFound = () => MessageBox.Show("Testcase not found");      
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
                OnTestcaseNotFound.Invoke();
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

        private enum TestcaseExceptionType
        {
            DUPLICATETESTCASE,
            UNEXPECTEDELEMENT,
            FLAGNONAME,

        }
        private class TestcaseException : Exception
        {
            public TestcaseException(TestcaseExceptionType type, string data="")
            {

            }
        }
    }    
}
