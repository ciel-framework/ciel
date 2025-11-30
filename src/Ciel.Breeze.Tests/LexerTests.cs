namespace Ciel.Breeze.Tests;

[TestFixture]
public class LexerTests
{
    private static List<Token> Lex(string input)
    {
        return Lexer.Tokenize(input).ToList();
    }

    [Test]
    public void PlainText_ProducesSingleTextToken_NoEof()
    {
        var tokens = Lex("hello world");

        Assert.That(tokens, Has.Count.EqualTo(1));
        Assert.That(tokens[0].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[0].Content, Is.EqualTo("hello world"));
    }

    [Test]
    public void SafeSplicer_SimpleIdentifier()
    {
        var tokens = Lex("{{ foo }}");

        Assert.That(tokens.Select(t => t.Type).ToArray(),
            Is.EqualTo(new[]
            {
                Token.Kind.SafeSplicerStart,
                Token.Kind.Identifier,
                Token.Kind.SafeSplicerEnd,
                Token.Kind.Eof
            }));

        Assert.That(tokens[1].Content, Is.EqualTo("foo"));
    }

    [Test]
    public void SpicySplicer_IfTrue_Block()
    {
        var tokens = Lex("{% if true %}x{% end %}");

        var kinds = tokens.Select(t => t.Type).ToArray();

        Assert.That(
            kinds,
            Is.EqualTo(new[]
            {
                Token.Kind.SpicySplicerStart, // {%
                Token.Kind.Identifier, // if
                Token.Kind.True, // true
                Token.Kind.SpicySplicerEnd, // %}
                Token.Kind.Text, // x
                Token.Kind.SpicySplicerStart, // {%
                Token.Kind.Identifier, // end
                Token.Kind.SpicySplicerEnd, // %}
                Token.Kind.Eof
            }).AsCollection);

        Assert.That(tokens[1].Content, Is.EqualTo("if"));
        Assert.That(tokens[2].Content, Is.EqualTo("true"));
        Assert.That(tokens[4].Content, Is.EqualTo("x"));
        Assert.That(tokens[6].Content, Is.EqualTo("end"));
    }

    [Test]
    public void EscapeName_InRawText()
    {
        var tokens = Lex("Hello §newline world");

        // Expect: "Hello " + EscapeName("newline") + " world"
        Assert.That(tokens, Has.Count.EqualTo(3));

        Assert.That(tokens[0].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[0].Content, Is.EqualTo("Hello "));

        Assert.That(tokens[1].Type, Is.EqualTo(Token.Kind.EscapeName));
        Assert.That(tokens[1].Content, Is.EqualTo("newline"));

        Assert.That(tokens[2].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[2].Content, Is.EqualTo(" world"));
    }

    [Test]
    public void EscapeHex_TerminatedBySemicolon()
    {
        var tokens = Lex("A §ABCD; B");

        // Expect: "A " + EscapeHex("ABCD") + " B"
        Assert.That(tokens, Has.Count.EqualTo(3));

        Assert.That(tokens[0].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[0].Content, Is.EqualTo("A "));

        Assert.That(tokens[1].Type, Is.EqualTo(Token.Kind.EscapeHex));
        Assert.That(tokens[1].Content, Is.EqualTo("ABCD"));

        Assert.That(tokens[2].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[2].Content, Is.EqualTo(" B"));
    }

    [Test]
    public void EscapeHex_Invalid_DegeneratesToLiteral()
    {
        var tokens = Lex("§12G");

        Assert.That(tokens, Has.Count.EqualTo(1));
        Assert.That(tokens[0].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[0].Content, Is.EqualTo("§12G"));
    }

    [Test]
    public void NumberLiteral_InSafeSplicer()
    {
        var tokens = Lex("{{ 123 }}");

        var kinds = tokens.Select(t => t.Type).ToArray();

        Assert.That(
            kinds,
            Is.EqualTo(new[]
            {
                Token.Kind.SafeSplicerStart,
                Token.Kind.Number,
                Token.Kind.SafeSplicerEnd,
                Token.Kind.Eof
            }).AsCollection);

        Assert.That(tokens[1].Content, Is.EqualTo("123"));
    }

    [Test]
    public void StringLiteral_WithSimpleEscape()
    {
        var tokens = Lex("{{ \"a\\\"b\" }}");

        var kinds = tokens.Select(t => t.Type).ToArray();

        Assert.That(
            kinds,
            Is.EqualTo(new[]
            {
                Token.Kind.SafeSplicerStart,
                Token.Kind.String,
                Token.Kind.SafeSplicerEnd,
                Token.Kind.Eof
            }).AsCollection);

        // Our string escape logic: we remove the backslash and keep the escaped char.
        Assert.That(tokens[1].Content, Is.EqualTo("a\"b"));
    }

    [Test]
    public void IdentifierKeywords_AreClassified()
    {
        var tokens = Lex("{{ this and or in not true false null foo }}");

        var kinds = tokens
            .Where(t => t.Type != Token.Kind.SafeSplicerStart &&
                        t.Type != Token.Kind.SafeSplicerEnd &&
                        t.Type != Token.Kind.Eof)
            .Select(t => t.Type)
            .ToArray();

        Assert.That(
            kinds,
            Is.EqualTo(new[]
            {
                Token.Kind.This,
                Token.Kind.And,
                Token.Kind.Or,
                Token.Kind.In,
                Token.Kind.Not,
                Token.Kind.True,
                Token.Kind.False,
                Token.Kind.Null,
                Token.Kind.Identifier
            }).AsCollection);

        Assert.That(tokens.Last(t => t.Type == Token.Kind.Identifier).Content,
            Is.EqualTo("foo"));
    }

    [Test]
    public void LoneBraceInRawText_IsTagOpen()
    {
        var tokens = Lex("a{b");

        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[0].Content, Is.EqualTo("a"));

        Assert.That(tokens[1].Type, Is.EqualTo(Token.Kind.TagOpen));
        Assert.That(tokens[1].Content, Is.EqualTo("b"));
    }

    [Test]
    public void LoneSimflouzInRawText_IsNotEscaped()
    {
        var tokens = Lex("a§b");

        Console.WriteLine(string.Join(",", tokens));

        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[0].Content, Is.EqualTo("a"));

        Assert.That(tokens[1].Type, Is.EqualTo(Token.Kind.Text));
        Assert.That(tokens[1].Content, Is.EqualTo("§b"));
    }

    [Test]
    public void MixedRawAndCode()
    {
        var tokens = Lex("Hello {{ name }}!");

        var kinds = tokens.Select(t => t.Type).ToArray();
        var contents = tokens.Select(t => t.Content).ToArray();

        Assert.That(
            kinds,
            Is.EqualTo(new[]
            {
                Token.Kind.Text, // "Hello "
                Token.Kind.SafeSplicerStart, // "{{"
                Token.Kind.Identifier, // name
                Token.Kind.SafeSplicerEnd, // "}}"
                Token.Kind.Text // "!"
                // No Eof because we end in raw text with content
            }).AsCollection);

        Assert.That(contents[0], Is.EqualTo("Hello "));
        Assert.That(contents[2], Is.EqualTo("name"));
        Assert.That(contents[4], Is.EqualTo("!"));
    }

    [Test]
    public void TagOpen_WithoutExpression()
    {
        var tokens = Lex("{if}");

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
        {
            Token.Kind.TagOpen,
            Token.Kind.Eof
        }));

        Assert.That(tokens[0].Content, Is.EqualTo("if"));
    }

    [Test]
    public void TagOpen_WithExpression_AndCloseTag()
    {
        var tokens = Lex("Before {if x} middle {/if} after");

        var kinds = tokens.Select(t => t.Type).ToArray();
        var contents = tokens.Select(t => t.Content).ToArray();

        Assert.That(
            kinds,
            Is.EqualTo(new[]
            {
                Token.Kind.Text, // "Before "
                Token.Kind.TagOpen, // "if"
                Token.Kind.Identifier, // "x"
                Token.Kind.Text, // " middle "
                Token.Kind.TagClose, // "if"
                Token.Kind.Text // " after"
                // No Eof token here because we end in raw text with content
            }).AsCollection);

        Assert.That(contents[0], Is.EqualTo("Before "));
        Assert.That(contents[1], Is.EqualTo("if"));
        Assert.That(contents[2], Is.EqualTo("x"));
        Assert.That(contents[3], Is.EqualTo(" middle "));
        Assert.That(contents[4], Is.EqualTo("if"));
        Assert.That(contents[5], Is.EqualTo(" after"));
    }
}