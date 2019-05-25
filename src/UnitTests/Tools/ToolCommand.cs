namespace UnitTests
{
    public struct ToolCommand
    {
        public ToolCommand(
            string command,
            string argument,
            string expectedOutput = null)
        {
            this.Command = command;
            this.Argument = argument;
            this.ExpectedOutput = expectedOutput;
        }

        public string Command { get; private set; }

        public string Argument { get; private set; }
        public string ExpectedOutput { get; private set; }

        public bool HasExpectedOutput => ExpectedOutput != null;
    }
}
