using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using OpenUnityDocs.Converter.Cli;

namespace OpenUnityDocs.Converter
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Html2MarkdownOptions, Markdown2HtmlOptions>(args)
                .WithParsed<Html2MarkdownOptions>(options => Run(options).Wait())
                .WithParsed<Markdown2HtmlOptions>(options => Run(options).Wait());
        }

        private static async Task Run(BaseConverterOptions options)
        {
            if (options.IsClean)
            {
                if (Directory.Exists(options.OutputDir)) Directory.Delete(options.OutputDir, true);
            }

            if (!Directory.Exists(options.OutputDir)) Directory.CreateDirectory(options.OutputDir);

            IConverter converter;
            if (options is Html2MarkdownOptions)
            {
                converter = new Html2MdConverter();
            }
            else if (options is Markdown2HtmlOptions)
            {
                converter = new Markdown2HtmlConverter();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

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
                    .GetFiles(dirInfo.FullName, $"*{converter.InFileEnding}", options.IsRecursive
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

            var startTime = DateTime.UtcNow;
            var errors = new Dictionary<string, Exception>();
            using (var progressBar = new ProgressBar())
            {
                if (!options.IsQuiet)
                {
                    Console.WriteLine($"Starting on {startTime.ToLocalTime()}");
                }

                // run all tasks in parallel
                // should massively increase performance on non Windows systems
                var tasks = files.Select(x => Task.Run(() => ConvertFileAsync(converter, x, errors, options))).ToList();

                var entireTask = Task.WhenAll(tasks);
                while (await Task.WhenAny(entireTask, Task.Delay(1000)) != entireTask)
                {
                    progressBar.Report(Math.Abs(tasks.Count(x => x.IsCompleted) / (double) files.Length));
                }
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

        private static async Task ConvertFileAsync(IConverter converter, string filePath,
            IDictionary<string, Exception> errors, BaseConverterOptions options)
        {
            try
            {
                var result = await converter.ConvertAsync(filePath!);

                // if recursive and file is not in root dir
                // put file into a sub folder inside the out dir
                var outPartDir = Path.GetDirectoryName(filePath)!
                    .Replace(
                        Directory.Exists(options.InputPath)
                            ? options.InputPath
                            : Path.GetDirectoryName(options.InputPath)!, "").Trim('/', '\\');
                var outDir = Path.Combine(options.OutputDir, outPartDir);
                var outFileName = Path.Combine(outDir,
                    Path.ChangeExtension(Path.GetFileName(filePath), converter.OutFileEnding)!);

                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                await File.WriteAllTextAsync(outFileName, result.Trim() + "\n");
            }
            catch (Exception e)
            {
                errors.Add(filePath, e);
            }
        }
    }
}