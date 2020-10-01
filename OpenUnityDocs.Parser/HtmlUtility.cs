using HtmlAgilityPack;

namespace OpenUnityDocs.Parser
{
    public static class HtmlUtility
    {
        public static bool HasParentWithName(this HtmlNode node, string name)
        {
            HtmlNode parent = node;
            while ((parent = parent.ParentNode) != null)
            {
                if (name == parent.Name) return true;
            }

            return false;
        }
    }
}