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
            Parser.Default.ParseArguments<CommandLineArgs>(args).WithParsed(options => Run(options).Wait());
        }

        private static async Task Run(CommandLineArgs options)
        {
            if (options.Clean)
            {
                if (Directory.Exists(options.OutputDir)) Directory.Delete(options.OutputDir, true);
            }

            if (!Directory.Exists(options.OutputDir)) Directory.CreateDirectory(options.OutputDir);

            var dirInfo = new DirectoryInfo(options.InputPath);
            if (!dirInfo.Exists && !File.Exists(options.InputPath))
            {
                await Console.Error.WriteAsync($"Directory or file does not exist: {options.InputPath}");
                Environment.Exit((int) ExitCode.INPUT_INVALID);
            }

            if (!options.Ignored.Any())
            {
                options.Ignored = new[]
                {
                    "UnityIAPStoreGuides.html", 
                    "30_search.html", 
                    "AssetImporters.ScriptedImporter.Awake.html", 
                    "Experimental.AssetImporters.ScriptedImporter.Awake.html", 
                    "EditorWindow.OnDidOpenScene.html"
                }; // these files are useless
            }

            // use dir if path is a dir
            // else if not absolute path try find file in current dir
            // else (if is absolute path) use file path
            string[] files;
            if (dirInfo.Exists)
            {
                files = Directory.GetFiles(dirInfo.FullName)!
                    .Where(x => !options.Ignored.Contains(Path.GetFileName(x)))
                    .ToArray();
            }
            else if (!Path.IsPathRooted(options.InputPath))
            {
                files = Directory.GetFiles(".", options.InputPath)!
                    .Where(x => !options.Ignored.Contains(Path.GetFileName(x)))
                    .ToArray();
            }
            else
            {
                files = new[] {options.InputPath};
            }

            var errors = new Dictionary<string, Exception>();
            foreach (var filePath in files)
            {
                try
                {
                    var markdown = await UnityDocsConverter.ParseAsync(filePath!);

                    var outFileName = Path.Combine(options.OutputDir,
                        Path.ChangeExtension(Path.GetFileName(filePath), ".md")!);
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