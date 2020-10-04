using System.IO;
using System.Threading.Tasks;
using Markdig;
using Markdig.Renderers;

namespace OpenUnityDocs.Converter
{
    public class Markdown2HtmlConverter : IConverter
    {
        public async Task<string> ConvertAsync(string filePath)
        {
            var input = await File.ReadAllTextAsync(filePath);
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            
            var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            renderer.LinkRewriter = raw => Path.GetExtension(raw) == ".md" ? raw.Replace(".md", ".html") : raw;
            pipeline.Setup(renderer);
            var md = Markdown.Parse(input, pipeline);
            renderer.Render(md);
            await writer.FlushAsync();

            return writer.ToString();
        }

        public string OutFileEnding => ".html";
        public string InFileEnding => ".md";
    }
}