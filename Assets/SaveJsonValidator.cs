using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Validates persisted JSON before Unity's deserializer sees it. The reader is
/// bounded, rejects duplicate object keys, and never throws on malformed input.
/// </summary>
internal static class SaveJsonValidator
{
    internal const int CurrentSchemaVersion = 1;
    private const int MaximumJsonLength = 4 * 1024 * 1024;
    private const int MaximumDepth = 64;
    private const int MaximumKeyLength = 256;

    internal struct Field
    {
        internal char Kind;
        internal string Scalar;

        internal bool TryGetInt(out int value)
        {
            value = 0;
            return (Kind == '-' || (Kind >= '0' && Kind <= '9'))
                && int.TryParse(Scalar, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        internal bool TryGetBool(out bool value)
        {
            value = Kind == 't';
            return Kind == 't' || Kind == 'f';
        }
    }

    internal static bool TryValidateWorldState(string json, int maximumSchemaVersion, out int schemaVersion)
    {
        schemaVersion = 0; // Missing version is the supported original world-state format.
        if (!TryReadObject(json, out Dictionary<string, Field> fields)
            || !fields.TryGetValue("puzzleStates", out Field puzzles) || puzzles.Kind != '[')
        {
            return false;
        }

        if (!fields.TryGetValue("saveSchemaVersion", out Field version))
        {
            return true;
        }

        return version.TryGetInt(out schemaVersion) && schemaVersion >= 1
            && schemaVersion <= maximumSchemaVersion;
    }

    internal static bool TryReadObject(string json, out Dictionary<string, Field> fields)
    {
        fields = null;
        if (string.IsNullOrEmpty(json) || json.Length > MaximumJsonLength)
        {
            return false;
        }

        Reader reader = new Reader(json);
        return reader.TryRead(out fields);
    }

    private sealed class Reader
    {
        private readonly string json;
        private int position;
        private Dictionary<string, Field> rootFields;

        internal Reader(string json) { this.json = json; }

        internal bool TryRead(out Dictionary<string, Field> fields)
        {
            fields = null;
            SkipWhitespace();
            if (!ReadObject(0, true)) return false;
            SkipWhitespace();
            if (position != json.Length) return false;
            fields = rootFields;
            return true;
        }

        private bool ReadObject(int depth, bool isRoot)
        {
            if (depth >= MaximumDepth || !Consume('{')) return false;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            if (isRoot) rootFields = new Dictionary<string, Field>(StringComparer.Ordinal);
            SkipWhitespace();
            if (Consume('}')) return true;
            while (position < json.Length)
            {
                if (!ReadString(true, out string key) || !keys.Add(key)) return false;
                SkipWhitespace();
                if (!Consume(':')) return false;
                SkipWhitespace();
                if (position >= json.Length) return false;
                int start = position;
                char kind = json[position];
                if (!ReadValue(depth + 1)) return false;
                if (isRoot)
                {
                    rootFields.Add(key, new Field
                    {
                        Kind = kind,
                        Scalar = kind == '{' || kind == '[' || kind == '"'
                            ? null : json.Substring(start, position - start)
                    });
                }
                SkipWhitespace();
                if (Consume('}')) return true;
                if (!Consume(',')) return false;
                SkipWhitespace();
            }
            return false;
        }

        private bool ReadArray(int depth)
        {
            if (depth >= MaximumDepth || !Consume('[')) return false;
            SkipWhitespace();
            if (Consume(']')) return true;
            while (position < json.Length)
            {
                if (!ReadValue(depth + 1)) return false;
                SkipWhitespace();
                if (Consume(']')) return true;
                if (!Consume(',')) return false;
                SkipWhitespace();
            }
            return false;
        }

        private bool ReadValue(int depth)
        {
            if (position >= json.Length || depth >= MaximumDepth) return false;
            char c = json[position];
            if (c == '{') return ReadObject(depth, false);
            if (c == '[') return ReadArray(depth);
            if (c == '"') return ReadString(false, out _);
            if (c == 't') return ReadLiteral("true");
            if (c == 'f') return ReadLiteral("false");
            if (c == 'n') return ReadLiteral("null");
            return ReadNumber();
        }

        private bool ReadString(bool retain, out string value)
        {
            value = null;
            if (!Consume('"')) return false;
            StringBuilder decoded = retain ? new StringBuilder() : null;
            while (position < json.Length)
            {
                char c = json[position++];
                if (c == '"')
                {
                    value = decoded != null ? decoded.ToString() : null;
                    return true;
                }
                if (c < 0x20) return false;
                if (c == '\\')
                {
                    if (position >= json.Length) return false;
                    char escape = json[position++];
                    switch (escape)
                    {
                        case '"': c = '"'; break;
                        case '\\': c = '\\'; break;
                        case '/': c = '/'; break;
                        case 'b': c = '\b'; break;
                        case 'f': c = '\f'; break;
                        case 'n': c = '\n'; break;
                        case 'r': c = '\r'; break;
                        case 't': c = '\t'; break;
                        case 'u':
                            if (json.Length - position < 4) return false;
                            int codePoint = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                char digit = json[position++];
                                int hex = digit >= '0' && digit <= '9' ? digit - '0'
                                    : digit >= 'a' && digit <= 'f' ? digit - 'a' + 10
                                    : digit >= 'A' && digit <= 'F' ? digit - 'A' + 10 : -1;
                                if (hex < 0) return false;
                                codePoint = codePoint * 16 + hex;
                            }
                            c = (char)codePoint;
                            break;
                        default: return false;
                    }
                }
                if (decoded != null)
                {
                    if (decoded.Length >= MaximumKeyLength) return false;
                    decoded.Append(c);
                }
            }
            return false;
        }

        private bool ReadNumber()
        {
            Consume('-');
            if (position >= json.Length) return false;
            if (!Consume('0'))
            {
                if (json[position] < '1' || json[position] > '9') return false;
                ReadDigits();
            }
            if (Consume('.'))
            {
                int start = position;
                ReadDigits();
                if (start == position) return false;
            }
            if (position < json.Length && (json[position] == 'e' || json[position] == 'E'))
            {
                position++;
                if (!Consume('+')) Consume('-');
                int start = position;
                ReadDigits();
                if (start == position) return false;
            }
            return true;
        }

        private void ReadDigits()
        {
            while (position < json.Length && json[position] >= '0' && json[position] <= '9') position++;
        }

        private bool ReadLiteral(string literal)
        {
            if (json.Length - position < literal.Length) return false;
            for (int i = 0; i < literal.Length; i++)
            {
                if (json[position + i] != literal[i]) return false;
            }
            position += literal.Length;
            return true;
        }

        private bool Consume(char expected)
        {
            if (position >= json.Length || json[position] != expected) return false;
            position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (position < json.Length)
            {
                char c = json[position];
                if (c != ' ' && c != '\t' && c != '\r' && c != '\n') break;
                position++;
            }
        }
    }
}
