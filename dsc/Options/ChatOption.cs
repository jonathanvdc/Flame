using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
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
    }


    public class ChatOption : IBuildOption<ILogFilter>
    {
        public ChatLevel GetLevel(string Input)
        {
            switch (Input.ToLower())
            {
                case "none":
                case "zipit":
                    return ChatLevel.None;
                case "errors":
                    return ChatLevel.Errors;
                case "warnings":
                case "warn":
                    return ChatLevel.Warnings;
                case "silent":
                    return ChatLevel.Silent;
                case "messages":
                    return ChatLevel.Messages;
                case "loud":
                default:
                    return ChatLevel.Loud;
            }
        }

        public ILogFilter GetValue(string[] Input)
        {
            return new ChatLogFilter(GetLevel(Input[0]));
        }

        public string Key
        {
            get { return "chat"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
}
