// The Log class provides a generalised way for handling errors, exceptions, and events in general throughout the program. 
// Each event has a LogCode, which allows the output of the log to be more generalised. For example, if there is an error
// in a testcase checkpoint, the error message will always start the same, "Could not parse checkpoint in '{0}':'{1}'".
// {0} and {1} are left to be customised by the caller to display a more specific cause to the user. This also means that
// the starting constant part of the string only has to be written once, reducing the chances of typos and general inconsistencies 
// getting through the system. Every .Log() call is also logged in a file, found at $LogPath, which is Log.txt in the current 
// execution directory. This makes it easier to go back in the future and see the cause of an error. These logs are also timestamped.
// Each LogCode also has a severity tied to it. Currently this changes the line in the file to a different prefix, e.g an info
// severity would start with [?] and an error would start with [X]. This is not displayed in the message box to the user, as its 
// main purpose is to navigate the log file quickly. Another feature is the LoggedException. It provides the ability to catch
// the exception before it is logged. This means that it could be handled without ever being logged, which would be useful in
// the case that it could be handled at somepoint. When developing new modules, I would strongly recommend using this class along side
// as for programs in general it gets very messy when logs are incomplete/misleading. To implementg a new LogCode, all that must be
// done is: Register it as a new entry in the enum definition, Create a new key pair in $LogMessages of which the value is a
// severity-string tuple, where the string is the error message.
using System;
using System.Collections.Generic;
using System.IO;
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
        public LoggedException(LogCode logCode, string Interpolation = "") : base(Logger.FetchMessage(logCode, new string[] { Interpolation }))
        {
            Initialise(logCode, new string[] { Interpolation });
        }
        public LoggedException(LogCode logCode, string[] interpolations) : base(Logger.FetchMessage(logCode, interpolations))
        {
            Initialise(logCode, interpolations);
        }

        private void Initialise(LogCode logCode, string[] interpolations)
        {
            // Log the exception.
            Logger.Log(logCode, interpolations);

            // Exit with error code 1. Anything that isn't zero should be fairly conventional here.
            Environment.Exit(1);
        }
    }
    public static class Logger
    {
        private static readonly FileInfo LogPath = new FileInfo(Environment.CurrentDirectory + "\\Log.txt");
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
        private static readonly Dictionary<Severity, char> SeverityPrefix = new Dictionary<Severity, char>()
        {
            { Severity.INFO, '?' },
            { Severity.WARNING, '*' },
            { Severity.ERROR, 'X' },
            { Severity.CRITICAL, '!' },
            { Severity.EXECUTION, '$' },
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
            // Fetch the definition of the log code from the dictionary.
            (Severity, string) CodeInfo = LogMessages[inputCode];

            // Format the error message with the caller provided interpolations(the customisations to the error message).
            string ErrorMessage = string.Format(CodeInfo.Item2, interpolations);

            // Display the error message tot the user.
            System.Windows.Forms.MessageBox.Show(ErrorMessage);

            // Create the log file if it does not exist
            if (!LogPath.Exists)
            {
                LogPath.Create();
            }

            // Write the error message along with its severity and a timestamp to the log file. Remember that streamwriter does
            // not automatically write a new line.
            using (StreamWriter stream = LogPath.AppendText())
            {
                stream.Write($"[{SeverityPrefix[CodeInfo.Item1]}][{DateTime.UtcNow.ToString()}]{((int)inputCode).ToString()}" + ErrorMessage + "\n");
            }

        }

        // Shortcut for formatting with a single string rather than a string[]
        public static void Log(LogCode inputCode, string interpolation) => Log(inputCode, new string[] { interpolation });

        // A method to fetch the error message of a log code without actually logging it.
        public static string FetchMessage(LogCode inputCode, string[] interpolations) => string.Format(LogMessages[inputCode].Item2, interpolations);
    }
}
