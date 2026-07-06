using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor
{
    public class FileCategorie(FileType type, List<string> extensions)
    {
        public FileType Type { get; } = type;
        public List<string> Extensions { get; } = extensions;
    }

    public static class FileCategories
    {
        public static List<FileCategorie> Categories =
        [
            new FileCategorie(FileType.image, new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".svg", ".webp", ".avif" }),
            new FileCategorie(FileType.video, new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv" }),
            new FileCategorie(FileType.text, new List<string>
            {
                ".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".config",
                ".yaml", ".yml", ".cs", ".java", ".py", ".js", ".ts", ".jsx", ".tsx",
                ".html", ".css", ".scss", ".cpp", ".c", ".h", ".rb", ".sql", ".sh",
                ".ps1", ".bat", ".cmd"
            }),
            new FileCategorie(FileType.audio, new List<string>
            {
                ".mp3", ".wav", ".aac", ".flac", ".ogg", ".wma", ".m4a",
                ".opus", ".aiff", ".aif", ".alac", ".mka", ".ac3", ".amr"
            }),
        ];

        public static FileType GetFileType(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return FileType.other;

            if (Directory.Exists(filePath))
                return FileType.folder;

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(ext))
                return FileType.other;

            ext = ext.ToLowerInvariant();
            foreach (var category in Categories)
            {
                if (category.Extensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                    return category.Type;
            }

            return FileType.other;
        }
    }
    public enum FileType
    {
        other,
        audio,
        image,
        text,
        video,
        folder
    }
}
