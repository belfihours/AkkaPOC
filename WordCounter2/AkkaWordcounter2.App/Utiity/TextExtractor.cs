using HtmlAgilityPack;

namespace AkkaWordcounter2.App.Utiity;

public static class TextExtractor
{
    /// <summary>
    /// Extracts raw text from a HtmlDocument
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static IEnumerable<string> ExtractText(HtmlDocument document)
    {
        var root = document.DocumentNode;
        var nodes = root.Descendants()
            .Where(n => n.NodeType == HtmlNodeType.Text
                && n.ParentNode.Name != "script"
                && n.ParentNode.Name != "style");
        foreach (var node in nodes)
        {
            var text = node.InnerText.Trim();
            if(!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    public static IEnumerable<string> ExtractTokens(string text)
    {
        var tokens = text.Split([' ',  '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            yield return token.Trim();
        }
    }
}