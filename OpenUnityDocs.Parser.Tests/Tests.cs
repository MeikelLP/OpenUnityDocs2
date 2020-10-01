using HtmlAgilityPack;
using NUnit.Framework;

namespace OpenUnityDocs.Parser.Tests
{
    public class Tests
    {
        [Test]
        public void Test1()
        {
            var result = UnityDocsParser.GetMarkdown(HtmlNode.CreateNode("<p>abc <strong>def</strong> hij</p>"));
            Assert.AreEqual("abc **def** hij\n\n", result);
        }
    }
}