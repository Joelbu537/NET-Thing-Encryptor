using NET_Thing_Encryptor;
using Nte.App.Services;

namespace Nte.App.ViewModels;

public sealed class VaultItemViewModel(VaultItem item)
{
    public ulong Id { get; } = item.Id;
    public string Name { get; } = item.Name;
    public FileType Type { get; } = item.Type;
    public long Size { get; } = item.Size;
    public string Extension { get; } = item.Extension;
    public DateOnly CreatedAt { get; } = item.CreatedAt;
    public string Location { get; } = item.Location;
    public bool HasLocation { get; } = !string.IsNullOrWhiteSpace(item.Location);
    public bool IsFolder { get; } = item.IsFolder;
    public string KindText { get; } = GetKindText(item.Type);
    public string SizeText { get; } = item.IsFolder ? "Ordner" : item.Size.Sizeify();
    public string Symbol { get; } = item.IsFolder ? "▰" : "▤";

    public string SuggestedFileName => string.IsNullOrWhiteSpace(Extension)
        ? Name
        : $"{Name}.{Extension.TrimStart('.')}";

    private static string GetKindText(FileType type) => type switch
    {
        FileType.folder => "Ordner",
        FileType.image => "Bild",
        FileType.video => "Video",
        FileType.audio => "Audio",
        FileType.text => "Text",
        _ => "Datei"
    };
}
