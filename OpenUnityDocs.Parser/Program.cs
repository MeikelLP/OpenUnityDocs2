using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace OpenUnityDocs.Parser
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CommandLineArgs>(args).WithParsed(options => Task.Run(() => Action(options)).Wait());
        }

        private static async Task Action(CommandLineArgs options)
        {
            if (options.Clean)
            {
                Directory.Delete(options.OutputDir, true);
            }
            if (!Directory.Exists(options.OutputDir))
            {
                Directory.CreateDirectory(options.OutputDir);
            }

            var parser = new UnityDocsParser();

            var files = !string.IsNullOrWhiteSpace(options.Folder)
                ? Directory.GetFiles(options.Folder)
                : new[] {options.FilePath};
            foreach (var filePath in files)
            {
                try
                {
                    var markdown = await parser.ParseAsync(filePath!);

                    var outFileName = Path.Combine(options.OutputDir, Path.ChangeExtension(Path.GetFileName(filePath), ".md")!);
                    await File.WriteAllTextAsync(outFileName, markdown.Trim() + "\n");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse file \"{filePath}\"");
                }
            }
        }
    }
}