using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    /// <summary>
    /// An enumeration of possible log entry types.
    /// This is a flag enum: more than one of its
    /// bit fields may be set to true.
    /// </summary>
    [Flags]
    public enum LogEntryType : byte
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Message = 4,
        Event = 8
    }

    /// <summary>
    /// Defines a type of log filter that operates based on a 
    /// number of boolean flags, and a set of reclassification rules.
    /// This is more powerful than a ChatLogFilter.
    /// </summary>
    public class FlagLogFilter : ILogFilter
    {
        public FlagLogFilter(bool LogErrors, bool LogWarnings, bool LogMessages, bool LogEvents, 
            IReadOnlyDictionary<string, LogEntryType> EntryReclassificationRules)
            : this(CreateAllowedEntryTypes(LogErrors, LogWarnings, LogMessages, LogEvents), EntryReclassificationRules)
        {
        }
        public FlagLogFilter(LogEntryType AllowedEntryTypes, IReadOnlyDictionary<string, LogEntryType> EntryReclassificationRules)
        {
            this.AllowedEntryTypes = AllowedEntryTypes;
            this.EntryReclassificationRules = EntryReclassificationRules;
        }

        /// <summary>
        /// Gets this log's allowed log entry types, as a flag enum.
        /// </summary>
        public LogEntryType AllowedEntryTypes { get; private set; }

        /// <summary>
        /// Gets a boolean flag that tells if errors are to be logged.
        /// </summary>
        public bool LogErrors { get { return (AllowedEntryTypes & LogEntryType.Error) == LogEntryType.Error; } }
        
        /// <summary>
        /// Gets a boolean flag that tells if warnings are to be logged.
        /// </summary>
        public bool LogWarnings { get { return (AllowedEntryTypes & LogEntryType.Warning) == LogEntryType.Warning; } }

        /// <summary>
        /// Gets a boolean flag that tells if messages are to be logged.
        /// </summary>
        public bool LogMessages { get { return (AllowedEntryTypes & LogEntryType.Message) == LogEntryType.Message; } }

        /// <summary>
        /// Gets a boolean flag that tells if events are to be logged.
        /// </summary>
        public bool LogEvents { get { return (AllowedEntryTypes & LogEntryType.Event) == LogEntryType.Event; } }

        /// <summary>
        /// Gets a map of entry names that are logged only if they would
        /// have been logged had they been the associated entry type.
        /// </summary>
        public IReadOnlyDictionary<string, LogEntryType> EntryReclassificationRules { get; private set; }

        private bool ShouldLogImpl(LogEntryType Type)
        {
            return (AllowedEntryTypes & Type) == Type;
        }

        private bool ShouldLog(LogEntry Entry, LogEntryType Type)
        {
            LogEntryType alternateType;
            if (EntryReclassificationRules.TryGetValue(Entry.Name, out alternateType))
            {
                return ShouldLogImpl(alternateType);
            }
            else
            {
                return ShouldLogImpl(Type);
            }
        }

        public bool ShouldLogError(LogEntry Error)
        {
            return ShouldLog(Error, LogEntryType.Error);
        }

        public bool ShouldLogWarning(LogEntry Warning)
        {
            return ShouldLog(Warning, LogEntryType.Warning);
        }

        public bool ShouldLogMessage(LogEntry Message)
        {
            return ShouldLog(Message, LogEntryType.Message);
        }

        public bool ShouldLogEvent(LogEntry Status)
        {
            return ShouldLog(Status, LogEntryType.Event);
        }

        #region Static

        /// <summary>
        /// Gets the -chat option name.
        /// </summary>
        public const string ChatName = "chat";
        /// <summary>
        /// Gets the -w (no warnings) option name.
        /// </summary>
        public const string NoWarningsName = "w";
        /// <summary>
        /// Gets the -v (verbose) option name.
        /// </summary>
        public const string VerboseName = "v";

        /// <summary>
        /// Creates an allowed entry type flag enum from the
        /// given boolean parameters.
        /// </summary>
        /// <param name="AllowErrors"></param>
        /// <param name="AllowWarnings"></param>
        /// <param name="AllowMessages"></param>
        /// <param name="AllowEvents"></param>
        /// <returns></returns>
        public static LogEntryType CreateAllowedEntryTypes(bool AllowErrors, bool AllowWarnings, bool AllowMessages, bool AllowEvents)
        {
            LogEntryType result = LogEntryType.None;
            if (AllowErrors)
            {
                result |= LogEntryType.Error;
            }
            if (AllowWarnings)
            {
                result |= LogEntryType.Warning;
            }
            if (AllowMessages)
            {
                result |= LogEntryType.Message;
            }
            if (AllowEvents)
            {
                result |= LogEntryType.Event;
            }
            return result;
        }

        /// <summary>
        /// Gets the default reclassification rules for log filters.
        /// The following names (case-insensitive) are reclassified as events:
        /// <list type="bullet">
        /// <item><description>exception</description></item>
        /// </list>
        /// </summary>
        public static IReadOnlyDictionary<string, LogEntryType> DefaultReclassificationRules
        {
            get
            {
                return new Dictionary<string, LogEntryType>(StringComparer.OrdinalIgnoreCase)
                {
                    { "exception", LogEntryType.Event }
                };
            }
        }

        /// <summary>
        /// Gets the list of chat level names.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, LogEntryType> ChatLevelNames =
            new Dictionary<string, LogEntryType>(StringComparer.OrdinalIgnoreCase)
        {
            { "none", LogEntryType.None },
            { "zipit", LogEntryType.None },
            { "errors", LogEntryType.Error },
            { "warnings", LogEntryType.Warning | LogEntryType.Error },
            { "warn", LogEntryType.Warning | LogEntryType.Error },
            { "messages", LogEntryType.Message | LogEntryType.Warning | LogEntryType.Error },
            { "silent", LogEntryType.Message | LogEntryType.Warning | LogEntryType.Error },
            { "loud", LogEntryType.Event | LogEntryType.Message | LogEntryType.Warning | LogEntryType.Error }
        };

        /// <summary>
        /// Parses the given chat level string.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static LogEntryType ParseChatLevel(string Input)
        {
            LogEntryType result;
            if (ChatLevelNames.TryGetValue(Input, out result))
            {
                return result;
            }
            else
            {
                return LogEntryType.Message | LogEntryType.Warning | LogEntryType.Error;
            }
        }

        /// <summary>
        /// Parses a log filter from the given compiler options 
        /// and reclassification rules.
        /// The following options are taken into account:
        /// <list type="bullet">
        /// <item><description>-chat</description></item>
        /// <item><description>-w</description></item>
        /// <item><description>-v</description></item>
        /// </list>
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="ReclassificationRules"></param>
        /// <returns></returns>
        public static FlagLogFilter ParseFilter(ICompilerOptions Options, IReadOnlyDictionary<string, LogEntryType> ReclassificationRules)
        {
            var chat = ParseChatLevel(Options.GetOption<string>(ChatName, ""));
            if (Options.GetOption<bool>(NoWarningsName, false))
            {
                chat &= ~LogEntryType.Warning;
            }
            if (Options.GetOption<bool>(VerboseName, false))
            {
                chat |= LogEntryType.Event;
            }
            return new FlagLogFilter(chat, ReclassificationRules);
        }

        /// <summary>
        /// Parses a log filter from the given compiler options 
        /// and the default reclassification rules.
        /// The following options are taken into account:
        /// <list type="bullet">
        /// <item><description>-chat</description></item>
        /// <item><description>-w</description></item>
        /// <item><description>-v</description></item>
        /// </list>
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static FlagLogFilter ParseFilter(ICompilerOptions Options)
        {
            return ParseFilter(Options, DefaultReclassificationRules);
        }

        #endregion
    }
}
