// TestHandler provides the core self-testing functionality that the program provides. With one method call, all operational functionality of the ControlUnit and Opcode classes can be
// tested. This module has saved countless hours of debugging, both finding bugs and narrowing down the cause. It can definitely be incorporated into any other modules or new opcodes added.
// Despite being a module itself, I highly discourage changing anything or creating a custom TestHandler module unless new ControlUnit functionality requires so. For new opcodes,
// a super simple XML format is used to create a "Testcase". A testcase contains assembled executable code along with checkpoints. Checkpoints are essentially breakpoints in the program,
// except when the checkpoint is reached, data in the ControlUnit can be compared with verified results tested on a live machine. A testcase follows the convention of testing one particular opcode
// or purpose. Some opcodes are concatenated into a single testcase, such as push and pop as they both rely on the same classes/methods, hence "pushpop". String operations are also combined into one
// testcase. This is because string operations cover a very specific area. They all use the same StringOperation base class and I see no future cause to ever change those instructions. This results in
// a moderately complex testcase, but a working version will always be available to refer back to in the repository. Sometimes a testcase can provide multiple functions discretely,
// For example, most of the old testcases such as addadc and mov will also test for memory to be correctly set. The idea is that every testcase tests something, and in total the volume of testcases
// will ensures that no mistake can slip through the system undetected, as somewhere along the line a testcase will fail. Every testcase provided complies with the May 2019
// spec of the intel manual( https://software.intel.com/sites/default/files/managed/39/c5/325462-sdm-vol-1-2abcd-3abcd.pdf ). They have been tested both on a virtual machine and on my
// laptop(which has a processor released according to this spec). There has only been one instance where the tests conflicted which was the sign flag being set on an IMUL instruction.
// I ruled in favour of the newer spec here for consistency. It would be unreliable to base code on one processor. In terms of intel x86, it is a large misconception that every processor
// requires different coding for each model. As you will find at lower layers of the program, there are insane circumstances where something has to be calculated as compatibility for an
// ancient processor. Off the top of my head, a great example is in the shift instructions. At a hardware level, there used to be two mechanisms for a bitwise shift, one was specifically
// designed for bitwise shifts of one place. The one place shift mechanism would set the overflow flag if a certain condition was met. Ever since then, it has been specially coded in
// that this behaviour remains, even though they are no longer two separate mechanisms and the case of the overflow flag being set indicates nothing of value at all. I suggest a similar
// depth of research goes into any testcase creation as generally there has never been cause to replace or update testcases once added. The XML format follows a simple syntax and an example
// is available in the Testcases folder. 
// The following assumes a basic understanding of XML.
// Firstly, every testcase must have a root level element "Testcase" and an attribute "name". E.g,
//  <Testcase name="MyTestcase">
// Secondly, UTF8 character representations of bytes in hex must be entered in a Hex element. This follows the same input validation as IO.FileParser uses, Core.TryParseHex(). In the case of invalid
// input, behaviour defined there along with more details of what "UTF8 character representations of bytes in hex" is. E.g,
//  <Hex>90909090909090</Hex>
// In the case of multiple hex tags being present, XElement.Element() defines this behaviour as, "Gets the first (in document order) child element with the specified XName.". IF there is none, a
// TESTCASE_NOHEX will be thrown. However, there is no reason for any more or less than one.
// Thirdly, checkpoints. A checkpoint-less testcase that only runs code is possible, but is not recommended as having a testcase with many checkpoints for specific behaviours is much more informative
// and is aligned with the purpose of a testcase. Checkpoints contain a tag and position_hex attribute. Both are necessary. The tag(which is called tag to easily differentiate between "named" elements)
// is a quality of life string that is used to tie parts of the results to a specific name. As you will see later, the result output would be an utter jungle without such a system. Use empty strings at your
// own peril. Secondly, position_hex represents a big endian address that marks the position to break. The instruction at this address is NOT run. If you decide to put it in the middle of an instruction,
// the testcase will run until the next checkpoint, or until the end of all instructions where it is forcibly stopped and you will get a TESTCASE_RUNTIME error. In addition, it is recommended that you
// have a checkpoint at the end of the testcase, not only because it would only make sense to do so, but also because you will recieve this error. A checkpoint has three possible elements it can contain.
// If there is some kind of invalid data in the contained elements, a TESTCASE_BADCHECKPOINT will be thrown. Any extraneous elements are ignored. The register element allows a register to be tested of sizes
// byte, word, dword, qword. In this sense, a light implementation of SSE registers is in place, however due to them not being implemented, they serve no functional purpose.
// This is done by writing the size of the register in bytes in a "size" attribute. The "id" attribute is for selecting which register to compare. Exact definitions can be found in $RegisterDecodeTable. To
// improve modularity, TestHandler has its own definitions of register mnemonics here. Enclosed in the register element should be big endian bytes that are to be compared with the register at the checkpoint.
// E.g,
//  <Register id="B" size="8">1000917</Register>
// RBX will be tested against 0x1000917(zero extended). Big endian is used because registers are normally represented in big endian format. I don't exactly love this convention because it becomes very confusing
// when comparing with memory, but is most prevalent. 
// The next type of element is the Memory element. The address of this memory could be an exact offset, a location pointed to by a register, or a combination of both. Registers in offset_register attribute have the same
// mnemonics as the Register element has. A value in the offset attribute represents a big endian signed offset qword offset to a memory location. If less than 8 bytes are provided, the given is sign extended. This allows
// a negative offset, as is very common when setting memory. If you want to put your arguments at $RBP-0x8, thats totally fine. However if there is only a negative offset, you will get an error. This is to prevent any
// uncertainty. As great as using both offsets in combination is, be wary of the additional dependencies this brings. Firstly it requires your assembly to be spot on, and secondly relies on the error not being a register
// that wasn't set properly, a bug in that area could be fatal. It may be significantly harder to track down a bug like this. However I have still used it numerous times as every testcase would fail if this was the case,
// and seeing every testcase fail in the results would be alarming enough to suggest such a situation. Enclosed in the Memory element should be bytes encoded in little endian. There is no set size requirements for these bytes,
// they can be long or short as wanted. In stringops.xml, 256 bytes are tested for at once.  The offset register it always of QWORD size.
// E.g, to test if [$rsp+1] == 00 and [$rsp+2] == 20,
//  <Memory offset_register="SP" offset="1">0020</Memory>
// The final type of element is the Flag element. The "name" attribute of the flag is defined in FlagSet. At the time of writing, full case insensitive names must be written such as Carry and siGN.  In the value of the element should
// be a 0 or 1, however in the code only 1s are considered. If the value is not "1", it will be considered a "0". By convention I recommend writing a 0 for consistency and simplicity. A flag will only ever be checked if a flag element
// for it is present, in other words, a flag that does not have an element referencing it will be ignored. This means that you can just put the flags that you are wanting to check without having to have an exhausive number of elements for
// each flag. Entering no/an unrecognised flag name will throw a TESTCASE_BADCHECKPOINT. 
// E.g,
//  <Flag name="Carry">1</Flag>
// The result of a testcase also is in xml format. The root level element is TestcaseResult. It has two attributes: "name", the name of the testcase it is a result of, and "result" which is a string value of either Passed or Failed. The result
// attribute indicates whether every testcase passed. It is "Failed" if any checkpoint in any testcase did not pass. This makes it easy to identify the presence of a problem in o(1), rather than having to iterate every result. Internally this operation
// is done in the same loop as individual checkpoints, so the algorithmic complexity remains the same. The next level consists of CheckpointResult elements. Each has a "tag" attribute which has the same value as the tag entered into the testcase
// xml file, or if there was no name present it will be "Unnamed". CheckpointResults do not have a result attribute as this information becomes excessive, if you are looking for individual results at this level you are likely to be iterating
// through the child elements also. The next level consists of SubCheckpointResult elements. These are a generalisation of Memory, Register and Flag elements that were in the testcase file. Each contains an Expected and Found element.
// Expected holds the data that was parsed from the testcase file(this is also useful for finding errors in the testcase file, or errors in the testhandler parsing it). The Found element holds what was actually found at that checkpoint.
// To be thorough, this information is shown every time. In one commit,  (Here is a link, https://github.com/koreanair/vmcs/commit/605b1536813ec4bec843553e08a06e2d48a6840a , I don't know of a more convenient way to reference this),
// a bug was addressed  where the boolean representing whether every testcase so far had passed would be initialised as true and never changed after that. It is unknown whence it originated, but I assume for a long time prior it
// had slipped under the radar. However, I assume it was only ever possible that I noticed this after manually inspecting the results output as the Expected and Found elements had completely different values for many of the checkpoints. Without
// this thoroughgoing testing paradigm it would likely have never been found. At the end of the day, TestHandler provides a quick way to test the lowest layer modules but making significant changes to the program always warrants a manual inspection
// of the results file. There is always the possiblity of bugs in the TestHandler. The value held by the Expected and Passed elements hold a very intuitive format that most programmers will be familiar with, 
// especially if they have used GDB or derivatives of, as I have tried to keep the programs very cross compatible in terms of skill set. A confident GDB user could use this program with ease. '$' denotes the value of a register,
// e.g $EAX=0x10. This disassembly is provided by a RegisterHandle, the same method used to retrieve the value. I definitely prefer this syntax over GAS syntax, which would suggest using "%". I really do not like this syntax as it makes
// code look very messy and unintuitive, so GDB and Intel flavoured syntax has always been favoured throughout. Secondly, memory checks follow standard pointer format, e.g [EAX+0x10]={0, 0, 0, 10}. The range of memory checked will always be
// the same length as the memory in the testcase file. Word convention sizes are not necessary. Strictly there should also be a '$' before the mnemonic in this format also, but to make it easier to differentiate between memory and register 
// checks this has been ommited. It is surprisingly easy to miss a square bracket in an xml file. Flags also follow a very simple format that is similar to that of GDB. String representations of flags are defined in FlagSet, but currently
// follow the system of concatenation. Each flag is put in "short form" where it is only two letters and added to a string, e.g "OFAF" would indicate the overflow and auxiliary flag were set. Only tested flags are shown in this string.
// These factors combined make the output very easy to interpret. It is easier to see the differences in a short string than a long one. 
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
        // This table is used to resolve user input for registers in testcase files.
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
                // Create an element to hold all the checkpoint results.
                XElement Output = new XElement("TestcaseResult", new XAttribute("name", name));

                // Set the result attribute
                Output.SetAttributeValue("result", Passed ? "Passed" : "Failed");

                // Iterate over and parse all the checkpoint results.
                foreach (var CheckpointOutput in CheckpointOutputs)
                {
                    // Create a new XML element with the same tag attribute as the checkpoint it corresponds to.
                    XElement CheckpointResult = new XElement("CheckpointResult", new XAttribute("Tag", CheckpointOutput.Key.Tag));

                    // Iterate over and parse every subcheckpoint 
                    foreach (var SubCheckpointOutput in CheckpointOutput.Value)
                    {
                        CheckpointResult.Add(
                            new XElement("SubCheckpointResult", new XAttribute("result", SubCheckpointOutput.Passed ? "Passed" : "Failed")
                                ,new XElement("Expected", SubCheckpointOutput.Expected)
                                ,new XElement("Found", SubCheckpointOutput.Found)));
                    }

                    // Add each CheckpointResult as sub element of Output.
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
                // This method attempts to parse a given XElement into a TestRegister. If there is an error it returns false. This allows for some simpler
                // error handling than if it was a constructor as the caller can decide the best course of action.
                // Here some checks are greatly compacted into a try catch block.
                // The scenarios where something could go wrong,
                //  - Invalid register id attribute hence cannot be found in dictionary, KeyNotFoundException.
                //  - Register has no id attribute, it is null, NullReferenceException.
                //  - No size attribute, NullReferenceException.
                //  - Size attribute is invalid, cannot be casted to int.
                //  - No value in $testCriteria.Value, null etc.
                //  - Value in $testCriteria.Value cannot be converted to a ulong.
                // All of those fit very nicely into a try catch block, which is much faster than validating all the input manually e.g with many ifs
                // but only when the input is valid. There is a large overhead when an exception is caught like this. However slowing down the proficient users
                // for the sake of others is not what I intend to do.
                try
                {
                    // Retrieve the definition of the mnemoinic in the id attribute.
                    (RegisterTable Table, XRegCode Code) = RegisterDecodeTable[testCriteria.Attribute("id").Value];

                    // Create a new register handle to it that will be used to compare the registers at runtime.
                    Register = new ControlUnit.RegisterHandle(Code, Table, (RegisterCapacity)(int)testCriteria.Attribute("size"));

                    // If the value that is to be compared is greater than the size of the register, the input must be invalid. It is doubled because it takes
                    // two characters to represent a byte in hex.
                    if (testCriteria.Value.Length > (int)Register.Size * 2)
                    {
                        return false;
                    }

                    // Convert the value from hex to ulong.
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
                // If none of the input data(bytes to be tested for at the address) can be parsed, throw the error. Its an edge case but better
                // than no measure at all.
                if (!Core.TryParseHex(input.Value, out ExpectedBytes))
                {
                    ErrorInfo = (LogCode.TESTCASE_PARSEFAIL, "Invalid value of expected memory.");
                    return false;
                }

                // For the following, when an attribute not present, a null is returned. That is the basis of these conditionals.

                // If there is an offset register,
                if (input.Attribute("offset_register") != null)
                {
                    // Attempt to fetch the definition if the given mnemonic from the dictionary.
                    (RegisterTable, XRegCode) Result;
                    if (!RegisterDecodeTable.TryGetValue(input.Attribute("offset_register").Value, out Result))
                    {
                        ErrorInfo = (LogCode.TESTCASE_PARSEFAIL, "Invalid value of offset register.");
                        return false;
                    }

                    // Store the register handle for comparison later.
                    OffsetRegister = new ControlUnit.RegisterHandle(Result.Item2, Result.Item1, RegisterCapacity.QWORD);
                }

                // If there is an offset,
                if (input.Attribute("offset") != null)
                {

                    // As explained in the summary, the value in offset is parsed as big endian. Core.ReverseEndian is very
                    // robust, it does not check any validity of the input and only does its job. Other functionality such as
                    // Core.TryParseHex() is left to do this.
                    byte[] OffsetBytes;                                      
                    if (!Core.TryParseHex(Core.ReverseEndian(input.Attribute("offset").Value), out OffsetBytes))
                    {
                        ErrorInfo = (LogCode.TESTCASE_PARSEFAIL, "Invalid hex bytes in memory offset attribute");
                        return false;
                    }

                    // Also if the offset is negative and there is no offset register, the input cannot be valid. There is still the possibility to 
                    // escape this by putting a register that will be zero at that time, but at that point there is either a huge obvious problem that
                    // will fail every testcase, or worse yet, your assembly code was wrong.
                    Offset = BitConverter.ToInt64(Bitwise.SignExtend(OffsetBytes, 8), 0);
                    if (Offset < 0 && OffsetRegister == null)
                    {
                        ErrorInfo = (LogCode.TESTCASE_BADCHECKPOINT, "Cannot have a negative memory offset without a register to offset it");
                        return false;
                    }
                }

                // To check memory there must be at least an offset or an offset register. 
                else if (OffsetRegister == null)
                {
                    ErrorInfo = (LogCode.TESTCASE_BADCHECKPOINT, "Must have an offset attribute, offset_register attribute, or both");
                    return false;
                }

                // As this is a struct, the values have to be assigned to something in the constructor.
                ErrorInfo = (LogCode.NONE, null);
                return true;
            }
        }
        private static readonly Dictionary<string, TestcaseObject> Testcases = new Dictionary<string, TestcaseObject>();
        private class TestingEmulator : HypervisorBase
        {
            readonly TestcaseObject CurrentTestcase;
            public TestingEmulator(TestcaseObject testcase) : 
                base("TestingEmulator", new Context(testcase.Memory)
                {
                    Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>
                        {
                            { XRegCode.SP, testcase.Memory.SegmentMap[".stack"].Range.Start },
                            { XRegCode.BP, testcase.Memory.SegmentMap[".stack"].Range.Start },
                        }),
                    Breakpoints = new ListeningList<ulong>(testcase.Checkpoints.Keys.ToList())

                // Its actually super crazy that a deep copy has to be used here. The testcases in the dictionary are objects too, so when one is run, the same problems are faced.
                // For example, references, shallow copies, the whole nine yards. An interesting logic error would occur when this was not deep copied, as each time the testcase ran,
                // the memory would be persist across each run such that it would appear like every opcode was doing twice the operation it should. E.g when the testcase ran the first time,
                // the results would stay in the MemorySpace, then the next time it was ran it would add on top of that memory rather than have a clean memory.
                }.DeepCopy()) 
            {
                CurrentTestcase = testcase;
            }
            public async Task<TestcaseResult> RunTestcase()
            {
                TestcaseResult Result = new TestcaseResult();
                
                // Run every checkpoint. As they are ordered(this is automatically done due to the nature of how they are parsed), you will get half baked results if
                // one checkpoint is out of the code address range or in the middle of an instruction. This is not something that can be fixed from this end because my
                // code nor myself know what to expect as the results of your code if there is an error in the way that I execute your code. That is why I highly recommend
                // properly testing on a real machine and basing your testcase of that.
                for (int CheckpointNum = 0; CheckpointNum < CurrentTestcase.Checkpoints.Count; CheckpointNum++)
                { 
                    // Run until the checkpoint
                    Run();

                    // This Snapshot variable will keep the code nice and readable.
                    Context Snapshot = Handle.ShallowCopy();

                    // If the instruction pointer is not an address in the checkpoint list, it must have ended prematurely, i.e not all checkpoints were handled before execution finished.
                    // This is more than likely an error in how the testcase was written.
                    Checkpoint CurrentCheckpoint;
                    if (!CurrentTestcase.Checkpoints.TryGetValue(Snapshot.InstructionPointer, out CurrentCheckpoint))
                    {
                        Logger.Log(LogCode.TESTCASE_RUNTIME, "");
                        break;
                    }
                    
                    // Iterate over every type of test present and handle them slightly differently.
                    List<CheckpointSubresult> CurrentSubresults = new List<CheckpointSubresult>();                    
                    foreach (TestRegister testReg in CurrentCheckpoint.ExpectedRegisters)
                    {
                        // Disassemble the mnemonic, provided by RegisterHandle. This will be different to how it was input as the full mnemonic that is representative of the size is given rather than a shorthand.
                        string Mnemonic = testReg.Register.DisassembleOnce();

                        // This is the "Found" value.
                        byte[] ActualValue = testReg.Register.FetchOnce();

                        // If CompareTo() returns 0, they are equal. ExpectedValue is cut to the same size as the register just to be sure. 
                        bool RegistersEqual = ActualValue.CompareTo(Bitwise.Cut(BitConverter.GetBytes(testReg.ExpectedValue), (int)testReg.Register.Size), false) == 0;

                        // AND $Passed by $RegistersEqual. This means that if $RegistersEqual is ever false, Passed will stay false for the entire testcase.
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
                        // ExactAddress will hold the starting address of the memory to compare.
                        ulong ExactAddress = 0;

                        // Build up the mnemonic throughout based on the conditions.
                        string Mnemonic = "";

                        // If there is an offset register(It would be null if there was not as TestMemoryAddress is a struct),
                        if (testMem.OffsetRegister != null)
                        {
                            // Use the RegisterHandle to do the disassembly.
                            Mnemonic = testMem.OffsetRegister.DisassembleOnce();

                            // This nested condition is unavoidable as there are certain things that need to happen if there is an offset register as well
                            // as an offset, and things that need to happen both ways if there is an offset at all.
                            if(testMem.Offset != 0)
                            {
                                // Append a +/- based on the offset as the absolute value is taken later. Just for quality of life really, It's alot easier than
                                // having to work out the magnitude of a twos compliment value in your head, even if its just subtraction it still leaves unnecessary
                                // work for the user.
                                Mnemonic += (testMem.Offset > 0) ? "+" : "-";
                            }

                            // Add the value of the register onto $ExactAddress.
                            ExactAddress += BitConverter.ToUInt64(testMem.OffsetRegister.FetchOnce(), 0);
                        }

                        // This secretly does two things. If there was no offset at all(no attribute), it was never assigned to into the struct, therefore its default
                        // value would be zero. Also, if offset was zero with an offset_register, its not very useful to see, so only the offset_register will be shown.
                        if (testMem.Offset != 0)
                        {
                            // The absolute value is taken because a negative is not possible at this point unless there was an offset register. In that case, a +/- was already
                            // added to show the sign.
                            Mnemonic += $"0x{Math.Abs(testMem.Offset).ToString("X")}";

                            // Finally add the offset onto $ExactAddress
                            ExactAddress += (ulong)testMem.Offset;
                        }

                        // Assume equality at first.
                        bool MemoryEqual = true;

                        // Create a new aob for the "Found" element.
                        byte[] FoundBytes = new byte[testMem.ExpectedBytes.Length];

                        // Compare the bytes in $Snapshot.Memory with the bytes in $ExpectedBytes
                        for (uint ByteOffset = 0; ByteOffset < testMem.ExpectedBytes.Length; ByteOffset++)
                        {
                            FoundBytes[ByteOffset] = Snapshot.Memory[ExactAddress + ByteOffset];
                            if (Snapshot.Memory[ExactAddress + ByteOffset] != testMem.ExpectedBytes[ByteOffset])
                            {
                                // If there is one or more bytes different, it must be false. A break cannot be added here
                                // because FoundBytes is still being filled up.
                                MemoryEqual = false;                                
                            }                            
                        }

                        // Same as with register.
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
                        // A lot of the heavy work here is done by FlagSet.

                        // This method will ignore all flags that are FlagState.UNDEFINED in $ExpectedFlags but still compare the flags that do have a value.
                        // This makes sure flags do not have to be exhaustively specified in the testcase file. If only the carry flag is specified in an element,
                        // only the carry flag will be considered.
                        bool FlagsEqual = CurrentCheckpoint.ExpectedFlags.EqualsOrUndefined(ControlUnit.Flags);
                        Result.Passed &= FlagsEqual;
                        CurrentSubresults.Add(
                        new CheckpointSubresult
                        {
                            Passed = FlagsEqual,
                            Expected = CurrentCheckpoint.ExpectedFlags.ToString(),

                            // This produces a string representation of all the flags that are not FlagState.UNDEFINED in $Snapshot.Flags and $ExpectedFlags.
                            Found = Snapshot.Flags.And(CurrentCheckpoint.ExpectedFlags).ToString()
                        });
                    }         
                    Result.CheckpointOutputs.Add(CurrentCheckpoint, CurrentSubresults);
                }
                return Result;
            } 
        }
        static TestHandler()
        {
            ReadTestcases();
        }
        private static void ReadTestcases()
        {
            // Iterate every testcase file in the Testcases/ directory
            foreach (string FilePath in Directory.GetFiles("Testcases")) 
            {
                // Put a simple filter to avoid parsing files that do not have the xml file extension.
                if (FilePath.Substring(FilePath.Length - 4) == ".xml")
                {
                    // There are many things that can go wrong here, such as files already being open, wrong permission, etc etc. I could write a 1000 lines handling this all separately,
                    // or just take the simple solution of telling the user what went wrong and moving on. These errors can not be recovered from from the programs perspective. Would you 
                    // want the program to start messing with other processes or permissions?
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

                    // There can only be one root level element in a valid xml file, so only parse a single testcase.
                    XElement InputTestcase = TestcaseFile.Element("Testcase");

                    // If there is a "noparse" attribute with the value "true", move on(this is used in the example testcase), or if there was no Testcase element.
                    if (InputTestcase.Attribute("noparse") != null && InputTestcase.Attribute("noparse").Value == "true" || InputTestcase == null)
                    {
                        continue;
                    }

                    string TestcaseName = InputTestcase.Attribute("name").Value.ToLower();

                    // If a testcase has already been added, throw an error. (The first parsed will be used).
                    if (Testcases.ContainsKey(TestcaseName))
                    {
                        Logger.Log(LogCode.TESTCASE_DUPLICATE, TestcaseName);
                        continue;
                    }
                    
                    TestcaseObject ParsedTestcase = new TestcaseObject();

                    // The testcase must have a hex element
                    if (InputTestcase.Element("Hex") == null)
                    {
                        Logger.Log(LogCode.TESTCASE_NOHEX, TestcaseName);
                        continue;
                    }

                    // If there is one, try to parse it
                    else if(Core.TryParseHex(InputTestcase.Element("Hex").Value, out byte[] TestcaseMemory))
                    {
                        // Create the memory from the value of the hex element.
                        ParsedTestcase.Memory = new MemorySpace(TestcaseMemory); 
                    }

                    // If there is nothing to parse, throw an error.
                    else
                    {
                        Logger.Log(LogCode.TESTCASE_BADHEX, TestcaseName);
                        continue;
                    }

                    // Checkpoint validation
                    foreach (var InputCheckpoint in InputTestcase.Elements("Checkpoint"))//for each <checkpoint> in file
                    {
                        // First try and parse the position_hex attribute.
                        ulong BreakpointAddr;
                        try
                        {
                            BreakpointAddr = Convert.ToUInt64(InputCheckpoint.Attribute("position_hex").Value, 16);//what the instruction pointer will be when the checkpoint is tested
                        }

                        // An exception could be caught for many reasons, e.g the value is too large, its not hex, its not present etc. No matter what it is assumed evil.
                        catch
                        {                            
                            Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Invalid value for position_hex." });
                            continue;
                        }                           
                        
                        // If there is no tag, call the testcase "Unnamed" as a last resort. 
                        Checkpoint ParsedCheckpoint = new Checkpoint()
                        {
                            Tag = InputCheckpoint.Attribute("tag") == null ? "Unnamed" : InputCheckpoint.Attribute("tag").Value,
                        };

                        // Parse each subcheckpoint check.
                        foreach (XElement TestCriteria in InputCheckpoint.Elements())
                        {
                            if (TestCriteria.Name == "Register") 
                            {
                                // The TestRegister struct does a lot of the heavy lifting here.
                                TestRegister Reg = new TestRegister();
                                if(Reg.TryParse(TestCriteria))
                                {
                                    ParsedCheckpoint.ExpectedRegisters.Add(Reg);
                                } else
                                {
                                    Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Invalid syntax of register element" });
                                }                                        
                            }
                            else if (TestCriteria.Name == "Memory") 
                            {                                
                                TestMemoryAddress Mem = new TestMemoryAddress();

                                // TestMemoryAddress will return some useful information if there is an error in the out parameter. 
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
                                // Do some validation based on whether a name attribute is present or invalid.
                                if (TestCriteria.Attribute("name") == null)
                                {
                                    Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Missing flag name attribute" });
                                }
                                else if(!FlagSet.ValidateString(TestCriteria.Attribute("name").Value))
                                {
                                    Logger.Log(LogCode.TESTCASE_BADCHECKPOINT, new string[] { TestcaseName, "Flag name attribute invalid" });
                                } else
                                {
                                    // A boolean determines whether flags will be checked at all. This needs to be done as they are all concatenated into one. This is all combined
                                    // into a single FlagSet.
                                    ParsedCheckpoint.TestFlags = true;

                                    // As said in the summary, anything that is not "1" is assumed to be 0.
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
        private static string ParseBytes(byte[] bytes)
        {
            // A simple method for converting an aob into a string. 
            string[] Output = new string[bytes.Length];

            // Iterate through each byte in $bytes, convert its hexadecimal string representation and set it in the array.
            for (int i = 0; i < bytes.Length; i++)
            {
                Output[i] += bytes[i].ToString("X");
            }

            // Afterwards join the array with commas inbetween for aesthetic and a little easier to read the output. 
            // This could produce the following for example,
            //  0, 0, 0, 30
            // This is also a lot more efficient than using a string as strings are immutable, the string would have to be reallocated
            // on the stack every time something is concatenated(in theory, some compiler optimisation may recognise this). 
            return string.Join(", ", Output);
        }    
        public static async Task<XElement> ExecuteTestcase(string name)
        {
            // An asynchrous method for executing testcases. This is useful especially when executing all the testcases at once which could
            // lag out the UI thread until the operation is finished. Concurrency can be used to execute this asynchrously such that the user
            // can continue using the program whilst these execute. This could be a short period of time, but regardless is a nice quality of life.
            // If the user created their own super long testcase(in most cases not recommended) for a good reason, which could even be performance,
            // they would definitely want to make use of this feature. 

            // Firstly always deal with lowercase for simplicity.
            name = name.ToLower();
            XElement Output;
            
            // Check if a testcase with that name has been loaded(the possiblity of duplicates was already handled).
            if(Testcases.ContainsKey(name))
            {
                // Create a new emulator instance with the corresponding testcase.
                TestingEmulator Emulator = new TestingEmulator(Testcases[name]);
                
                // Run it and parse the result.
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
            // ExecuteAll() will do some very useful things for the user. All the output is under one root level element, "all" and has its
            // own "result" attribute for easy identification of whether all testcases passed or not. Obviously you do not want to spend your
            // time looking for a failure every time. The UI also makes use of this. It is asynchrous for reasons explained in ExecuteTestcase().

            XElement RootElement = new XElement("all");

            // ExecuteAll() has to do its own thing with running testcases and cannot make use of ExecuteTestcase().
            // This is because of the "result" attribute; checking whether each testcase passed or not.
            bool Passed = true;
            foreach (var Testcase in Testcases)
            {
                TestingEmulator Emulator = new TestingEmulator(Testcase.Value);
                TestcaseResult Result = await Emulator.RunTestcase();
                RootElement.Add(Result.ToXML(Testcase.Key));
                Passed &= Result.Passed;
            }
            RootElement.SetAttributeValue("result", Passed ? "Passed" : "Failed");
            return RootElement;
        }
        public static string[] GetTestcases() => Testcases.Keys.ToArray();
        
    }    
}
