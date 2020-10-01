using System.Collections.Generic;
using CommandLine;

namespace OpenUnityDocs.Converter
{
    public class CommandLineArgs
    {
        [Option('o', "output-directory", Required = true, HelpText = "Where to put the files")]
        public string OutputDir { get; set; }
        
        [Option('f', "file", SetName = "WithoutDirectory", HelpText = "File to convert")]
        public string? FilePath { get; set; }
        
        [Option('m', "folder", SetName = "WithDirectory", HelpText = "Folder where the files are enumerated")]
        public string? Folder { get; set; }
        
        [Option('c', "clean", SetName = "WithDirectory", Default = false, HelpText = "Clears target directory before executing")]
        public bool Clean { get; set; }

        [Option('i', "ignore", SetName = "WithDirectory", HelpText = "File names (excluding directory) to ignore if using --folder option")]
        public IEnumerable<string> Ignored { get; set; }
    }
}