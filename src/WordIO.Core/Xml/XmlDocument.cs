namespace WordIO.Core.Xml;

public sealed class XmlDocument
{
    internal XmlDocument(XmlElement? root)
    {
        Root = root;
    }

    public XmlElement? Root { get; }

    public static XmlDocument Parse(string xml) => SimpleXmlParser.Parse(xml);

    public IEnumerable<XmlElement> Descendants(string? localName = null) =>
        Root is null ? Enumerable.Empty<XmlElement>() : Root.Descendants(localName);
}
