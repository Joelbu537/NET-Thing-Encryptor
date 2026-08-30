using System.Text;
using Nte.App.Services;

namespace Nte.App.Tests;

public sealed class TextDocumentCodecTests
{
    [Fact]
    public void Utf8Bom_RoundTripPreservesTextAndPreamble()
    {
        byte[] source = [0xEF, 0xBB, 0xBF, .. "Grüße"u8.ToArray()];

        DecodedTextDocument decoded = TextDocumentCodec.Decode(source);
        byte[] encoded = TextDocumentCodec.Encode(decoded, decoded.Text + "!");

        Assert.Equal("Grüße", decoded.Text);
        Assert.True(decoded.HasByteOrderMark);
        Assert.Equal("UTF-8 BOM", decoded.EncodingName);
        Assert.True(encoded.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Equal("Grüße!", TextDocumentCodec.Decode(encoded).Text);
    }

    [Fact]
    public void Utf16BigEndian_RoundTripPreservesEncoding()
    {
        var encoding = new UnicodeEncoding(true, true, true);
        byte[] body = encoding.GetBytes("Text Ω");
        byte[] source = [.. encoding.GetPreamble(), .. body];

        DecodedTextDocument decoded = TextDocumentCodec.Decode(source);
        byte[] encoded = TextDocumentCodec.Encode(decoded, decoded.Text);

        Assert.Equal("Text Ω", decoded.Text);
        Assert.Equal(source, encoded);
    }

    [Fact]
    public void InvalidUtf8_UsesWindows1252AndWritesSameEncoding()
    {
        byte[] source = [0x80, 0x20, 0xE4];

        DecodedTextDocument decoded = TextDocumentCodec.Decode(source);
        byte[] encoded = TextDocumentCodec.Encode(decoded, decoded.Text);

        Assert.Equal("€ ä", decoded.Text);
        Assert.Equal("WINDOWS-1252", decoded.EncodingName);
        Assert.Equal(source, encoded);
    }
}
