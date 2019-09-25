// The FlagSet class is an artificially crafted flags register that allows flags to be set and read easily, as
// opposed to if the flags were stored in an actual register, and would have to be modified by shifting bits.
// E.g, the carry flag the first bit of the eflags register.
//      If I was writing a script for GDB and wanted to set the CF, I would have to run the command,
//      "set eflags |= 1" (OR the register by 2, which would set the first bit on)
// This is very non-intuitive for somebody who is not as advanced of a user. To set the carry in my program,
// it is much simpler, 
//      ControlUnit.SetFlags(new FlagSet() { Carry = FlagState.ON });
// Whilst having all the flags stored in one register(as it is on the processor certainly has its benefits,
// having an intuitive programming interface is more aligned with the intentions of my program.
using System.Linq;
using debugger.Util;
using debugger.Logging;
namespace debugger.Emulator
{
    public enum FlagState
    {
        // Flags can have 3 states: on, off or never specified at all.
        // If it is undefined, the flag will be ignored when Overlap() is called        
        // In previous versions, flags were stored as boolean values, which definitely
        // allowed for simpler code within the class. However having the option to leave
        // a flag as UNDEFINED greatly simplifies the usage of the FlagSet outside of the class

        UNDEFINED=0,
        OFF=1,
        ON=2,        
    }
    public struct FlagSet
    {
        public FlagState Carry;
        public FlagState Auxiliary;
        public FlagState Overflow;  
        public FlagState Zero;     
        public FlagState Sign;     
        public FlagState Parity;
        public FlagState Direction; 
        public FlagState Interrupt; 
        public FlagSet(FlagState initialiseAs = FlagState.UNDEFINED)
        {
            // Construct a flag set with all flags equal to $intialiseAs
            Carry = intialiseAs;
            Auxiliary = intialiseAs;
            Overflow = initialiseAs; 
            Zero = intialiseAs; 
            Sign = intialiseAs;
            Parity = initialiseAs;
            Direction = intialiseAs;
            Interrupt = intialiseAs;
        }
        public void Set(FlagState setTo)
        {
            // Change all the flags in the struct to $setTo
            this = new FlagSet(setTo);
        }
        public FlagSet(byte[] input) 
        {
            // A constructor that can set ZF, SF, PF to what they are defined as in most cases            
            Carry = FlagState.UNDEFINED;
            Auxiliary = FlagState.UNDEFINED;
            Overflow = FlagState.UNDEFINED;
            Direction = FlagState.UNDEFINED;
            Interrupt = FlagState.UNDEFINED;
            // ZF is set if $input is equal to zero.
            Zero = input.IsZero() ? FlagState.ON : FlagState.OFF;
            // SF is set if $input has a negative sign in twos compliment form.
            Sign = input.IsNegative() ? FlagState.ON : FlagState.OFF;
            // PF is set if the number of bits on in the first byte of $input is even.
            // e,g 
            // input[0] == 0b0000 ; PF
            // input[0] == 0b1000 ; NO PF
            // input[0] == 0b1010 ; PF
            Parity = Bitwise.GetBits(input[0]).Count(x => x == '1') % 2 == 0 ? FlagState.ON : FlagState.OFF;
        }
        public FlagSet Overlap(FlagSet input) => new FlagSet()
        {
            // Return a new flag set based on $this and $input.
            // If a flag in $input is FlagState.UNDEFINED, the value of that flag in $this is used instead(which could also be FlagState.UNDEFINED)
            // Otherwise, that flag in the returned FlagSet is equal to the same flag in $input.
            // For example,
            //  $Input.Carry == FlagState.UNDEFINED
            //  $this.Carry == FlagState.ON
            //  New Carry = FlagState.ON
            // Another example,
            //  $Input.Overflow == FlagState.ON
            //  $this.Overflow == FlagState.OFF
            //  New Carry = FlagState.ON
            Carry = input.Carry == FlagState.UNDEFINED ? Carry : input.Carry,
            Auxiliary = input.Auxiliary == FlagState.UNDEFINED ? Auxiliary : input.Auxiliary,
            Overflow = input.Overflow == FlagState.UNDEFINED ? Overflow : input.Overflow,
            Zero = input.Zero == FlagState.UNDEFINED ? Zero : input.Zero,
            Sign = input.Sign == FlagState.UNDEFINED ? Sign : input.Sign,
            Parity = input.Parity == FlagState.UNDEFINED ? Parity : input.Parity,
            Direction = input.Direction == FlagState.UNDEFINED ? Direction : input.Direction,
            Interrupt = input.Interrupt == FlagState.UNDEFINED ? Interrupt : input.Interrupt
        };

        public bool EqualsOrUndefined(FlagSet toCompare)
        {
            // Return whether two flag sets have the same flags set ON. OFF and UNDEFINED are treat as the same.                        
            return toCompare.ToString() == ToString();
        }
        public FlagSet And(FlagSet toCompare) => new FlagSet()
        {
            // Perform a bitwise AND on every flag(in the sense that ON == 1, OFF | UNDEFINED == 0).
            // If both flags of a kind are ON in $toCompare and $this, that flag will be ON in the result. Otherwise, it will be OFF(no UNDEFINED).
            Carry = (Carry & toCompare.Carry)              == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Overflow = (Overflow & toCompare.Overflow)     == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Sign = (Sign & toCompare.Overflow)             == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Zero = (Zero & toCompare.Zero)                 == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Auxiliary = (Auxiliary & toCompare.Auxiliary)  == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Parity = (Parity & toCompare.Parity)           == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Direction = (Direction & toCompare.Direction)  == FlagState.ON ? FlagState.ON : FlagState.OFF ,
            Interrupt = (Interrupt & toCompare.Interrupt)   == FlagState.ON ? FlagState.ON : FlagState.OFF
        }
        public FlagState this[string name]
        {
            // An index attribute allows a flag to be fetched by entering its name in the accessor.
            // e.g Flag["carry"] would be valid, Flag["invalid"] would not.
            get
            {
                return name.ToLower() switch
                {
                    "carry" => Carry,
                    "sign" => Sign,
                    "overflow" => Overflow,
                    "parity" => Parity,
                    "zero" => Zero,
                    "auxiliary" => Auxiliary,
                    "direction" => Direction,
                    "interrupt" => Interrupt,
                    _ => throw new LoggedException(LogCode.FLAGSET_INVALIDINPUT, name)

                };
            }
            // Also provides the same for setting a flag.
            set
            {
                switch (name.ToLower())
                {
                    case "carry":
                        Carry = value;
                        return;
                    case "sign":
                        Sign = value;
                        return;
                    case "overflow":
                        Overflow = value;
                        return;
                    case "parity":
                        Parity = value;
                        return;
                    case "zero":
                        Zero = value;
                        return;
                    case "auxiliary":
                        Auxiliary = value;
                        return;
                    case "direction":
                        Direction = value;
                        return;
                    case "interrupt":
                        Interrupt = value;
                        return;
                    default:
                        throw new LoggedException(LogCode.FLAGSET_INVALIDINPUT, name);
                }
            }
        }
        public static bool ValidateString(string input)
        {
            // A method to check whether $input is a valid name for a flag that can be used in the index accessor.
            input = input.ToLower();
            return
                input == "zero"
             || input == "carry"
             || input == "overflow"
             || input == "sign"
             || input == "parity"
             || input == "auxiliary"
             || input == "direction"
             || input == "interrupt";
        }
        public override string ToString()
        {
            // Returns a string of flag names in the order CF,OF,SF,ZF,AF,PF,DF,IF. If a flag is not set ON, it is not appended to the string.
            string Output = "";

            //If carry == FlagState.ON, append CF
            Output += Carry == FlagState.ON ? "CF" : "";
            Output += Overflow == FlagState.ON ? "OF" : "";
            Output += Sign == FlagState.ON ? "SF" : "";
            Output += Zero == FlagState.ON ? "ZF" : "";
            Output += Auxiliary == FlagState.ON ? "AF" : "";
            Output += Parity == FlagState.ON ? "PF" : "";
            Output += Direction == FlagState.ON ? "DF" : "";
            Output += Interrupt == FlagState.ON ? "IF" : "";
            return Output;
        }
    }
}
