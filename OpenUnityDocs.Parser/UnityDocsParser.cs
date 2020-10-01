using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace OpenUnityDocs.Parser
{
    public class UnityDocsParser
    {
        private static readonly Regex EmptyRowsRegex = new Regex("\n{2,}", RegexOptions.Compiled);
        private static readonly Regex NoSpaceAfterDot = new Regex(".(\\s+)$", RegexOptions.Compiled);
        
        public async Task<string> ParseAsync(string filePath)
        {
            var text = await File.ReadAllTextAsync(filePath);
            var doc = new HtmlDocument();
            doc.LoadHtml(text);

            var contentNode = doc.DocumentNode.SelectSingleNode(
                ".//*[@id=\"content-wrap\"]/div/div/div[contains(concat(\" \",normalize-space(@class),\" \"),\" section \")]");
            var breadCrumb =
                contentNode.SelectNodes(
                    ".//div[contains(concat(\" \",normalize-space(@class),\" \"),\" breadcrumbs \")][contains(concat(\" \",normalize-space(@class),\" \"),\" clear \")]/ul/li");
            var data = new Dictionary<string, string>();
            data.Add("parent", breadCrumb.Reverse().Skip(1).Take(1).Single().InnerText.Trim());

            var headerNode = contentNode.SelectSingleNode(".//h1") ?? contentNode.SelectSingleNode(".//h2"); // because unity doesn't like h1 some times
            data.Add("name", headerNode.InnerText.Trim());

            var currentNode = headerNode.NextSibling;
            // fix unity stuff
            FixUnityStuff(contentNode);

            var sb = new StringBuilder();
            sb.Append($"# {data["name"]}\n\n");
            while ((currentNode = currentNode.NextSibling) != null && currentNode.Id != "_content") // stop at id="_content"
            {
                var element = GetMarkdown(currentNode);
                if (!string.IsNullOrWhiteSpace(element))
                {
                    sb.Append(element);
                }
            }

            var output = sb.ToString();
            output = EmptyRowsRegex.Replace(output, "\n\n");
            return output;
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
            if (node.InnerText.Trim() == "" && node.Name != "img") return null;

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
                case "img":
                    output = node.GetAttributeValue("alt", null);
                    break;
                case "#text":
                    if (node.ParentNode.LastChild == node && NoSpaceAfterDot.IsMatch(node.InnerText))
                    {
                        output = NoSpaceAfterDot.Replace(node.InnerText, ".");
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
                case "a":
                    return "[";
                case "code":
                    return "`";
                case "br":
                    return "\n";
                case "strong":
                    return $"**";
                case "pre":
                    return $"{prefix}```csharp\n";
                case "ul":
                case "ol":
                    // if (node.HasParentWithName("li"))
                    // {
                    //     return $"\n";
                    // }

                    return null;
                case "li":
                    
                    if (node.ParentNode.Name == "ul")
                    {
                        return $"{prefix}* ";
                    }
                    else if (node.ParentNode.Name == "ol")
                    {
                        var index = node.ParentNode.ChildNodes.Where(x => x.Name == "li").ToList().IndexOf(node) + 1;
                        return $"{prefix}{index}. ";
                    }

                    throw new ArgumentException("li must be child of ol or ul");
                default:
                    return null;
            }
        }

        private static string? GetSuffix(HtmlNode node)
        {
            switch (node.Name)
            {
                case "strong":
                    return "**";
                case "h1":
                case "h2":
                case "h3":
                    return "\n\n";
                case "p":
                    if (node.ParentNode.Name == "li")
                    {
                        return "\n";
                    }

                    return "\n\n";
                case "li":
                    if (node.ParentNode.ChildNodes.LastOrDefault(x => x.Name == "li") == node)
                    {
                        return null;
                    }

                    return "\n";
                case "ol":
                case "ul":
                    if (node.HasParentWithName("ol") || node.HasParentWithName("ul") || node.HasParentWithName("li"))
                    {
                        return "";
                    }

                    return "\n\n";
                case "code":
                    return "`";
                case "pre":
                    return "```\n\n";
                case "figure":
                    return "\n";
                case "img":
                    var src = node.GetAttributeValue("src", null);
                    return $"]({src})";
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