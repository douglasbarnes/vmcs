using System.IO;
using System;
using System.Collections.Generic;
using static debugger.Logging.LogCode;
namespace debugger.Logging
{    
    public enum LogCode
    {
        DISASSEMBLY_RIPNOTFOUND,
        FLAGSET_INVALIDINPUT,
        TESTCASE_IOERROR,
        TESTCASE_NOT_FOUND,
        TESTCASE_RESULT,
        TESTCASE_DUPLICATE,
        TESTCASE_BADHEX,
        TESTCASE_BADCHECKPOINT,
    }
    public class LoggedException : Exception
    {
        public LoggedException(LogCode logCode, string Interpolation) : base(Logger.FetchMessage(logCode, new string[] { Interpolation }))
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
            { DISASSEMBLY_RIPNOTFOUND, (Severity.CRITICAL, "RIP('{0}') pointed to an address not in the internal disassembly list view dictionary.") },
            { FLAGSET_INVALIDINPUT, (Severity.CRITICAL, "Attempt to access invalid flag, '{0}'.") },
            { TESTCASE_NOT_FOUND, (Severity.WARNING, "Could not run testcase '{0}', file not found.") },
            { TESTCASE_IOERROR, (Severity.WARNING, "Error parsing testcase file {0}:'{1}'.") },
            { TESTCASE_RESULT, (Severity.INFO, "Testcase '{0}' completed with result '{1}'.") },
            { TESTCASE_DUPLICATE, (Severity.WARNING, "Multiple testcases with the name '{0}' found. Only the first will be parsed.") },
            { TESTCASE_BADHEX, (Severity.WARNING, "Could not parse testcase '{0}', make sure there is a <Hex></Hex> tag pair with valid hex bytes between") },
            { TESTCASE_BADCHECKPOINT, (Severity.WARNING, "Could not parse checkpoint in '{0}':'{1}'") }
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
            CRITICAL
        }

        public static void Log(LogCode inputCode, string[] interpolations)
        {

        }
        public static void Log(LogCode inputCode, string interpolation) => Log(inputCode, new string[] { interpolation });
        public static string FetchMessage(LogCode inputCode, string[] interpolations) => String.Format(LogMessages[inputCode].Item2, interpolations);
    }
}
