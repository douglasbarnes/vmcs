using System;
using System.Collections.Generic;
using System.Threading;
namespace debugger.Emulator
{
    [Flags]
    public enum HandleParameters
    {
        NONE=0,
        DISASSEMBLEMODE=1,
        NOJMP=2,
    }
    public static partial class ControlUnit
    {        
        public static bool IsBusy
        {
            get { lock (ControlUnitLock) { return _busy; } }
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
            while (IsBusy)
            {
                Thread.Sleep(10);
            }
        }
        private static readonly object ControlUnitLock = "L"; 
        private static bool _busy = false;

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
            public Context DeepCopy() => StoredContexts[this].DeepCopy();
            public Context ShallowCopy() => StoredContexts[this];
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
