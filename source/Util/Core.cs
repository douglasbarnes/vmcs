// Util.Core provides more general methods used throughout the program. They are generally very necessary but less performant as
// they make use of higher level objects, most notably linq.
//
// Most notably, Util.Core provides DeepCopy() methods. DeepCopy is what I coin a "language-hack". In high level languages, it is
// often the case that the language works against you. The main problem is that a (good) high level language wants to remove as many
// unnecessary details as it can from the programmer, whilst still giving them as much control over their code as possible. Most of
// the time, this plays out great. Unfortunately, in the case of my program, it does not. The ideas of the Context class are great
// victims of high level languages in this way. The Context class is like a snapshot of the ControlUnit. Think of it like a system backup
// that can be restored at any time. Now, once my backup is stored away. I definitely would not want that backup to automatically update with
// the latest changes to my computer, because then what would be the point? If I restored the backup, the filesystem would be the exact same 
// as before I restored. The Context class suffers a very similar problem. 
// Lets talk about some language fundamentals before addressing this problem.
// C# treats real types,chars, and structs as values(not exhaustive). Everything else is considered an object, and most likely put somewhere on the heap.
// This means that when interacting with a variable that isn't a real type, char, or struct, that interaction is to a reference(pointer), hence "reference type".
// Consider the following C code,
//  #include <stdio.h>
//  int Print_Number(int* pointer)
//  {
//      // Dereference the input pointer and pass it to printf()
//      printf("And the result is, %d\n", *pointer); 
//  }
//  int Add_Two(int input)
//  {
//      // Add two
//      input += 2; 
//  }
//  int main()
//  {
//      int* MyIntegerPointer;
//
//      // Define a new integer and assign 1 to it.
//      int MyInteger = 1;
//
//      // Define a pointer to said integer
//      MyIntegerPointer = &MyInteger;
//
//      // Add two to the integer
//      Add_Two(MyInteger);
//
//      // Print it
//      Print_Number(MyIntegerPointer);
//  }
// Can you see the problem already? 
//  root@kali:~/Documents/c# gcc pointer.c -o pointer.out;./pointer.out
//  And the result is, 1
// As you can see, the Add_Two() method seemed to do nothing. This is because an integer was passed as the argument not a pointer to an integer.  When you want to see
// the changes to an integer in the caller, you need to use a pointer because the pointer points to a constant memory location throughout execution. The following
// changes to Add_Two() and main() would fix the problem.
//  int Add_Two(int* input)
//  {
//      // Dereference the pointer
//      *input += 2;
//  }
// The pointer has to be dereferenced because it holds an address in memory. This can be explained in assembly.
// Here is a memory map of 6 bytes representing the solutions of 2x+1,
//  [Address]                     [Offset]
//            0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
//   0x100 :  01  03  05  07  09  0B  0D  0F  11  13  15  17  19  1B  1D  1F
// Now to point to this I have to to access it by its address not its value.
// For example,
//  0x100 + 0x5 == 0x6 evaluates FALSE
//  [0x100] + 0x5 == 0x6 evaluates TRUE (because in the memory map, 0x01 is stored at 0x100]
// [] denotes a dereferenced pointer in assembly. Dereferencing a pointer just tells the assembler that you intend to access the
// memory at address $integer, rather than add $integer to a value.
// In C this could look like
// *(100) + 0x5 == 0x6 evaluates TRUE
// Though in C, the compiler will handle specific addressing for you rather than dealing with absolute pointers yourself.
// Some more examples,
//  [0x104] == 0x09 TRUE
//  [0x109] + 2 == [0x0A] TRUE
//  0x109 + 2 == [0x0A] FALSE
//  [0x100 + 6] == 0D TRUE
//  [0x10C] == (C * 2) + 1 TRUE
// As you can see in the 3rd example, 0x109 was NOT a pointer(no square braces), therefore the expression could be simplfied as,
//  0x10B == 0x15
// which is obviously not true.
// Lets take a look at how the C code from earlier looks in assembly,
//  0x173 <main+8>     mov    dword ptr [rbp - 0xc], 1
//  0x17a <main+15>    lea rax, [rbp - 0xc]
//  0x17e <main+19>    mov qword ptr[rbp - 8], rax
//  0x182 <main+23>    mov eax, dword ptr[rbp - 0xc]
//  0x185 <main+26>    mov edi, eax
//  0x187 <main+28>    call Add_Two<0x15d>
//  0x18c <main+33>    mov rax, qword ptr [rbp - 8]
//  0x190 <main+37>    mov rdi, rax
//  0x193 <main+40>    call Print_Number <0x135>
// This is the raw disassembly from the GCC command earlier, but I will simplify it a little to make it easier to explain. Read ahead and
// understand the simpler version then come back and look at the former.
//  ; This is where $MyInteger is assigned
//  mov edi, 1 
//
//  ; Then it is put into memory because there is no guarantee that Add_Two wont modify $edi, more so if it was an external library.
//  ; It also ensures that the value of $MyInteger is stored at least somewhere in case the first instruction of Add_TWo modifies $edi etc.
//  mov dword ptr [rbp - 0xc], edi 
//
//  ; Add_Two($edi)
//  call Add_Two<0x15d> 
//
//  ; LEA, Load effective address is often used when dealing with pointers. In this context, it is essentially a compacted add function.
//  ; It stores the effect address of its source operand in its destination(here in rdi). So instead of dereferencing the pointer(despite the []),
//  ; only the resulting address of the pointer is calculated.
//  ; Here it is equivalent to,
//  ;  mov rdi, rbp
//  ;  sub rdi, 0xc
//  ; So at this point, $rdi is a pointer to $MyInteger, because $MyInteger is stored at address [$rbp-0xc]
//  lea rdi, [rbp - 0xc] 
//  ; Print the integer pointed to by $rdi
//  call Print_Number <0x135>
// You now should have a good idea of what a pointer is, if not, you need to understand the concept before reading on. Now, back to C#. C# will always
// use references(a pointer, but less specific to a particular address) to a variable that is a "reference type". As said before, this is generally anything
// that isn't a struct. If you look at https://docs.microsoft.com/en-us/dotnet/api/system.int32 , an integer is actually treat as a struct. Then compared to
// say an array, https://docs.microsoft.com/en-us/dotnet/api/system.array , which inherits from object, and therefore is a reference type. This is generally for
// good reason, because copying out every element of an array into memory every single time would be very slow. However, if you wanted to use a integer pointer in
// a method, you could use the "ref" keyword. Here is the previous C program written in C#, except with a functional Add_Two() method.
//  private static void Add_Two(ref int input)
//  {
//      // Add two
//      input += 2; 
//  }
//  private static int main()
//  {
//      MyInteger = 1;
//      Add_Two(ref MyInteger);
//  }
// At the end of main(), MyInteger is equal to 3, because ref meant that a pointer was passed rather than just an integer value. I don't have a source to back this up,
// but I believe the reason that structs aren't passed by reference is that generally(e.g ulongs, ints) they can fit in a register and so in some cases may never
// even have to go into memory which is obviously a lot faster. Though sometimes they will be put on the stack.
// Here is a demonstration showing the same, but with an int[]. http://prntscr.com/pc7blq
//  private static void Add_Two(ref int[] input)
// 	{
// 		input[0] += 2;	
// 	}
// 	private static void Add_Two(int[] input)
//  {
//      input[0] += 2;
//  }
//  public static void Main()
//  {
//      int[] MyIntArray = new int[] { 0 };
//      Add_Two(MyIntArray);
//      Add_Two(ref MyIntArray);
//      System.Console.WriteLine("Result: " + MyIntArray[0]);
//  }
// As you can see, at the end $MyIntArray[0] was equal to 4. This is because objects are always passed by reference regardless of the ref keyword. Don't be mislead by the
// fact that the contained type is an integer and therefore would be passed by value type. This goes back to how an array is implemented. The pointer to an index of an
// element of an array can be calculated by,
//  (base address of array) + ((size of contained type) * (desired index))
// So to move MyIntArray[5], which has a base address at $rsp, into eax,
//  mov eax, dword ptr [$rsp + 20] ; 20 in decimal
// In effect, the pointer to the integer is inferred from the containing array.
// So to recap,
//  - Structs can only passed by reference with the ref keyword
//  - Objects are always pass by reference
//  - Structs act like reference types when accessed from a container object such as a class
// Our current definition of a pointer to a reference type would look like,
//           -----------------
//           | Addr on stack |
//           -----------------
//                  V
//            X------------X
//            |   Object   |
//            X------------X
// However, objects can also be passed by reference type. This is where our definition of passing by reference has to be changed slightly, because before was a simplified version. In effect
// it is the same but has another layer of depth to consider that most high level programmers will neglect.
// Consider this more accurate definition, where V denotes, "above is a pointer to below".
//           -----------------
//           | Addr on stack |
//           -----------------
//                   V
//   -----------------------------------
//   | Reference stored someplace else |
//   -----------------------------------
//                   V
//             X------------X
//             |   Object   |
//             X------------X
// This is what is called a pointer chain. The address stored on the stack(like in the  assemblyexample earlier) points to the reference stored elsewhere, which is then dereferenced to get the object
// itself. This is where the difference between a pointer and a reference becomes apparent. A reference is the middle pointer in this chain. However, this is not a converse statement.
// A reference is specifically a pointer to an object. They are typically the only pointers dealt with in C#. For the last time, a pointer is a reference if it points to any object.
//   - An address stored on the stack is a pointer. Points to reference. 
//   - Reference is a reference. Is also a pointer. It points to object
//   - Object is some data on the heap, such as an array.
//   - Calling MyMethod(MyByteArray) actually calls MyMethod(Reference to MyByteArray). The passed reference is a NOT a pointer to "Reference stored someplace else", it is a pointer
//     to Object, which is the value of "Reference stored someplace else".
// Lets relate back to how value types can be passed by reference with this new definition.
//  -MyMethod(ref MyInteger) is called.
//  -MyInteger is stored somewhere. Probably on the heap or in the stack. Lets say it is stored at 0x100.
//  -A pointer to MyInteger is passed into MyMethod. E.g MyMethod(0x100)
// Since the compiler knew that there was going to be a ref keyword for arguments into MyMethod(), the method will compile differently to suit the fact that it deals with pointers.
// For example,
//  MyInteger += 1; 
// becomes at compile time,
//  *MyInteger += 1;
// Because the value pointed to by the pointer is what should be changed, rather than the pointer. Otherwise would make the pointer point to something else instead, which would probably be useless
// or if a wilder arithmetic operation was done, throw a seg fault.  In disassembly it would look a lot more like a method that used objects rather than one that only used values because of 
// the use of many pointers.
// Now, when an object is passed as an argument is passed into a method, the value of the reference is given instead of the entire object. The value of the reference is the address of the object
// (The address of the object is a simplification. Each value stored by the class will have its own pointer further down the chain, but this is another story).
// For example,
//  Console.WriteLine("Hello world");
// Before WriteLine() is called, "Hello World" is stored somewhere, then WriteLine is passed a pointer to where it was stored. Remember how arguments are passed to methods at a low level? 
// Specifics depend on calling convention, but it was really a generalisation when I said "Address stored on stack" earlier because maybe the compiler found a way to avoid that and 
// kept it in a register, or even the on the heap if it will be used a lot throughout execution(this really is a special case that I won't go into too much). And yes, strings are reference
// types, I assume because they are translated to char arrays in the front end CIL translator.
// So what happens when the ref keyword is used in conjunction with a reference type. This is where knowledge of the pointer chain becomes very handy. Remember how the value of the reference
// was passed to the method(the value being a pointer to the object)? Well, if everything uses that to access the memory of the object, what if the pointer was a pointer to the reference
// rather than a pointer to the object? This would mean that the reference could be changed, and everywhere else in the program would go along with it. First we have to make some changes
// to our pseudo code to keep up with this. If MyInteger was a value, then became a pointer when the ref keyword, the exact same happens with the pointer to the reference.
// For example without ref,
//  *MyClassReference = null;
// $MyClassReference is a reference, a pointer that points to the memory of MyClass. This needs to be dereferenced in the CIL as what the programmer wrote was an abstraction. This is because
// the annotated pointer chain above says that the given pointer is a reference; the last pointer in the chain before an object.
//  -Memory at *$MyClassReference is set to null(lets go with the idea that nulling a class sets all its memory to nulls, be it [00] or [00] [00] [00] [00]...
// Now if MyClass was passed into the method with a ref keyword.
//  **MyClassReference = null;
// There are two pointers which need to be dereferenced. This is because MyClassReference was passed into the method at a low level was at the top of the earlier pointer chain. 
// It was a pointer to the reference.
//  -MyClassReference holds a pointer to the reference. Dereference it and get the reference; a pointer to the object
//  -Dereference the reference gives the first byte of the object, which is being set to null
// It was a small lie that nulling nulls the entire class. Don't forget the above concept but understand it instead, because it is definitely a way of implementing the idea. However, in .NET
// it is done slightly better due to how the CLR works. How about instead of nulling the entire memory of the class, I just forget about it? That would save time. So instead lets consider this
// idea, (without ref keyword)
//  MyNullByte := 0x00;
//  MyNullBytePointer := &MyNullByte;
//  MyClassReference := MyNullBytePointer;
// Now, instead of pointing to "MyClass", $MyClassReference points to the null byte. This means that now if I wrote,
//  MyByte := *MyClassReference
// It would be 0x00 rather than what the first byte of MyClass was, because the reference is now to a null byte rather to MyClass. However, this is only in the current method right? Because
// the value of the reference the callee was given was changed. The reference to the object is still somewhere else, it just got lost to the callee. This can be thought of exactly like
// value types, and can even be demonstrated in C#.
//  private static void Callee(MyClass MyClassReference)
//  {
//      MyClassReference.Name = "Callee";
//      MyClassReference = null;
//  }
//  public static void Main()
//  {
//      MyClass MyClassReference = new MyClass();
//      MyClassReference.Name = "Main";
//      Callee(MyClassReference);
//      System.Console.WriteLine("Result: " + MyClassReference.Name);
//  }
//  public class MyClass
//  {
//      public string Name;
//  }
// The output would be "Result: Callee". This is because the reference used as normal before it is nulled. After it is nulled, it can still be used in the caller because the callee changed
// its own copy of the reference that was passed as an argument, not the actual reference. If I swapped the lines in Callee() around,
// 	MyClassReference = null;
//  MyClassReference.Name = "Callee";
// The following exception is thrown,
//  Run-time exception: Object reference not set to an instance of an object.
// This is a little a safety net by .NET because the reference to "null" is likely a hard coded value such as 0 which tells the CLR that something went wrong, giving a more demonstrative
// exception. 
// However, what about changing the reference for everybody? What if I want everywhere to see $MyClassReference as null? This is where the ref keyword comes in handy. As shown in pseudo earlier,
// ref gives the extra layer of pointer that has to be dereferenced. But I showed it being dereferenced twice. If I wanted to change the reference globally, the given pointer could be derefenced
// once. At this point I'm at the memory of the reference right? because I dereferenced the pointer to the reference. This can absolutely be changed to anything now, and everything would access
// this new object rather than the former. Lets change the previous example a little bit.
//   private static void Callee(ref MyClass MyClassReference)
//	 {
//		MyClassReference = new MyClass();
//      MyClassReference.LikesAssembly = true;
//   }
//   public static void Main()
//   {
//      MyClass MyClassReference = new MyClass();
//      MyClassReference.Name = "Main";
//      Callee(ref MyClassReference);
//      if (MyClassReference.LikesAssembly)
//      {
//         System.Console.WriteLine("Me too, " + MyClassReference.Name);
//      }
//   }   
//   public class MyClass
//   {
//       public string Name = "MyClass";
//       public bool LikesAssembly = false;
//   }
// The output would be "Me too, MyClass". This is because the reference was globally changed. This is always implied when using a ref keyword, because otherwise you wouldn't require one in the 
// arguments. 
// That concludes everything that needs to be known about pointers and references before understanding how a deep copy works.
// The difference between a deep copy and a shallow copy is the whether a reference is passed to a method or a copy of the object. Generally deep copies are frowned upon because the go against 
// some concepts of object orientation and are not included in compiler optimisations(in effect, not using deep copies is an optimisation), however they have good use in my program that will be
// demonstrated later. Most of what has been talked about so far concerns shallow copies, except passing value types into a method. This is a good example of the desired effect of a deep copy.
// To recap, a value type passed as an argument to a method can be modified by the callee without the caller seeing the changes. The value type is (most likely) copied to the heap, and a reference
// to it is stored somewhere. 
// This is exactly what a deep copy aims to do, but with objects rather than only with value types. Due to it being a "language-hack", there is no generic method for deep copying an object.
// Here I will demonstrate a method of doing so. 
// Consider the following code, where the intention is to output "Hello world" "Hello program".
//  public static void Add_Text(byte[] InputBytes, string Input)
//  {
//      for (int i = 1; i <= Input.Length; i++)
//      {
//          InputBytes[InputBytes.Length - i] = (byte)Input[Input.Length - i];
//      }
//  }
//  public static void Main()
//  {
//      byte[] MyBytes = Encoding.ASCII.GetBytes("Hello        ");
//      byte[] MyOtherBytes = MyBytes;
//      Add_Text(MyBytes, "world  ");
//      Add_Text(MyOtherBytes, "program");
//      Console.WriteLine(Encoding.ASCII.GetString(MyBytes) + "\n" + Encoding.ASCII.GetString(MyOtherBytes));
//  }
// However the output is "Hello program" "Hello program". From what we know about references, the bug is evident. $MyOtherBytes is a reference to the object "MyBytes". Essentially, they are the
// same thing. This is where a deep copy would come in handy(Lets ignore the far better ways of doing this intended function for the sake of simplicity).
// Now, swap the second line of main for,
//  byte[] MyOtherBytes = DeepCopy(MyBytes);
// The output is, http://prntscr.com/pci36q
//  "Hello world"
//  "Hello program"
// Around about the most efficient implementation of an array deep copy function would be,
//	byte[] Buffer = new byte[ToCopy.Length];
//  Array.Copy(ToCopy, Buffer, ToCopy.Length);
//	return Buffer;
// There is some argument for using Buffer.BlockCopy() for maximum performance, but for purposes in a program when byte arrays are rarely longer than 8, the former is more than adequate. Class
// specific deep copy methods can be found in their respective classes(where necessary).  This will only work when the contained type is a value type.
// Back into context, deep copying is a fundamental requirement of the Context class. It allows an element of control and consistency across parts of the program, especially between threads.
// As a static entity, ControlUnit naturally is a threading nightmare, therefore is essential that the necessary that there are internal methods to maintain easy modularity. Deep copying is
// one part of that. It allows a Context to be the heart of the ControlUnit. Every method in the ControlUnit acts on the Context which is referenced from its handle. This is where references
// work absolutely awesome with the Context, so this is mainly the reason the Context cannot be a struct(because the reference is handy). DeepCopy allows the best of both worlds. The context
// wants to be deep copied when another module wants to use it without changes being reflected in the owner handle, such as the Disassembler class, it copies the VM context and runs it itself. 
// An entertaining problem in the past was that the disassembler class would run without deep copying, then the VM context would run using the same memory and at the end of execution it would seem
// like the result of every algorithm was double because its result has already been added to memory when the disassembler ran.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace debugger.Util
{
    public enum FormatType
    {
        // FormatType is a enum used universally for defining and generalising output formats.
        Hex,
        Decimal,
        SignedDecimal,
        String
    }
    public static class Core
    {
        public static Dictionary<T1, T2> DeepCopy<T1, T2>(this Dictionary<T1, T2> toClone)
        {
            // A method for creating a DeepCopy of a dictionary
            // Constraints: 
            //  It does not recursively deep copy as there is no generic method to do so, therefore only
            //  works on key pairs which contain value types.
            // Create the new output dictionary
            Dictionary<T1, T2> Output = new Dictionary<T1, T2>();

            // Convert all the key pairs in toClone into arrays. This also creates a deep copy of
            // the array index values.
            T1[] ClonedKeys = toClone.Keys.ToArray();
            T2[] ClonedValues = toClone.Values.ToArray();

            // Add the deep copied items to the output.
            for (long i = 0; i < ClonedKeys.LongLength; i++)
            {
                Output.Add(ClonedKeys[i], ClonedValues[i]);
            }
            return Output;
        }
        public static List<T> DeepCopy<T>(this List<T> toClone) => toClone.ToArray().ToList();
        public static T[] DeepCopy<T>(this T[] toClone) 
        {
            // This is one of the fastest implementations of a deep copy. Internally, Array.Copy calls native C++ code using P/Invoke. There is no reason why
            // this couldn't be as fast as if it was written in C. 
            // Constraints:
            //  It does not recursively deep copy as there is no generic method to do so, therefore only
            //  works on arrays which contain value types.
            // Performance test: http://prntscr.com/op88f4

            // Create an array to hold the output
            T[] CopyBuffer = new T[toClone.Length];

            // Copy the items of toClone into the CopyBuffer.
            Array.Copy(toClone, CopyBuffer, toClone.LongLength);
            return CopyBuffer;
        }
        public static int CompareTo(this byte[] leftSide, byte[] rightSide, bool signed)
        {
            // A method to compare two byte arrays arithmetically. There are 3 possible return values.
            // 0: The arrays were equal.
            // 1: The left side was greater than the right.
            // 3: The right side was greater than the left.
            
            // PadEqual sign/zero extends(depending on $signed) the smallest operand to the greatest length of the two input arrays
            // If they are both equal in length, nothing happens.
            Bitwise.PadEqual(ref leftSide, ref rightSide, signed);
            
            // The main algorithm will not work on inputs with different signs because it doesn't consider the weight of the MSB in twos compliment.
            if(signed)
            {
                // Get the signs of the inputs
                bool LeftSign = leftSide.IsNegative();
                bool RightSign = rightSide.IsNegative();

                // The following statement will evaluate as true if exactly one of the signs are on. In this case it is immediately clear which is greater. 
                if((LeftSign ^ RightSign) == true)
                {
                    // If the left side is negative, return -1, because the right side must have been positive for the XOR to work.
                    return LeftSign ? -1 : 1;
                }
            }

            // As little endian is exclusively worked with, work backwards. This means that the bytes in the array are compared in order of magnitude.
            // For example, 
            // When comparing two base 10 numbers, 10 and 21, the 0 and 1 digits can be completely ignored, only the tens column is needed to determine the outcome.
            // The numbers could be,
            //  1, 2
            //  10000, 21000
            // The result is the same. This allows the problem to be abstracted. Naturally, the numbers had to be padded to the correct size with their value preserved
            // which has already been done.
            // -The most significant column difference dictates which value is greater. This can be used instead of subtraction to determine the result, which is more
            //  performant because no unecessary operations are carried out.
            for (int i = leftSide.Length - 1; i > 0; i--)
            {
                // If the two indexes are not the same value, the two inputs cannot be equal, the result can be determined here.
                if (leftSide[i] != rightSide[i]) 
                {
                    // Return 1 if the most significant column difference of leftSide is greater than that of rightSide, -1 if it is not.
                    return leftSide[i] > rightSide[i] ? 1 : -1;
                }
            }

            // If the method hasn't returned already, the two inputs must be equal
            return 0; 
        }
        public static bool ListsEqual(List<string> input1, List<string> input2)
        {
            // Simple method for comparing two lists. Using .Equals() on a list does not do this!! It checks if two variables
            // are references to the same list.

            // If the count is not the same they cannot be equal
            if (input1.Count() != input2.Count())
            {
                return false;
            }

            // Iterate compating every element the lists with each other.
            // If any of them are not equal, the list is not equal.
            for (int i = 0; i < input1.Count; i++)
            {
                if (input1[i] != input2[i])
                {
                    return false;
                }
            }
            return true;
        }        
        public static string[] SeparateString(string inputString, string testFor, bool stopAtFirstDifferent = false) => SeparateString(inputString, new string[] { testFor }, stopAtFirstDifferent);
        public static string[] SeparateString(string inputString, string[] testFor, bool stopAtFirstDifferent = false) // output {inputstring with stuff removed, (strings of separated testFors)
        {
            // SeparateString serves a somewhat deprecated purpose, as other methods have been developed in its place, however I still find
            // it quite an interesting idea. It is easier explained through demonstration
            // Say there are two strings(this will be expanded upon later), one that is being tested, say $toTest and
            // one that is being tested against it, say $input. Let $toTest = "_HELLO_WORLD_HELLO_PROGRAM" and $input = $"HELLO"
            // What will happen is that the output will be two strings, one I call $base and the other I will call $output.
            // $base will be "_     _WORLD_     _PROGRAM", and $output will be "_HELLO       HELLO"(Note no spaces at end). As you can see,
            // $input was extracted from $toTest and put in $output. The space taken up by matches of $toTest previously was replaced
            // with spaces.
            // Now, $input does not have to be a single string, it could be an array.  In this case, the same thing would happen and
            // the priority of each input would be the order they are in the array. E.g $input2 would get what is left over in the base
            // from $input1. 
            // In terms of use cases, this was really useful in drawing and markdown as if you wanted to draw a certain string with a
            // different emphasis, you could use this and draw each one differently. However, the newer and clearer method, 
            // Drawing.InsertAtNonZero() has replaced all needs for this, but it may still be useful in the future.
            // $stopAtFirstDifferent is used to exit early if the instances of $testFor strings are not consecutive.
            // E.g,
            //  $input = "0000000000000000123000"
            //  $testFor = "0"
            //  $stopAtFirst.. = false
            // Outputs,
            //  $base = "                123   "
            //  $output = "0000000000000000   000"
            // (With these two in the same array)
            // and,
            //  $input = "0000000000000000123000"
            //  $testFor = "0"
            //  $stopAtFirst.. = true
            // Outputs,
            //  $base = "                123000"
            //  $output = "0000000000000000   "
            // As you can see the search ends after the first non-match.

            // There will be an extra string, thhe base to hold.
            string[] Output = new string[testFor.Length + 1];

            // Start with the base the same as $inputString.
            Output[0] = inputString; 

            // Iterate through the strings to be tested for.
            for (int i = 0; i < testFor.Length; i++)
            {
                // The string to test for will be referred to as $substring

                // Find the index of $testFor in the base.
                int InsertIndex = Output[0].IndexOf(testFor[i]);

                // Whilst $substring is present and not an empty string(this would mess things up badly)
                while (InsertIndex != -1 && testFor[i] != "")
                {
                    // Replace the instance of $substring with spaces by using the index obtained earlier.
                    Output[0] = Output[0].Remove(InsertIndex, testFor[i].Length).Insert(InsertIndex, RepeatString(" ", testFor[i].Length));

                    // Use PadRight() to make up for any spaces that should be added to have the replaced string have the same position in the output as it did the base,
                    // then add $substring on the end of that.
                    Output[i + 1] = Output[i + 1].PadRight(InsertIndex - Output[i + 1].Length) + testFor[i]; 

                    // If $stopAtFirstDifferent and the next index of a string is not equal to the last index + the length of $substring, 
                    // there must be a gap between the two and therefore are not consecutive and that search is over. The next $substring
                    // in $testFor will still be tested.
                    int LastIndex = InsertIndex;
                    InsertIndex = Output[0].IndexOf(testFor[i]);
                    if (stopAtFirstDifferent && InsertIndex != LastIndex + testFor[i].Length)
                    {
                        break;
                    }
                }
            }
            return Output;
        }
        public static string RepeatString(string inputString, int count)
        {
            // A method of repeating strings that is much more performant for small counts than Enumerable.Repeat().
            // 10000 iterations, $count = 1 http://prntscr.com/plk1a2
            // 1 iteration, $count = 10000 http://prntscr.com/plk0vj

            string Output = "";
            for (int i = 0; i < count; i++)
            {
                Output += inputString;
            }
            return Output;
        }
        public static string Itoa(byte[] toConvert, bool addSpaces=false)
        {            
            // Convert a byte array into a string representation of bytes in big endian and hex format.
            // Similar nature to posix Itoa() but obviously different inputs/outputs. Maybe there is 
            // a similar function called Htoi() in C that does this.

            string Output = "";

            // Significant denotes significant 0 bytes in the array.
            // LSB             MSB
            // [00] [10] [00] [00]
            //             ^---^ Insignificant
            // The value in the examples is from the [00] [10].
            // This idea could be demonstrated in decimal numbers also,
            //  000100
            // The first three zeroes are unnecessary, could maybe even be seen as incorrect, as
            // the value is always 100 regardless of how many zeros before it.
            bool Significant = false;

            for (int i = 0; i < toConvert.Length; i++)
            {
                // The current byte in the array.
                byte Cursor = toConvert[toConvert.Length - i - 1];

                if(Significant || Cursor != 0)
                {
                    // Every byte after the first non zero must be significant.
                    Significant = true;

                    // Convert the current byte into a string and pad it left with an insignificant zero if necessary.
                    // E.g consider the bytes [01] [02] [03] [04] in little endian. 
                    // The value they represent is 0x04030201, which is completely different to 0x4321, so that extra
                    // padding needs to be added that ToString("X") does not provide.
                    // I also prefer to have every byte complete like this, some programs would output the example
                    // as 0x4030201 because of the first zero technically being insignificant, but I find it to be
                    // annoying having to count up the digits to see thether the MSB is 04 or 40.
                    Output += Cursor.ToString("X").PadLeft(2, '0');

                    // When $addSpaces, the output would look like
                    //  04 03 02 01
                    // rather than
                    //  04030201
                    // The other condition is to prevent a trailing space.
                    if (i + 1 != toConvert.Length && addSpaces)
                    {
                        Output += " ";
                    }
                }                
            }

            // Return 0 if every byte in the array was insignificant(all zeroes). Althought mathematically
            // zero and no value could be seen as the same, it would be odd to have values missing from
            // disassembly for example.
            if(Output.Length == 0)
            {
                Output = "0";
            }

            return Output;
        }
        public static bool Htoi(char hexChar, out byte output)
        {
            // If the input is between 0 and 9 on the char map, subtract the value of '0' from it.
            // This would work on any character set that has numbers adjacent in ascending order, e.g 0123456789
            if(hexChar >= '0' && hexChar <= '9')
            {
                output = (byte)(hexChar - '0');
                return true;
            }
            
            // If the char is a letter, 0xA must be added because 0xA is 10d.
            else if(hexChar >= 'A' && hexChar <= 'F')
            {
                output = (byte)(hexChar - 'A' + 0xA);
                return true;
            }
            else if (hexChar >= 'a' && hexChar <= 'f')
            {
                output = (byte)(hexChar - 'a' + 0xA);
                return true;
            }

            // Set output to a value that would never be returned from the method and return false to make it super obvious it failed.
            output = 0xFF;
            return false;
        }
        public static T[] Trim<T>(T[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (Convert.ToUInt64(input[input.Length-i-1]) != 0 || i == input.Length - 1)
                {
                    Array.Copy(input, i, input, 0, input.Length - i);
                    Array.Resize(ref input, input.Length - i);// cut after first non zero                                      
                    break;
                }
            }
            return input;
        }
        public static bool TryParseHex(string hex, out byte[] Output) => TryParseHex(Encoding.UTF8.GetBytes(hex), out Output);
        public static bool TryParseHex(byte[] encoded_bytes, out byte[] Output)
        {
            // The following is an method of parsing the UTF-8 encoded bytes in the file as actual hex bytes.
            // Throughout explanation of this particular algorithm I will use "character" to describe a character in the file, e.g A, B, 1, G, ]
            // and use byte to refer to an actual byte in memory. This is not technically accurate but extremely necessary to explain the logic of this algorithm,
            // so forget about this definition afterwards.
            // Understand that a byte written in hex as text is two bytes in the file. E.g, writing B8 is two characters B and 8. The numerical value of "B8" is not 0xB8.
            // This is the premise of the algorithm, converting the characters "B8" into the byte 0xB8.            

            // An array to store the bytes in the file once parsed. As said earlier, two bytes in the file really represents a single byte, so this 2:1 ratio can
            // be used to save memory in the array. It also depicts a worst(memory-wise) case scenario, that every byte is valid hex, e.g no line feeds etc.
            // If there is an odd number of bytes, the greatest possibility is that there are invalid characters in the input,
            // however if that isn't the case assume the last parsable character to be the upper nibble and the lower nibble to be [0000]
            Output = new byte[encoded_bytes.Length / 2 + encoded_bytes.Length % 2];

            // $BytesParsed holds the total number of bytes parsed.
            int NibblesParsed = 0;

            // $CharsLeft holds the number of characters left needed to finish the current byte.
            bool UpperNibble = true;

            // $Caret is the iterator, it holds the position in the array throughout iteration.
            int Caret = 0;

            // Loop the entire array
            while (Caret < encoded_bytes.Length)
            {
                // Here the fortunate fact that each character in a hex byte is a nibble can be used.
                // For example if I had two hex values,
                //  0xD - [00001101]
                //  0x7 - [00000111]
                // 0xD7 would just be,
                //  0xD7 - [11010111]
                // This is an cool relationship between bases that are powers of 2, but some maths can be used to implement it.
                // In the code I take a little bit of a performance shortcut because single byte values are being dealt with, such
                // that there will only ever be two columns, e.g 0xFF not 0x100. Conversely, the single digit value will either be
                // shifted by 4 or none at all. This is because as shown before, one digit represents a nibble.
                // So, if it is the first character in the byte being parsed, it is shifted by 4 because it would be the D in D7 in
                // the earlier example. Otherwise it's just ORed normally.
                byte ParsedNibble;

                // Core.Htoi() will convert a char to the hex byte it visually represented, e.g B -> 0xB.
                if (Htoi((char)encoded_bytes[Caret], out ParsedNibble))
                {
                    // Don't change this order without considering the ternary condition!

                    // If the current character represents the upper nibble(e.g the D in 0xD7), shift it into the upper nibble.
                    // $NibblesParsed / 2 will always give the desired index.
                    // For example,                   
                    // 2 / 2 = 1
                    // 3 / 2 = 1
                    // 4 / 2 = 2
                    // There will always be 2 values(2n/2 and 2n/2 +1) for each index, as there are two nibbles in a byte.
                    // If there were 3 nibbles in a byte, $NP/3 would work so and and so forth.
                    Output[NibblesParsed / 2] |= UpperNibble ? (byte)(ParsedNibble << 4) : ParsedNibble;

                    // XOR $UpperNibble and true; Toggle it. if the upper nibble was just parsed, the next character must be the lower nibble.
                    // If the lower nibble was just parsed, the upper nibble of the next byte must be next(false ^ true = true).
                    UpperNibble ^= true;

                    NibblesParsed++;
                }

                // Whether the byte could be parsed or not, the next character is next to be parsed.
                Caret++;
            }            
           
            if (NibblesParsed > 0)
            {
                // Resize the array to the number of bytes parsed, as the worst case scenario was assumed earlier. Otherwise a whole host of "add    byte ptr [rax], al" would be added to the
                // end of the program because the unused array elements would be [00] [00] [00] [00] ...                
                Array.Resize(ref Output, NibblesParsed / 2 + NibblesParsed % 2);
                return true;
            }
            else
            {
                // The file must be invalid if nothing at all could be parsed.
                return false;
            }
        }   

        public static string ReverseEndian(string input)
        {
            // A method to flip the endianness of a given string that represents bytes.

            // If the length of the input is uneven, add a 0 to the start. This will not affect the value but is a necessary precondition
            // for the algorithm.
            if(input.Length % 2 == 1)
            {
                input = input.Insert(0, "0");
            }

            // Create a new char array to store the result in. Strings are immutable in C#, so every $string1 += $string2 in theory would require
            // the entire string to be relocated in memory every time. A char array allocated enough memory initially so this only has to happen once(afterwards).
            char[] Reversed = new char[input.Length];

            // Reversing endian of a string representation is not as simple as reversing its order, as two characters represent one byte.
            // This means that the character at $i and the character at $i+1 need to have their order preserved.
            // Consider the following,
            //  Big endian      Little endian
            //    0xB4              0xB4
            //   0xB4C3            0xC3B4
            //  0xB4C3D2E1       0xE1D2C3B4
            // In both examples, B4 is the MSByte. 
            // If the string was reversed, B4 would become 4B. By incrementing $i by 2 after each iteration and accounting for so by acting on two
            // characters in each iteration, the order of the characters in each byte is preserved.
            // This is where without the "evening" of the length earlier, there would be an ArrayOutOfBoundsException for any odd length array.
            for (int i = 0; i < input.Length; i+=2)
            {
                Reversed[i] = input[input.Length - i - 2];
                Reversed[i+1] = input[input.Length - i - 1];
            }
            return new string(Reversed);
        }
    }
}
