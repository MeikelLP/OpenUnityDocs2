using HtmlAgilityPack;

namespace OpenUnityDocs.Converter
{
    public static class HtmlUtility
    {
        public static bool HasParentWithName(this HtmlNode node, string name)
        {
            HtmlNode parent = node;
            while ((parent = parent.ParentNode) != null)
            {
                if (parent.Name == name) return true;
            }

            return false;
        }
        public static bool HasParentWithClass(this HtmlNode node, string className)
        {
            HtmlNode parent = node;
            while ((parent = parent.ParentNode) != null)
            {
                if (parent.HasClass(className)) return true;
            }

            return false;
        }
    }
}