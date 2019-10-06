using System.IO;
using System;
using System.Collections.Generic;
using static debugger.Logging.LogCode;
namespace debugger.Logging
{    
    public enum LogCode
    {  
        NONE,
        INVALID_OPCODE,
        DIVIDE_BY_ZERO,
        REGISTER_BADLEN,
        REGISTER_NOTREADY,
        DISASSEMBLY_RIPNOTFOUND,
        FLAGSET_INVALIDINPUT,
        TESTCASE_RUNTIME,
        TESTCASE_PARSEFAIL,
        TESTCASE_IOERROR,
        TESTCASE_NOT_FOUND,
        TESTCASE_RESULT,
        TESTCASE_DUPLICATE,
        TESTCASE_BADHEX,
        TESTCASE_BADCHECKPOINT,
        TESTCASE_NOHEX,
        IO_FILENOTFOUND,
        IO_INVALIDFILE,
    }
    public class LoggedException : Exception
    {
        public LoggedException(LogCode logCode, string Interpolation="") : base(Logger.FetchMessage(logCode, new string[] { Interpolation }))
        {
            Initialise(logCode, new string[] { Interpolation });
        }
        public LoggedException(LogCode logCode, string[] interpolations) : base(Logger.FetchMessage(logCode, interpolations))
        {
            Initialise(logCode, interpolations);
        }
        private void Initialise(LogCode logCode, string[] interpolations)
        {
            Logger.Log(logCode, interpolations);            
            Environment.Exit(1);
        }
    }
    public static class Logger
    {
        private static readonly Dictionary<LogCode, (Severity, string)> LogMessages = new Dictionary<LogCode, (Severity, string)>()
        {
            { INVALID_OPCODE, (Severity.EXECUTION, "Invalid opcode was read. Execution from now is undefined.") },
            { DIVIDE_BY_ZERO, (Severity.EXECUTION, "Attempt to divide by zero. Execution from now is undefined.") },
            { REGISTER_NOTREADY, (Severity.ERROR, "Attempt to access register before it had a size assigned.") },
            { REGISTER_BADLEN, (Severity.ERROR, "Register was set to a length that did not match it's capacity.") },
            { DISASSEMBLY_RIPNOTFOUND, (Severity.CRITICAL, "RIP('{0}') pointed to an address not in the internal disassembly list view dictionary.") },
            { FLAGSET_INVALIDINPUT, (Severity.CRITICAL, "Attempt to access invalid flag, '{0}'.") },
            { TESTCASE_RUNTIME, (Severity.ERROR, "Runtime error in testcase. Execution stopped before expected end.") },
            { TESTCASE_PARSEFAIL, (Severity.WARNING, "Data in '{0}' could not be parsed: '{1}'.") },
            { TESTCASE_IOERROR, (Severity.WARNING, "Error parsing testcase file {0}:'{1}'.") },
            { TESTCASE_NOT_FOUND, (Severity.WARNING, "Could not run testcase '{0}', file not found.") },            
            { TESTCASE_RESULT, (Severity.INFO, "Testcase '{0}' completed with result '{1}'.") },
            { TESTCASE_DUPLICATE, (Severity.WARNING, "Multiple testcases with the name '{0}' found. Only the first will be parsed.") },
            { TESTCASE_BADHEX, (Severity.WARNING, "Could not parse testcase '{0}', utf-8 characters in the hex tags could not be parsed as bytes") },
            { TESTCASE_BADCHECKPOINT, (Severity.WARNING, "Could not parse checkpoint in '{0}':'{1}'") },
            { TESTCASE_NOHEX, (Severity.WARNING, "Could not parse testcase '{0}', there was no <Hex></Hex> tags with shellcode present") },
            { IO_FILENOTFOUND, (Severity.WARNING, "Could not open file '{0}'.") },
            { IO_INVALIDFILE, (Severity.WARNING, "File contained invalid data: '{0}'") },
        };
        private static Dictionary<Severity, char> SeverityPrefix = new Dictionary<Severity, char>()
        {
            { Severity.INFO, '?' },
            { Severity.WARNING, '*' },
            { Severity.ERROR, 'X' },
            { Severity.CRITICAL, '!' },
        };
        private enum Severity
        {
            INFO,
            WARNING,
            ERROR,
            CRITICAL,
            EXECUTION
        }

        public static void Log(LogCode inputCode, string[] interpolations)
        {
            System.Windows.Forms.MessageBox.Show(string.Format(LogMessages[inputCode].Item2, interpolations));
        }
        public static void Log(LogCode inputCode, string interpolation) => Log(inputCode, new string[] { interpolation });
        public static string FetchMessage(LogCode inputCode, string[] interpolations) => String.Format(LogMessages[inputCode].Item2, interpolations);
    }
}
