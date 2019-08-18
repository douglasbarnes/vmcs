using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
namespace debugger.Emulator
{
    [Flags]
    public enum HandleParameters
    {
        NONE=0,
        DISASSEMBLEMODE=1,
    }
    public static partial class ControlUnit
    {        
        public static bool IsBusy
        {
            get { return Interlocked.Read(ref _busy) == 1; }
            private set
            {
                if (value == true && Interlocked.Read(ref _busy) == 0)
                {
                    Interlocked.Increment(ref _busy);
                }
                else if (value == false && Interlocked.Read(ref _busy) == 1)
                {
                    Interlocked.Decrement(ref _busy);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        private static void WaitNotBusy()
        {
            while (Interlocked.Read(ref _busy) == 1)
            {
                Thread.Sleep(1);
            }
        }
       
        private static long _busy = 0;

        public struct Handle
        {
            private static Dictionary<Handle, Context> StoredContexts = new Dictionary<Handle, Context>();
            private static int NextHandleID = 0;
            private static int GetNextHandleID { get { NextHandleID++; return NextHandleID; } set { NextHandleID = value; } }
            public readonly string HandleName;
            public readonly int HandleID;
            public readonly HandleParameters HandleSettings;
            public Handle(string handleName, Context inputContext, HandleParameters inputSettings)
            {
                HandleName = handleName;
                HandleID = GetNextHandleID;
                HandleSettings = inputSettings;
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
                WaitNotBusy();
                IsBusy = true;
                toExecute.Invoke();
                IsBusy = false;
            }
            public Context ShallowCopy() => StoredContexts[this].DeepCopy();
            public Context DeepCopy() => StoredContexts[this];
            public Status Run(bool step)
            {
                Status Result = new Status();
                WaitNotBusy();
                IsBusy = true;
                if (CurrentHandle != this)
                {
                    if (CurrentHandle != EmptyHandle)
                    {
                        StoredContexts[CurrentHandle] = CurrentContext;
                    }

                    CurrentHandle = this;
                }
                Result = new Func<Status>(() => Execute(step)).Invoke();
                IsBusy = false;
                return Result;
            }
            public void Dispose()
            {
                if (CurrentHandle == this)
                {
                    CurrentHandle = EmptyHandle;
                }// could zero everything here if wanted to
                StoredContexts.Remove(this);
            }
            public static bool operator !=(Handle input1, Handle input2)
            {
                return input1.HandleID != input2.HandleID;
            }
            public static bool operator ==(Handle input1, Handle input2)
            {
                return input1.HandleID == input2.HandleID;
            }
        }
    }
   
}
