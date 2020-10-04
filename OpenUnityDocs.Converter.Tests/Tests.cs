using HtmlAgilityPack;
using NUnit.Framework;

namespace OpenUnityDocs.Converter.Tests
{
    public class Tests
    {
        [Test]
        public void Test1()
        {
            var result = Html2MdConverter.GetMarkdown(HtmlNode.CreateNode("<p>abc <strong>def</strong> hij</p>"));
            Assert.AreEqual("abc **def** hij\n\n", result);
        }
    }
}