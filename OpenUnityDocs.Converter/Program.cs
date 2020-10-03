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
            if (options.IsClean)
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

            if (!options.IgnoredFileNames.Any())
            {
                options.IgnoredFileNames = new[]
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
                files = Directory
                    .GetFiles(dirInfo.FullName, "*.html", options.IsRecursive
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly)
                    .Where(x => !options.IgnoredFileNames.Contains(Path.GetFileName(x)))
                    .ToArray();
                if (!options.IsQuiet)
                {
                    Console.WriteLine($"Converting {files.Length}x files...");
                }
            }
            else if (!Path.IsPathRooted(options.InputPath))
            {
                files = Directory.GetFiles(".", options.InputPath)!
                    .Where(x => !options.IgnoredFileNames.Contains(Path.GetFileName(x)))
                    .ToArray();
                if (!options.IsQuiet)
                {
                    Console.WriteLine($"Converting {files.Length}x files...");
                }
            }
            else
            {
                files = new[] {options.InputPath};
            }

            var errors = new Dictionary<string, Exception>();
            using var progressBar = new ProgressBar();

            // parallels tasks
            var startTime = DateTime.UtcNow;

            if (!options.IsQuiet)
            {
                Console.WriteLine($"Starting on {startTime.ToLocalTime()}");
            }
            
            // run all tasks in parallel
            // should massively increase performance on non Windows systems
            var tasks = files.Select((x, i) => Task.Run(() => ParseFileAsync(i, x, errors, options))).ToList();
            
            var entireTask = Task.WhenAll(tasks);
            while (await Task.WhenAny(entireTask, Task.Delay(1000)) != entireTask)
            {
                progressBar.Report(Math.Abs(tasks.Count(x => x.IsCompleted) / (double)files.Length));
            }

            var endTime = DateTime.UtcNow;

            if (!Console.IsOutputRedirected)
            {
                Console.WriteLine();
            }
            if (!options.IsQuiet)
            {
                Console.WriteLine($"Took {(endTime - startTime).TotalSeconds:F2}s");
            }

            if (errors.Count > 0)
            {
                var errorLines = string.Join("\n", errors.Select(x => $"{x.Key} => {x.Value.Message}"));
                await Console.Error.WriteLineAsync($"Failed to parse file(s):\n{errorLines}");
                Environment.Exit((int) ExitCode.FAILED_TO_PARSE_SOME_FILES);
            }
        }

        private static async Task ParseFileAsync(int i, string filePath, IDictionary<string, Exception> errors, CommandLineArgs options)
        {
            try
            {
                var markdown = await UnityDocsConverter.ParseAsync(filePath!);

                // if recursive and file is not in root dir
                // put file into a sub folder inside the out dir
                var outPartDir = Path.GetDirectoryName(filePath)!.Replace(options.InputPath, "").Trim('/', '\\');
                var outDir = Path.Combine(options.OutputDir, outPartDir);
                var outFileName = Path.Combine(outDir, Path.ChangeExtension(Path.GetFileName(filePath), ".md")!);

                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                await File.WriteAllTextAsync(outFileName, markdown.Trim() + "\n");
            }
            catch (Exception e)
            {
                errors.Add(filePath, e);
            }
        }
    }
}