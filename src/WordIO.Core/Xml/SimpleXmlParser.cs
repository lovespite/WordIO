using System.Globalization;
using System.Text;

namespace WordIO.Core.Xml;

/// <summary>
/// A deliberately small, dependency-free XML parser intended for reading document text.
/// It supports elements, attributes, text, comments, CDATA, the XML declaration, and
/// the standard XML character entities. It does not validate DTDs, namespaces, or schemas.
/// </summary>
public static class SimpleXmlParser
{
    public static XmlDocument Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var parser = new Parser(xml);
        return parser.ParseDocument();
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;

        public Parser(string text)
        {
            _text = text;
        }

        public XmlDocument ParseDocument()
        {
            SkipProlog();

            if (End)
            {
                throw Error("The XML document does not contain a root element.");
            }

            var root = ParseElement();
            SkipProlog();

            if (!End)
            {
                throw Error("Content is not allowed after the root element.");
            }

            return new XmlDocument(root);
        }

        private XmlElement ParseElement()
        {
            Expect('<');
            var name = ReadName();
            var element = new XmlElement(name);

            var selfClosing = ParseAttributes(element);
            if (!selfClosing)
            {
                ParseChildren(element);
            }

            return element;
        }

        private bool ParseAttributes(XmlElement element)
        {
            while (true)
            {
                SkipWhitespace();

                if (End)
                {
                    throw Error($"Element '{element.Name}' is not closed.");
                }

                if (Current == '>')
                {
                    _position++;
                    return false;
                }

                if (Current == '/' && Peek(1) == '>')
                {
                    _position += 2;
                    return true;
                }

                var attributeName = ReadName();
                SkipWhitespace();
                Expect('=');
                SkipWhitespace();

                if (End || Current is not ('"' or '\''))
                {
                    throw Error($"Attribute '{attributeName}' must have a quoted value.");
                }

                var quote = Current;
                _position++;
                var valueStart = _position;

                while (!End && Current != quote)
                {
                    _position++;
                }

                if (End)
                {
                    throw Error($"Attribute '{attributeName}' is missing its closing quote.");
                }

                var rawValue = _text[valueStart.._position];
                _position++;
                element.Attributes[attributeName] = DecodeEntities(rawValue);
            }
        }

        private void ParseChildren(XmlElement element)
        {
            while (true)
            {
                if (End)
                {
                    throw Error($"Element '{element.Name}' is not closed.");
                }

                if (Current != '<')
                {
                    ReadText(element);
                    continue;
                }

                if (Peek(1) == '/')
                {
                    ParseEndTag(element);
                    return;
                }

                if (Peek(1) == '?')
                {
                    _position += 2;
                    SkipUntil("?>");
                    continue;
                }

                if (Peek(1) == '!')
                {
                    _position += 2;

                    if (StartsWithAtCurrent("--"))
                    {
                        _position += 2;
                        SkipUntil("-->");
                    }
                    else if (StartsWithAtCurrent("[CDATA["))
                    {
                        _position += "[CDATA[".Length;
                        var textStart = _position;
                        var endIndex = IndexOf("]]>");

                        if (endIndex < 0)
                        {
                            throw Error("CDATA section is not closed.");
                        }

                        element.AddChild(new XmlText(_text[textStart..endIndex]));
                        _position = endIndex + 3;
                    }
                    else
                    {
                        SkipUntil(">");
                    }

                    continue;
                }

                var child = ParseElement();
                element.AddChild(child);
            }
        }

        private void ReadText(XmlElement element)
        {
            var start = _position;

            while (!End && Current != '<')
            {
                _position++;
            }

            if (_position > start)
            {
                element.AddChild(new XmlText(DecodeEntities(_text[start.._position])));
            }
        }

        private void ParseEndTag(XmlElement element)
        {
            _position += 2;
            var name = ReadName();
            SkipWhitespace();
            Expect('>');

            if (!string.Equals(name, element.Name, StringComparison.Ordinal))
            {
                throw Error($"End tag '{name}' does not match start tag '{element.Name}'.");
            }
        }

        private void SkipProlog()
        {
            while (!End)
            {
                if (Current == '\uFEFF' || char.IsWhiteSpace(Current))
                {
                    _position++;
                    continue;
                }

                if (Current != '<' || Peek(1) is not '?' and not '!')
                {
                    return;
                }

                if (Peek(1) == '?')
                {
                    _position += 2;
                    SkipUntil("?>");
                    continue;
                }

                _position += 2;

                if (StartsWithAtCurrent("--"))
                {
                    _position += 2;
                    SkipUntil("-->");
                }
                else
                {
                    SkipUntil(">");
                }
            }
        }

        private void SkipWhitespace()
        {
            while (!End && char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        private void Expect(char character)
        {
            if (End || Current != character)
            {
                throw Error($"Expected '{character}'.");
            }

            _position++;
        }

        private string ReadName()
        {
            var start = _position;

            while (!End && IsNameCharacter(Current))
            {
                _position++;
            }

            if (start == _position)
            {
                throw Error("Expected an XML name.");
            }

            return _text[start.._position];
        }

        private static bool IsNameCharacter(char character) =>
            char.IsLetterOrDigit(character) ||
            character is '_' or ':' or '-' or '.';

        private void SkipUntil(string delimiter)
        {
            var index = _text.IndexOf(delimiter, _position, StringComparison.Ordinal);
            if (index < 0)
            {
                throw Error($"Expected '{delimiter}'.");
            }

            _position = index + delimiter.Length;
        }

        private int IndexOf(string value) =>
            _text.IndexOf(value, _position, StringComparison.Ordinal);

        private bool StartsWithAtCurrent(string value) =>
            _position + value.Length <= _text.Length &&
            _text.AsSpan(_position, value.Length).SequenceEqual(value);

        private XmlParseException Error(string message) => new(message, _position);

        private bool End => _position >= _text.Length;

        private char Current => _text[_position];

        private char? Peek(int offset)
        {
            var index = _position + offset;
            return index < _text.Length ? _text[index] : null;
        }

        private static string DecodeEntities(string value)
        {
            if (value.IndexOf('&') < 0)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length);

            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] != '&')
                {
                    builder.Append(value[index]);
                    continue;
                }

                var semicolonIndex = value.IndexOf(';', index);
                if (semicolonIndex < 0)
                {
                    builder.Append('&');
                    continue;
                }

                var entity = value[(index + 1)..semicolonIndex];

                switch (entity)
                {
                    case "amp":
                        builder.Append('&');
                        break;
                    case "lt":
                        builder.Append('<');
                        break;
                    case "gt":
                        builder.Append('>');
                        break;
                    case "quot":
                        builder.Append('"');
                        break;
                    case "apos":
                        builder.Append('\'');
                        break;
                    default:
                        if (entity.StartsWith("#x", StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(entity.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexadecimal))
                        {
                            builder.Append((char)hexadecimal);
                        }
                        else if (entity.StartsWith('#') &&
                                 int.TryParse(entity.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalValue))
                        {
                            builder.Append((char)decimalValue);
                        }
                        else
                        {
                            builder.Append(value, index, semicolonIndex - index + 1);
                        }

                        break;
                }

                index = semicolonIndex;
            }

            return builder.ToString();
        }
    }
}
