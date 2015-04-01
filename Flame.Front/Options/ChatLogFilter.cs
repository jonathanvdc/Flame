using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public enum ChatLevel
    {
        None,
        Errors,
        Warnings,
        Messages,
        Silent = Messages,
        Loud
    }

    public class ChatLogFilter : ILogFilter
    {
        public ChatLogFilter(ChatLevel Level)
        {
            this.Level = Level;
        }

        public ChatLevel Level { get; private set; }

        public bool ShouldLogError(LogEntry Error)
        {
            if ("exception".Equals(Error.Name, StringComparison.OrdinalIgnoreCase)) // Only log exceptions when being loud
            {
                return Level == ChatLevel.Loud;
            }
            return Level != ChatLevel.None;
        }

        public bool ShouldLogWarning(LogEntry Warning)
        {
            return Level != ChatLevel.None && Level != ChatLevel.Errors;
        }

        public bool ShouldLogMessage(LogEntry Message)
        {
            return Level != ChatLevel.Warnings && Level != ChatLevel.Errors && Level != ChatLevel.None;
        }

        public bool ShouldLogEvent(LogEntry Status)
        {
            return Level == ChatLevel.Loud;
        }

        public static ChatLevel ParseChatLevel(string Value)
        {
            switch (Value.ToLower())
            {
                case "none":
                case "zipit":
                    return ChatLevel.None;
                case "errors":
                    return ChatLevel.Errors;
                case "warnings":
                case "warn":
                    return ChatLevel.Warnings;
                case "messages":
                    return ChatLevel.Messages;
                case "loud":
                    return ChatLevel.Loud;
                case "silent":
                default:
                    return ChatLevel.Silent;
            }
        }

        public static ILogFilter ParseLogFilter(string Value)
        {
            return new ChatLogFilter(ParseChatLevel(Value));
        }
    }
}
