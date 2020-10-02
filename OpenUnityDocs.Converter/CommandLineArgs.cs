using System.Collections.Generic;
using CommandLine;

namespace OpenUnityDocs.Converter
{
    public class CommandLineArgs
    {
        [Value(0, Required = true, Default = "*", HelpText = "File or directory to convert")]
        public string InputPath { get; set; }
        
        [Value(1, Default = ".", HelpText = "Where to put the files")]
        public string OutputDir { get; set; }
        
        [Option('c', "clean", Default = false, HelpText = "Clears target directory before executing")]
        public bool Clean { get; set; }

        [Option('i', "ignore", HelpText = "File names (excluding directory) to ignore if using --folder option")]
        public IEnumerable<string> Ignored { get; set; }
    }
}