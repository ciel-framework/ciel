using System.Text;

namespace Ciel.Breeze;

public struct Token
{
    public enum Kind
    {
        // Template structure
        Text,

        SafeSplicerStart, // '{{'
        SafeSplicerEnd, // '}}'
        SpicySplicerStart, // '{%'
        SpicySplicerEnd, // '%}'

        // Block tags: {tag expr} ... {/tag}
        TagOpen, // "{tag ...}" header (Content = "tagname")
        TagClose, // "{/tag}" (Content = "tagname")

        // Expression punctuation
        LParen, // '('
        RParen, // ')'
        Colon, // ':'
        Comma, // ','

        // Operators / keywords-as-operators
        And, // 'and'
        Or, // 'or'
        In, // 'in'
        Plus, // '+'
        Minus, // '-'
        Star, // '*'
        Slash, // '/'
        Percent, // '%'
        Not, // 'not'

        // Identifiers & literals
        Identifier,
        This, // 'this'
        True, // 'true'
        False, // 'false'
        Null, // 'null'
        Number,
        String,

        // Raw text escapes
        EscapeName, // §newline
        EscapeHex, // §ABCD;

        Eof
    }

    public Kind Type { get; init; }
    public string Content { get; init; }

    public override string ToString()
    {
        return $"{Type}: \"{Content}\"";
    }
}

public class Lexer
{
    private readonly StringBuilder _current = new();
    private CodeMode _codeMode = CodeMode.None;
    private State _state = State.InRawText;
    private char _stringQuote;

    public Token Emit(Token.Kind type)
    {
        var content = _current.ToString();
        _current.Clear();

        return new Token
        {
            Type = type,
            Content = content
        };
    }

    public Token EmitFixed(Token.Kind type, string content = "")
    {
        _current.Clear();
        return new Token
        {
            Type = type,
            Content = content
        };
    }

    public Token? EmitIfNotEmpty(Token.Kind type)
    {
        if (_current.Length == 0)
            return null;

        return Emit(type);
    }

    private static bool IsIdentStart(char c)
    {
        return char.IsLetter(c) || c == '_' || c == '$';
    }

    private static bool IsIdentPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '$';
    }

    private static bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') ||
               (c >= 'a' && c <= 'f') ||
               (c >= 'A' && c <= 'F');
    }

    private Token.Kind ClassifyIdentifier(string id)
    {
        return id switch
        {
            "this" => Token.Kind.This,
            "true" => Token.Kind.True,
            "false" => Token.Kind.False,
            "null" => Token.Kind.Null,
            "and" => Token.Kind.And,
            "or" => Token.Kind.Or,
            "in" => Token.Kind.In,
            "not" => Token.Kind.Not,
            _ => Token.Kind.Identifier
        };
    }

    /// <summary>
    ///     Main state machine step. Yields 0..N tokens for a single char.
    /// </summary>
    public IEnumerable<Token> Accept(char c, bool eof)
    {
        switch (_state)
        {
            case State.InRawText:
                if (eof)
                {
                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    if (t != null)
                        yield return t.Value;
                    else
                        yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (c == '{')
                {
                    // Potential start of splicer or tag; don't flush yet.
                    _state = State.AfterOpenBrace;
                    yield break;
                }

                if (c == '§')
                {
                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    _state = State.AfterEscapePrefix;
                    if (t != null)
                        yield return t.Value;
                    yield break;
                }

                _current.Append(c);
                yield break;

            case State.AfterOpenBrace:
                if (eof)
                {
                    // Lone '{' at end: treat as raw text.
                    _state = State.InRawText;
                    _current.Append('{');

                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    if (t != null)
                        yield return t.Value;
                    else
                        yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (c == '{')
                {
                    // Safe splicer start: flush prior text then emit '{{'
                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    if (t != null)
                        yield return t.Value;

                    _state = State.InSafeSplicer;
                    _codeMode = CodeMode.Safe;
                    yield return EmitFixed(Token.Kind.SafeSplicerStart, "{{");
                    yield break;
                }

                if (c == '%')
                {
                    // Spicy splicer start: flush prior text then emit '{%'
                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    if (t != null)
                        yield return t.Value;

                    _state = State.InSpicySplicer;
                    _codeMode = CodeMode.Spicy;
                    yield return EmitFixed(Token.Kind.SpicySplicerStart, "{%");
                    yield break;
                }

                if (IsIdentStart(c))
                {
                    // Tag open: {tagname ...}
                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    if (t != null)
                        yield return t.Value;

                    _state = State.InTagNameOpen;
                    _codeMode = CodeMode.Tag;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (c == '/')
                {
                    // Tag close: {/tagname}
                    var t = EmitIfNotEmpty(Token.Kind.Text);
                    if (t != null)
                        yield return t.Value;

                    _state = State.InTagNameClose;
                    _codeMode = CodeMode.Tag;
                    _current.Clear();
                    yield break;
                }

                // Not a splicer or tag: treat '{' + this char as raw text.
                _state = State.InRawText;
                _current.Append('{');
                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;

            case State.AfterEscapePrefix:
                if (eof)
                {
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                // Hex has priority: §ABCD; is hex, not a name.
                if (IsHexDigit(c))
                {
                    _state = State.InEscapeHex;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (char.IsLetter(c))
                {
                    _state = State.InEscapeName;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                // Not a valid escape. Treat '§' + this c as text.
                _state = State.InRawText;
                _current.Append('§');
                _current.Append(c);
                yield break;

            case State.InEscapeName:
                if (eof)
                {
                    var name = _current.ToString();
                    _current.Clear();
                    _state = State.InRawText;
                    yield return new Token { Type = Token.Kind.Text, Content = "§" + name };
                    yield break;
                }

                if (char.IsLetter(c))
                {
                    _current.Append(c);
                    yield break;
                }

                // End of escape name (no terminator char).
            {
                var name = _current.ToString();
                _current.Clear();
                _state = State.InRawText;
                yield return new Token { Type = Token.Kind.EscapeName, Content = name };

                // Reprocess current char in raw text.
                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;
            }

            case State.InEscapeHex:
                if (eof)
                {
                    var hex = _current.ToString();
                    _current.Clear();
                    _state = State.InRawText;
                    yield return new Token { Type = Token.Kind.Text, Content = "§" + hex };
                    yield break;
                }

                if (IsHexDigit(c))
                {
                    _current.Append(c);
                    yield break;
                }

                if (c == ';')
                {
                    var hex = _current.ToString();
                    _current.Clear();
                    _state = State.InRawText;
                    yield return new Token { Type = Token.Kind.EscapeHex, Content = hex };
                    yield break;
                }

                // Invalid hex escape, downgrade to literal text "§<digits><c>"
            {
                var invalidHex = _current.ToString();
                _current.Clear();
                _state = State.InRawText;
                _current.Append('§');
                _current.Append(invalidHex);
                _current.Append(c);
                yield break;
            }

            // ---- TAG SYNTAX ----

            case State.InTagNameOpen:
                if (eof)
                {
                    var name = _current.ToString();
                    _current.Clear();
                    _state = State.InRawText;
                    yield return new Token { Type = Token.Kind.TagOpen, Content = name };
                    yield break;
                }

                if (IsIdentPart(c))
                {
                    _current.Append(c);
                    yield break;
                }

                // Tag name finished on whitespace or '}' or any non-ident char.
            {
                var name = _current.ToString();
                _current.Clear();
                yield return new Token { Type = Token.Kind.TagOpen, Content = name };

                if (char.IsWhiteSpace(c))
                {
                    // Go into tag expression mode: {tag <expr>}
                    _state = State.InTagCode;
                    yield break;
                }

                if (c == '}')
                {
                    // {tag} with no expression.
                    _state = State.InRawText;
                    _codeMode = CodeMode.None;
                    yield break;
                }

                // Expression starting immediately after tag name: {tag(expr ...)}
                _state = State.InTagCode;
                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;
            }

            case State.InTagNameClose:
                if (eof)
                {
                    var name = _current.ToString();
                    _current.Clear();
                    _state = State.InRawText;
                    yield return new Token { Type = Token.Kind.TagClose, Content = name };
                    yield break;
                }

                if (IsIdentPart(c))
                {
                    _current.Append(c);
                    yield break;
                }

                if (c == '}')
                {
                    var name = _current.ToString();
                    _current.Clear();
                    _state = State.InRawText;
                    _codeMode = CodeMode.None;
                    yield return new Token { Type = Token.Kind.TagClose, Content = name };
                    yield break;
                }

                // Weird char in closing tag; just end tag and reprocess as raw/code.
            {
                var name = _current.ToString();
                _current.Clear();
                _state = State.InRawText;
                _codeMode = CodeMode.None;
                yield return new Token { Type = Token.Kind.TagClose, Content = name };
                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;
            }

            case State.InTagCode:
                if (eof)
                {
                    // Unterminated tag; close expression as-is.
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (char.IsWhiteSpace(c))
                    yield break;

                if (c == '}')
                {
                    // End of tag header expression.
                    _state = State.InRawText;
                    _codeMode = CodeMode.None;
                    yield break;
                }

                // Expression lexing is basically the same as InSafeSplicer/InSpicySplicer:

                if (IsIdentStart(c))
                {
                    _state = State.InIdentifier;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (char.IsDigit(c))
                {
                    _state = State.InNumber;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (c == '"' || c == '\'')
                {
                    _state = State.InString;
                    _stringQuote = c;
                    _current.Clear();
                    yield break;
                }

                switch (c)
                {
                    case '(':
                        yield return EmitFixed(Token.Kind.LParen, "(");
                        yield break;
                    case ')':
                        yield return EmitFixed(Token.Kind.RParen, ")");
                        yield break;
                    case ':':
                        yield return EmitFixed(Token.Kind.Colon, ":");
                        yield break;
                    case ',':
                        yield return EmitFixed(Token.Kind.Comma, ",");
                        yield break;
                    case '+':
                        yield return EmitFixed(Token.Kind.Plus, "+");
                        yield break;
                    case '-':
                        yield return EmitFixed(Token.Kind.Minus, "-");
                        yield break;
                    case '*':
                        yield return EmitFixed(Token.Kind.Star, "*");
                        yield break;
                    case '/':
                        yield return EmitFixed(Token.Kind.Slash, "/");
                        yield break;
                    case '%':
                        yield return EmitFixed(Token.Kind.Percent, "%");
                        yield break;
                }

                // Unknown char in tag expression — treat like identifier-ish start
                _state = State.InIdentifier;
                _current.Clear();
                _current.Append(c);
                yield break;

            // ---- EXISTING CODE MODES ----

            case State.InSafeSplicer:
                if (eof)
                {
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (char.IsWhiteSpace(c))
                    yield break;

                if (c == '}')
                {
                    _state = State.AfterSafeBrace;
                    yield break;
                }

                if (IsIdentStart(c))
                {
                    _state = State.InIdentifier;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (char.IsDigit(c))
                {
                    _state = State.InNumber;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (c == '"' || c == '\'')
                {
                    _state = State.InString;
                    _stringQuote = c;
                    _current.Clear();
                    yield break;
                }

                switch (c)
                {
                    case '(':
                        yield return EmitFixed(Token.Kind.LParen, "(");
                        yield break;
                    case ')':
                        yield return EmitFixed(Token.Kind.RParen, ")");
                        yield break;
                    case ':':
                        yield return EmitFixed(Token.Kind.Colon, ":");
                        yield break;
                    case ',':
                        yield return EmitFixed(Token.Kind.Comma, ",");
                        yield break;
                    case '+':
                        yield return EmitFixed(Token.Kind.Plus, "+");
                        yield break;
                    case '-':
                        yield return EmitFixed(Token.Kind.Minus, "-");
                        yield break;
                    case '*':
                        yield return EmitFixed(Token.Kind.Star, "*");
                        yield break;
                    case '/':
                        yield return EmitFixed(Token.Kind.Slash, "/");
                        yield break;
                    case '%':
                        yield return EmitFixed(Token.Kind.Percent, "%");
                        yield break;
                }

                _state = State.InIdentifier;
                _current.Clear();
                _current.Append(c);
                yield break;

            case State.AfterSafeBrace:
                if (eof)
                {
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (c == '}')
                {
                    _state = State.InRawText;
                    _codeMode = CodeMode.None;
                    yield return EmitFixed(Token.Kind.SafeSplicerEnd, "}}");
                    yield break;
                }

                _state = State.InSafeSplicer;
                yield return EmitFixed(Token.Kind.Identifier, "}");
                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;

            case State.InSpicySplicer:
                if (eof)
                {
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (char.IsWhiteSpace(c))
                    yield break;

                if (c == '%')
                {
                    _state = State.AfterSpicyPercent;
                    yield break;
                }

                if (IsIdentStart(c))
                {
                    _state = State.InIdentifier;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (char.IsDigit(c))
                {
                    _state = State.InNumber;
                    _current.Clear();
                    _current.Append(c);
                    yield break;
                }

                if (c == '"' || c == '\'')
                {
                    _state = State.InString;
                    _stringQuote = c;
                    _current.Clear();
                    yield break;
                }

                switch (c)
                {
                    case '(':
                        yield return EmitFixed(Token.Kind.LParen, "(");
                        yield break;
                    case ')':
                        yield return EmitFixed(Token.Kind.RParen, ")");
                        yield break;
                    case ':':
                        yield return EmitFixed(Token.Kind.Colon, ":");
                        yield break;
                    case ',':
                        yield return EmitFixed(Token.Kind.Comma, ",");
                        yield break;
                    case '+':
                        yield return EmitFixed(Token.Kind.Plus, "+");
                        yield break;
                    case '-':
                        yield return EmitFixed(Token.Kind.Minus, "-");
                        yield break;
                    case '*':
                        yield return EmitFixed(Token.Kind.Star, "*");
                        yield break;
                    case '/':
                        yield return EmitFixed(Token.Kind.Slash, "/");
                        yield break;
                    case '%':
                        yield return EmitFixed(Token.Kind.Percent, "%");
                        yield break;
                }

                _state = State.InIdentifier;
                _current.Clear();
                _current.Append(c);
                yield break;

            case State.AfterSpicyPercent:
                if (eof)
                {
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                if (c == '}')
                {
                    _state = State.InRawText;
                    _codeMode = CodeMode.None;
                    yield return EmitFixed(Token.Kind.SpicySplicerEnd, "%}");
                    yield break;
                }

                _state = State.InSpicySplicer;
                yield return EmitFixed(Token.Kind.Percent, "%");
                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;

            case State.InIdentifier:
                if (eof)
                {
                    var id = _current.ToString();
                    _current.Clear();
                    var kind = ClassifyIdentifier(id);

                    // Go back to whatever code mode we were in (safe, spicy, tag)
                    _state = _codeMode switch
                    {
                        CodeMode.Safe => State.InSafeSplicer,
                        CodeMode.Spicy => State.InSpicySplicer,
                        CodeMode.Tag => State.InTagCode,
                        _ => State.InRawText
                    };

                    yield return new Token { Type = kind, Content = id };
                    yield break;
                }

                if (IsIdentPart(c))
                {
                    _current.Append(c);
                    yield break;
                }

            {
                var id = _current.ToString();
                _current.Clear();
                var kind = ClassifyIdentifier(id);

                _state = _codeMode switch
                {
                    CodeMode.Safe => State.InSafeSplicer,
                    CodeMode.Spicy => State.InSpicySplicer,
                    CodeMode.Tag => State.InTagCode,
                    _ => State.InRawText
                };

                yield return new Token { Type = kind, Content = id };

                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;
            }

            case State.InNumber:
                if (eof)
                {
                    var num = _current.ToString();
                    _current.Clear();

                    _state = _codeMode switch
                    {
                        CodeMode.Safe => State.InSafeSplicer,
                        CodeMode.Spicy => State.InSpicySplicer,
                        CodeMode.Tag => State.InTagCode,
                        _ => State.InRawText
                    };

                    yield return new Token { Type = Token.Kind.Number, Content = num };
                    yield break;
                }

                if (char.IsDigit(c))
                {
                    _current.Append(c);
                    yield break;
                }

            {
                var num = _current.ToString();
                _current.Clear();

                _state = _codeMode switch
                {
                    CodeMode.Safe => State.InSafeSplicer,
                    CodeMode.Spicy => State.InSpicySplicer,
                    CodeMode.Tag => State.InTagCode,
                    _ => State.InRawText
                };

                yield return new Token { Type = Token.Kind.Number, Content = num };

                foreach (var t in Accept(c, eof))
                    yield return t;
                yield break;
            }

            case State.InString:
                if (eof)
                {
                    var s = _current.ToString();
                    _current.Clear();

                    _state = _codeMode switch
                    {
                        CodeMode.Safe => State.InSafeSplicer,
                        CodeMode.Spicy => State.InSpicySplicer,
                        CodeMode.Tag => State.InTagCode,
                        _ => State.InRawText
                    };

                    yield return new Token { Type = Token.Kind.String, Content = s };
                    yield break;
                }

                if (c == '\\')
                {
                    _state = State.InStringEscape;
                    yield break;
                }

                if (c == _stringQuote)
                {
                    var s = _current.ToString();
                    _current.Clear();

                    _state = _codeMode switch
                    {
                        CodeMode.Safe => State.InSafeSplicer,
                        CodeMode.Spicy => State.InSpicySplicer,
                        CodeMode.Tag => State.InTagCode,
                        _ => State.InRawText
                    };

                    yield return new Token { Type = Token.Kind.String, Content = s };
                    yield break;
                }

                _current.Append(c);
                yield break;

            case State.InStringEscape:
                if (eof)
                {
                    var s = _current.ToString();
                    _current.Clear();

                    _state = _codeMode switch
                    {
                        CodeMode.Safe => State.InSafeSplicer,
                        CodeMode.Spicy => State.InSpicySplicer,
                        CodeMode.Tag => State.InTagCode,
                        _ => State.InRawText
                    };

                    yield return new Token { Type = Token.Kind.String, Content = s };
                    yield break;
                }

                _current.Append(c);
                _state = State.InString;
                yield break;

            default:
                if (eof)
                {
                    yield return EmitFixed(Token.Kind.Eof);
                    yield break;
                }

                _state = State.InRawText;
                _current.Append(c);
                yield break;
        }
    }

    /// <summary>
    ///     Run the lexer over a full TextReader.
    /// </summary>
    public IEnumerable<Token> Tokenize(TextReader reader)
    {
        while (true)
        {
            var ch = reader.Read();
            var eof = ch == -1;
            var c = eof ? '\0' : (char)ch;

            foreach (var tok in Accept(c, eof))
            {
                yield return tok;
                if (tok.Type == Token.Kind.Eof)
                    yield break;
            }

            if (eof)
                yield break;
        }
    }

    /// <summary>
    ///     Convenience: tokenize a string.
    /// </summary>
    public static IEnumerable<Token> Tokenize(string input)
    {
        var lexer = new Lexer();
        using var reader = new StringReader(input);
        foreach (var tok in lexer.Tokenize(reader))
            yield return tok;
    }

    private enum State
    {
        InRawText,

        AfterOpenBrace, // Saw '{' in raw, waiting for second char
        AfterEscapePrefix, // Saw '§' in raw, decide name vs hex

        InSafeSplicer, // default code state for '{{ }}'
        InSpicySplicer, // default code state for '{% %}'

        InTagNameOpen, // "{tag..."
        InTagNameClose, // "{/tag..."
        InTagCode, // expression part of "{tag expr}"

        InIdentifier,
        InNumber,
        InString,
        InStringEscape,

        InEscapeName, // §newline
        InEscapeHex, // §ABCD;
        AfterSafeBrace, // saw '}' inside safe splicer, maybe '}}'
        AfterSpicyPercent // saw '%' inside spicy splicer, maybe '%}'
    }

    private enum CodeMode
    {
        None,
        Safe,
        Spicy,
        Tag
    }
}