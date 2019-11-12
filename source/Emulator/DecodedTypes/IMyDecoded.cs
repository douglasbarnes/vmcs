// IMyDecoded and IMyMultiDecoded provide universal interfaces for inputs to an opcode. Using these, an opcode class can take an IMyDecoded(Or IMyMultiDecoded) in its constructor and handle every operand the same.
// This means that a opcode class could take a brand new IMyDecoded.ExampleType as an input and rely solely on the public interface members to operate. It also makes the creation of opcode classes super simple,
// most classes will only ever need to use Fetch() and Set(), the opcode base class will handle the rest.
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public interface IMyDecoded
    {
        // The size of the operands. This is very operand specific, some classes may ignore this, such as the NoOperands class.
        // Its purpose will be demonstrated in the derived classes that make use of it on a case-by-case basis.
        public RegisterCapacity Size { get; }

        // An input must have a method of disassembling. This will follow standard convention of showing everything that the user
        // would expect to see except commas between operands. This is because the class itself does not know whether if it is
        // part of a DecodedCompound nor its position within one. In my implementation, the comma is added in the Disassembler
        // class, however it would be just as reasonable in Opcode.cs.
        public List<string> Disassemble();

        // Fetches data to be passed to the opcode. This could be one or more arrays, such as in a ModRM byte, both the source
        // and the destination are encoded, but with an immediate only one value is. This is the best way of implementing this
        // idea without requiring IMyMultiDecoded to be a standalone interface, which would defeat the purpose.
        public List<byte[]> Fetch();

        // Initialise the input. It could be that this isn't necessary, e.g a constant. In every case, an input has Initialise()
        // called before it is used. This is especially useful for when the RegisterCapacity of an opcode isn't known initially.
        // This is called at the end of the Opcode base constructor, so there is time for a custom method of determining the
        // RegisterCapacity in the base constructor call.
        public void Initialise(RegisterCapacity size);

        // Set the destination to the given data. This is mostly left up to the inheritor as many input methods may not be able
        // to be set(such as an immediate) or have different implementations of setting(e.g a modrm told to swap)
        public void Set(byte[] data);
    }
    public interface IMyMultiDecoded : IMyDecoded
    {
        // IMyMultiDecoded has an extra option, to set the source. Naturally there are only a few methods that support this,
        // so it has its own interface. This also gives opcode classes the option of only accepting an input that has SetSource() available.
        public void SetSource(byte[] data);
    }
}
