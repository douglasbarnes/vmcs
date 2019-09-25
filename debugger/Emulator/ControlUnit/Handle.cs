using System;
using System.Collections.Generic;
using System.Threading;
namespace debugger.Emulator
{
    [Flags]
    public enum HandleParameters
    {
        // An enum for defining the behaviour of a handle.

        // Run everything as on paper.
        NONE=0,

        // Opcodes are not executed rather disassembled.
        DISASSEMBLEMODE=1,

        // JMP opcodes are ignored. More precisely, the instruction pointer can only be changed by the ControlUnit itself.
        // Used in conjunction with DISASSEMBLEMODE, but there may be a time when both behaviours need to be separated.
        NOJMP=2,
    }
    public static partial class ControlUnit
    {        
        public static bool IsBusy
        {
            // A thread safe property for knowing whether the ControlUnit is in use by another handle.

            get 
            { 
                lock (ControlUnitLock) 
                { 
                    return _busy; 
                } 
            }

            private set
            {
                lock(ControlUnitLock)
                {
                     _busy = value;
                }
            }
        }
        private static void WaitNotBusy()
        {
            // A method for handles to wait until the ControlUnit is free before executing. This allows a handle to wait until another is finished stepping before executing. If a handle
            // was half way through execution and $ControlUnit.CurrentHandle changed, the already running handle would now affect the new $CurrentHandle rather than its intended. This
            // avoids that scenario.
            while (IsBusy)
            {
                Thread.Sleep(10);
            }
        }
        private static readonly object ControlUnitLock = "L"; // A locking object(as a value type such as a boolean cannot be locked) to ensure the value of _busy is consistent across threads.
        private static bool _busy = false; // The private value for the public property IsBusy.

        public struct Handle
        {
            // A dictionary that pairs all existing handles with a context.
            private static Dictionary<Handle, Context> StoredContexts = new Dictionary<Handle, Context>();
            // A private value holding the next handle id.
            private static int NextHandleID = 0;
            private static int GetNextHandleID 
            { 
                // A private property for accessing $NextHandleID such that it is incremented every time to avoid Handle ID collisions.
                get 
                { 
                    NextHandleID++; 
                    return NextHandleID; 
                } 
                set 
                { 
                    NextHandleID = value; 
                } 
            }
            // Readonly variables that allow other classes to identify a handle and its behaviour.
            public readonly string HandleName;
            public readonly int HandleID;
            public readonly HandleParameters HandleSettings;
            public Handle(string handleName, Context inputContext, HandleParameters inputSettings)
            {
                // A constructor to make a handle out of a given context.
                HandleName = handleName;
                HandleID = GetNextHandleID;
                HandleSettings = inputSettings;

                // If the StoredContexts dictionary already contains an identical handle , adding it again would throw an error.
                if (StoredContexts.ContainsKey(this))
                {
                    StoredContexts[this] = inputContext;
                }
                else
                {
                    StoredContexts.Add(this, inputContext);
                }

            }
            public void Invoke(Action toExecute) // dont invoke run
            {
                // A method to interact with a context or the ControlUnit safely. 
                WaitNotBusy();
                IsBusy = true;
                toExecute.Invoke();
                IsBusy = false;
            }
            public Context DeepCopy() => StoredContexts[this].DeepCopy(); // Return a Context that can be modified without changing the actual context.
            public Context ShallowCopy() => StoredContexts[this]; // Return a reference to an existing Context such that changes to the returned context are reflected in the actual context.
            public Status Run(bool step)
            {
                // A method for telling the ControlUnit to execute the handle. Public access to ControlUnit.Execute() would not be advisable because the handle has to ensure that
                // the ControlUnit isn't already in use and that $ControlUnit.CurrentHandle is equal to the desired handle.
                Status Result = new Status();
                WaitNotBusy();
                IsBusy = true;
                // Set this handle to be the current handle in the ControlUnit.
                if (CurrentHandle != this)
                {
                    // If there is an existing handle set in the ControlUnit, store it.
                    if (CurrentHandle != EmptyHandle)
                    {
                        StoredContexts[CurrentHandle] = CurrentContext;
                    }

                    CurrentHandle = this;
                }
                // Return the $Result status struct that Execute() returned.
                Result = new Func<Status>(() => Execute(step)).Invoke();
                IsBusy = false;
                return Result;
            }
            
            public static bool operator !=(Handle input1, Handle input2)
            {
                // An equality operator for comparing handle IDs.
                return input1.HandleID != input2.HandleID;
            }
            public static bool operator ==(Handle input1, Handle input2)
            {
                // An equality operator for comparing handle IDs.
                return input1.HandleID == input2.HandleID;
            }
        }
    }
   
}
