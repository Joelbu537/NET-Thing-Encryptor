using System.Text;

namespace Nte.App.Services;

public sealed record DecodedTextDocument(
    string Text,
    Encoding Encoding,
    bool HasByteOrderMark)
{
    public string EncodingName =>
        $"{Encoding.WebName.ToUpperInvariant()}{(HasByteOrderMark ? " BOM" : string.Empty)}";
}

public static class TextDocumentCodec
{
    public static DecodedTextDocument Decode(ReadOnlySpan<byte> data)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (data.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            return DecodeWithPreamble(data, new UTF8Encoding(true, true), 3);
        if (data.StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }))
            return DecodeWithPreamble(data, new UTF32Encoding(false, true, true), 4);
        if (data.StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF }))
            return DecodeWithPreamble(data, new UTF32Encoding(true, true, true), 4);
        if (data.StartsWith(new byte[] { 0xFF, 0xFE }))
            return DecodeWithPreamble(data, new UnicodeEncoding(false, true, true), 2);
        if (data.StartsWith(new byte[] { 0xFE, 0xFF }))
            return DecodeWithPreamble(data, new UnicodeEncoding(true, true, true), 2);

        var utf8 = new UTF8Encoding(false, true);
        try
        {
            return new DecodedTextDocument(utf8.GetString(data), utf8, false);
        }
        catch (DecoderFallbackException)
        {
            Encoding windows1252 = Encoding.GetEncoding(1252);
            return new DecodedTextDocument(windows1252.GetString(data), windows1252, false);
        }
    }

    public static byte[] Encode(DecodedTextDocument document, string text)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(text);
        byte[] body = document.Encoding.GetBytes(text);
        if (!document.HasByteOrderMark)
            return body;

        byte[] preamble = document.Encoding.GetPreamble();
        if (preamble.Length == 0)
            return body;
        byte[] result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    private static DecodedTextDocument DecodeWithPreamble(
        ReadOnlySpan<byte> data,
        Encoding encoding,
        int preambleLength) => new(
            encoding.GetString(data[preambleLength..]),
            encoding,
            true);
}
