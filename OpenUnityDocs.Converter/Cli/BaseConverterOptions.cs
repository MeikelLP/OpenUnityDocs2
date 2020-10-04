using System.Collections.Generic;
using CommandLine;

namespace OpenUnityDocs.Converter.Cli
{
    public abstract class BaseConverterOptions : BaseOptions
    {
        [Value(0, Required = true, Default = "*", HelpText = "File or directory to convert")]
        public string InputPath { get; set; }
        
        [Value(1, Default = ".", HelpText = "Where to put the files")]
        public string OutputDir { get; set; }
        
        [Option('c', "clean", Default = false, HelpText = "Clears target directory before executing")]
        public bool IsClean { get; set; }

        [Option('i', "ignore", HelpText = "File names (excluding directory) to ignore if using --folder option")]
        public IEnumerable<string> IgnoredFileNames { get; set; }

        [Option('r', "recursive", Default = false, HelpText = "If path is a folder, scan for files recursively")]
        public bool IsRecursive { get; set; }
    }
}