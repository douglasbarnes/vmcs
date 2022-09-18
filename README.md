Vmcs is an expository project I developed to gain a better understanding of Intel x64 assembly and practice C# OO skills. The project aims to allow the user to analyse the assembly code behind real executable files compiled for the 2016 version of Intel x64 processors ([described here](https://www.intel.com/content/dam/www/public/us/en/documents/manuals/64-ia-32-architectures-software-developer-vol-1-manual.pdf)).
- An executable can be opened and disassembled. Each instruction is converted into (usually) two objects, an Opcode and an IMyDecoded interface which holds the arguments given to the opcode.
- All parts of the processor (eg memory, registers, opcodes) have been rewritten in equivalent object orientated classes and interfaces in C#.
- The UI resembles that of GDB and other debugging software. The stack, entire program, registers, and breakpoints are all displayed. 
- The user can step through the program, continue to next breakpoint etc.
- It is incredibly easy to extend functionality. Almost all commonly used opcodes are implemented. You can easily add your own by inherting the Opcode class. The code has all been annotated and explained.
- The behaviour corresponds exactly to how is described in the manual (for that which has been implemented). Upsides and downsides.
- Due to the size of the project, a powerful self-testing component has been developed which allows the user-develoeper to provide a test-case. This consists of an input assembly program and expected output. VMCS can execute a single selected testcase, or all of them (about 100 have already been written for testing existing features). Output is stored in a JSON file and presented to the user in the UI.

A writeup for the project was produced, some 400 pages long. It is available in the repository, but not worth looking at.
