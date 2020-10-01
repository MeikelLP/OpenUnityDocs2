using CommandLine;

namespace OpenUnityDocs.Converter
{
    public class CommandLineArgs
    {
        [Option('f', "file", HelpText = "File to convert")]
        public string? FilePath { get; set; }
        
        [Option('m', "folder", HelpText = "Folder where the files are enumerated")]
        public string? Folder { get; set; }
        
        [Option('o', "output-directory", Required = true, HelpText = "Where to put the files")]
        public string OutputDir { get; set; }
        
        [Option('c', "clean", Default = false, HelpText = "Clears target directory before executing")]
        public bool Clean { get; set; }
    }
}