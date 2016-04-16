namespace MetaWear.NugetImplementation.SampleApp
{
    public class ConsoleLine
    {
        public ConsoleLine(ConsoleEntryType type)
        {
            this.Type = type;
        }

        public ConsoleLine(ConsoleEntryType type, string value)
        {
            this.Type = type;
            this.Value = value;
        }

        public ConsoleEntryType Type { get; }
        public string Value { get; set; }
    }
}
