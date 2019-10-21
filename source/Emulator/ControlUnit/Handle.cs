// Handle allows an easy way to have consistent access to a stored context. In essence, it is the method of
// registering a hypervisor class into the actual ControlUnit. The handle makes sure each hypervisor is allocated
// the control unit such that it is not disturbed by other hypervisors. This could be seen as concurrency in its
// purest form as it is a basic implementation of "Context switching". Minimal time is wasted on the control unit,
// as once execution for one handle is finished(hypervisors with registered handles will be referred to as handles),
// another that was waiting for execution(this is automatically done when handle-sensitive methods are called) can
// then execute. In the current program, this window is very small as no handle executes for very long, but obviously
// not every user will have a model computer, so may experience different latencies. By convention I recommend keeping
// a handle for as long as possible.
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
        NONE = 0,

        // Opcodes are not executed rather disassembled.
        DISASSEMBLE = 1,

        // JMP opcodes are ignored. More precisely, the instruction pointer can only be changed by the ControlUnit itself.
        // Used in conjunction with DISASSEMBLEMODE, but there may be a time when both behaviours need to be separated.
        NOJMP = 2,

        // The handle will ignore breakpoints set for the Context. Not very useful at the minute as the disassembler(which it is intended for) runs before any breakpoints are set,
        // however in the future a custom IO file format for loading contexts with breakpoints saved in them may find this useful. It will still return at the end of memory.
        NOBREAK = 4,
    }
    public static partial class ControlUnit
    {
        public static bool IsBusy
        {
            // A thread safe property for knowing whether the ControlUnit is in use by another handle.
            // This is achieved through locking and therefore returning the thread constant value of $_busy.
            // This prevents other hypervisors from accidently taking over the control unit whilst another
            // hypervisor is using it.
            get
            {
                lock (ControlUnitLock)
                {
                    return _busy;
                }
            }

            private set
            {
                lock (ControlUnitLock)
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
            public void UpdateContext(Context inputContext)
            {
                WaitNotBusy();
                IsBusy = true;
                StoredContexts[this] = inputContext;
                IsBusy = false;
            }
            public static Context GetContextByID(int id)
            {
                // Iterate through contexts searching for a particular id. Return null if not found.               
                foreach (var KeyPair in StoredContexts)
                {
                    if (KeyPair.Key.HandleID == id)
                    {
                        return KeyPair.Value;
                    }
                }
                return null;
            }
            public void Invoke(Action toExecute)
            {
                // A method to interact with a context or the ControlUnit safely. DONT INVOKE HANDLE.RUN() OR HANDLE.INVOKE() RECURSIVELY!!
                // A very useful method for when creating new hypervisors, as usage of this function will likely be what differentiates one from another.
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

                WaitNotBusy();
                IsBusy = true;

                // Set this handle to be the current handle in the ControlUnit.
                CurrentHandle = this;

                // Return the $Result status struct that Execute() returned.
                Status Result = Execute(step);
                IsBusy = false;
                return Result;
            }
        }
    }

}
