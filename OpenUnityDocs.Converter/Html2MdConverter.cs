using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace OpenUnityDocs.Converter
{
    /// <summary>
    /// Special converter for Unity
    /// </summary>
    public class Html2MdConverter : IConverter
    {
        private static readonly Regex EmptyRowsRegex = new Regex("\n{2,}", RegexOptions.Compiled);
        private static readonly Regex NoSpaceAfterSentenceFinalizer = new Regex("([\\.|;])(\\s+)$", RegexOptions.Compiled);

        public async Task<string> ConvertAsync(string filePath)
        {
            var text = await File.ReadAllTextAsync(filePath);
            var doc = new HtmlDocument();
            doc.LoadHtml(text);

            var contentNode = doc.DocumentNode.SelectSingleNode(
                ".//*[@id=\"content-wrap\"]/div/div/div[contains(concat(\" \",normalize-space(@class),\" \"),\" section \")]");
            // var breadCrumb =
            //     contentNode.SelectNodes(
            //         ".//div[contains(concat(\" \",normalize-space(@class),\" \"),\" breadcrumbs \")][contains(concat(\" \",normalize-space(@class),\" \"),\" clear \")]/ul/li");
            // var parent = breadCrumb.Reverse().Skip(1).Take(1).Single().InnerText.Trim();

            // fix unity stuff
            FixUnityStuff(contentNode);

            string output;
            if (contentNode.SelectNodes(
                ".//*[contains(concat(\" \",normalize-space(@class),\" \"),\" subsection \")]") != null)
            {
                // ScriptReference
                output = ParseScriptReference(contentNode);
            }
            else
            {
                // Manual
                output = ParseManual(contentNode);
            }

            output = EmptyRowsRegex.Replace(output, "\n\n");
            return output;
        }

        public string OutFileEnding => ".md";
        public string InFileEnding => ".html";

        private static string ParseScriptReference(HtmlNode containerNode)
        {
            var relevantNodes =
                containerNode.SelectNodes(
                    ".//*[contains(concat(\" \",normalize-space(@class),\" \"),\" subsection \")]");

            var name = containerNode.SelectSingleNode(".//h1").InnerText;

            var sb = new StringBuilder();
            sb.Append($"# {name}\n\n");
            foreach (var relevantNode in relevantNodes)
            {
                var element = GetMarkdown(relevantNode);
                sb.Append(element);
            }

            return sb.ToString();
        }

        private static string ParseManual(HtmlNode containerNode)
        {
            var headerNode =
                containerNode.SelectSingleNode(".//h1") ??
                containerNode.SelectSingleNode(".//h2"); // because unity doesn't like h1 some times
            var name = headerNode.InnerText.Trim();

            var currentNode = headerNode.NextSibling;

            var sb = new StringBuilder();
            sb.Append($"# {name}\n\n");
            while ((currentNode = currentNode.NextSibling) != null && currentNode.Id != "_content"
            ) // stop at id="_content"
            {
                var element = GetMarkdown(currentNode);
                sb.Append(element);
            }

            return sb.ToString();
        }

        private static void FixUnityStuff(HtmlNode contentNode)
        {
            var tooltips =
                contentNode.SelectNodes(
                    ".//span[contains(concat(\" \",normalize-space(@class),\" \"),\" tooltiptext \")]");
            if (tooltips != null)
            {
                foreach (var tooltip in tooltips)
                {
                    // remove tooltips for now
                    tooltip.Remove();
                }
            }

            var codeNodes = contentNode.SelectNodes(".//pre/code");
            if (codeNodes != null)
            {
                foreach (var code in codeNodes)
                {
                    // why do they put code into pre ???
                    var codeBlock = code.InnerText;
                    var parent = code.ParentNode;
                    parent.RemoveAllChildren();
                    parent.AppendChild(HtmlNode.CreateNode(codeBlock));
                }
            }
        }

        public static string? GetMarkdown(HtmlNode node)
        {
            if (node.InnerText.Trim() == "" && node.Name != "img" && node.Name != "br") return null;

            var sb = new StringBuilder();
            sb.Append(GetPrefix(node));
            if (node.HasChildNodes)
            {
                foreach (var childNode in node.ChildNodes)
                {
                    sb.Append(GetMarkdown(childNode));
                }
            }
            else
            {
                sb.Append(GetContent(node));
            }

            sb.Append(GetSuffix(node));

            return sb.ToString();
        }

        private static string GetContent(HtmlNode node)
        {
            string? output;
            switch (node.Name)
            {
                case "br":
                    return "\n";
                case "img":
                    output = node.GetAttributeValue("alt", null) ?? "";
                    break;
                case "#text":
                    if (node.ParentNode.LastChild == node && NoSpaceAfterSentenceFinalizer.IsMatch(node.InnerText))
                    {
                        output = NoSpaceAfterSentenceFinalizer.Replace(node.InnerText, "$1");
                    }
                    else
                    {
                        output = node.InnerText;
                    }

                    break;
                default:
                    output = node.InnerText;
                    break;
            }

            return output.Replace("“", "\"").Replace("”", "\"");
        }

        private static string? GetPrefix(HtmlNode node)
        {
            var indentionLevel = -1;
            HtmlNode parent = node;
            while ((parent = parent.ParentNode) != null)
            {
                if (node.Name == "li" && (parent.Name == "ul" || parent.Name == "ol") ||
                    (node.Name == "ul" || node.Name == "ol") && parent.Name == "li")
                {
                    indentionLevel++;
                }
            }

            var prefix = indentionLevel > 0
                ? string.Join("", Enumerable.Range(0, indentionLevel).Select(x => "    "))
                : "";

            switch (node.Name)
            {
                case "div" when node.HasClass("sig-block"):
                    // example ScriptReference/Rigidbody-collisionDetectionMode.html
                    // script block covered as div
                    return $"{prefix}```csharp\n";
                case "table":
                    var cols = node.SelectSingleNode(".//tr")?.SelectNodes(".//td")?.Count;
                    if (cols == null) return null; // table empty
                    if (node.SelectSingleNode(".//thead") == null || node.SelectSingleNode(".//th") == null)
                    {
                        var headers = Enumerable.Range(0, cols.Value).Select(x => " --- ");
                        var seperator = $"| {prefix}{string.Join("|", headers)} |\n";
                        return
                            $"| {string.Join("|", Enumerable.Range(0, cols.Value).Select(x => "     "))} |\n{seperator}"; // empty - else invalid syntax
                    }
                    else
                    {
                        return null;
                    }
                case "thead":
                    var headerColumns = node.SelectNodes(".//th") ?? node.SelectNodes(".//td");
                    if (headerColumns == null) return null; // handled in "table"
                    return $"{string.Join("|", headerColumns.Select(x => $" {x.InnerText} "))}\n";
                case "tr":
                    return "|";
                case "h1":
                    return $"{prefix}# ";
                case "h2":
                    return $"{prefix}## ";
                case "h3":
                    return $"{prefix}### ";
                case "img":
                    return $"{prefix}![";
                case "figcaption":
                    return $"\n{prefix}| ";
                case "a" when !node.HasParentWithClass("sig-block"): // don't convert links in code blocks as this is not markdown compliant
                    return "[";
                case "code":
                    return "`";
                case "strong":
                    return "**";
                case "pre":
                    return $"{prefix}```csharp\n";
                case "ul":
                case "ol":
                    // if (node.HasParentWithName("li"))
                    // {
                    //     return $"\n";
                    // }

                    return null;
                case "li" when node.ParentNode.Name == "ul":
                    return $"{prefix}* ";
                case "li" when node.ParentNode.Name == "ol":
                    var index = node.ParentNode.ChildNodes.Where(x => x.Name == "li").ToList().IndexOf(node) + 1;
                    return $"{prefix}{index}. ";
                default:
                    return null;
            }
        }

        private static string? GetSuffix(HtmlNode node)
        {
            switch (node.Name)
            {
                case "div" when node.HasClass("sig-block"):
                    // example ScriptReference/Rigidbody-collisionDetectionMode.html
                    // script block covered as div
                    return "\n```\n\n";
                case "td":
                    return "|";
                case "strong":
                    return "**";
                case "table":
                case "h1":
                case "h2":
                case "h3":
                    return "\n\n";
                case "p" when node.ParentNode.Name == "li":
                    return "\n";
                case "p":
                    return "\n\n";
                case "li" when node.ParentNode.ChildNodes.LastOrDefault(x => x.Name == "li") == node:
                    return null;
                case "li":
                    return "\n";
                case "ol":
                case "ul" when node.HasParentWithName("ol") || node.HasParentWithName("ul") || node.HasParentWithName("li"):
                    return "";
                case "ul":
                    return "\n\n";
                case "code":
                    return "`";
                case "pre":
                    return "```\n\n";
                case "tr":
                case "figure":
                    return "\n";
                case "img":
                    return $"]({node.GetAttributeValue("src", null)})";
                case "a" when node.HasParentWithClass("sig-block"):
                    return " "; // unity special case
                case "a":
                    var title = node.GetAttributeValue("title", null);
                    title = title != null ? $" \"{title}\"" : null;
                    var href = node.GetAttributeValue("href", null);
                    href = href.Replace(".html", ".md");
                    return $"]({href}{title})";
                default:
                    return null;
            }
        }
    }
}