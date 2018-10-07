namespace UnitTests
{
    public struct ToolCommand
    {
        public ToolCommand(string command, string argument)
        {
            this.Command = command;
            this.Argument = argument;
        }

        public string Command { get; private set; }

        public string Argument { get; private set; }
    }
}
