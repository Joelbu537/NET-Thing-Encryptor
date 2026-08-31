using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NET_Thing_Encryptor;

namespace Nte.App.Converters;

public sealed class FileTypeIconConverter : IValueConverter
{
    private static readonly Lazy<IReadOnlyDictionary<FileType, IImage>> Icons = new(CreateIcons);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FileType type && Icons.Value.TryGetValue(type, out IImage? icon)
            ? icon
            : Icons.Value[FileType.other];

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;

    private static IReadOnlyDictionary<FileType, IImage> CreateIcons() =>
        new Dictionary<FileType, IImage>
        {
            [FileType.audio] = Load("imageres_audio_file.ico"),
            [FileType.image] = Load("imageres_image_file.ico"),
            [FileType.other] = Load("imageres_other_file.ico"),
            [FileType.video] = Load("imageres_video_file.ico"),
            [FileType.folder] = Load("imageres_folder_empty.ico"),
            [FileType.text] = Load("imageres_text_file.ico")
        };

    private static Bitmap Load(string fileName)
    {
        var uri = new Uri($"avares://Nte.App/Assets/Legacy/{fileName}");
        using Stream stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }
}
