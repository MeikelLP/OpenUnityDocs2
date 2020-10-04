using CommandLine;

namespace OpenUnityDocs.Converter.Cli
{
    public abstract class BaseOptions
    {
        [Option('q', "quiet", Default = false, HelpText = "No info output")]
        public bool IsQuiet { get; set; }
    }
}