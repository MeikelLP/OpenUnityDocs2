using HtmlAgilityPack;
using NUnit.Framework;

namespace OpenUnityDocs.Converter.Tests
{
    public class Tests
    {
        [Test]
        public void Test1()
        {
            var result = UnityDocsConverter.GetMarkdown(HtmlNode.CreateNode("<p>abc <strong>def</strong> hij</p>"));
            Assert.AreEqual("abc **def** hij\n\n", result);
        }
    }
}