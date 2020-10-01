using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace OpenUnityDocs.Converter
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineArgs>(args).WithParsed(options => Action(options).Wait());
        }

        private static async Task Action(CommandLineArgs options)
        {
            if (options.Clean)
            {
                if (Directory.Exists(options.OutputDir))
                {
                    Directory.Delete(options.OutputDir, true);
                }
            }
            if (!Directory.Exists(options.OutputDir))
            {
                Directory.CreateDirectory(options.OutputDir);
            }

            var parser = new UnityDocsConverter();

            string[] files;
            if (!string.IsNullOrWhiteSpace(options.Folder))
            {
                if (!options.Ignored.Any())
                {
                    options.Ignored = new[] {"UnityIAPStoreGuides.html"}; // this file is garbage
                }
                files = Directory.GetFiles(options.Folder)!
                    .Where(x => !options.Ignored.Contains(Path.GetFileName(x)))
                    .ToArray();
            }
            else
            {
                files = new[] {options.FilePath!};
            }

            var errors = new Dictionary<string, Exception>();
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
                    errors.Add(filePath, e);
                }
            }

            if (errors.Count > 0)
            {
                var errorLines = string.Join("\n", errors.Select(x => $"{x.Key} => {x.Value.Message}"));
                await Console.Error.WriteLineAsync($"Failed to parse file(s):\n{errorLines}");
                Environment.Exit((int) ExitCode.FAILED_TO_PARSE_SOME_FILES);
            }
        }
    }
}