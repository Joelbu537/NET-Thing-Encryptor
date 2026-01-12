using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor
{
    public class FileCategorie(string categoryName, List<string> extensions)
    {
        public readonly string CategoryName = categoryName;
        public List<string> Extensions = extensions;
    }

    public static class FileCategories
    {
        public static List<FileCategorie> Categories =
        [
            new FileCategorie("Image", new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".svg", ".webp", ".avif" }),
            new FileCategorie("Video", new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv" }),
            new FileCategorie("Text", new List<string> { ".txt", ".cs", ".java", ".py", ".js", ".html", ".css", ".cpp", ".rb" }),
            new FileCategorie("Audio", new List<string> { ".mp3", ".wav", ".aac", ".flac", ".ogg", ".wma" }),
        ];
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

    /*  This is supposed to be the new enum for when the FileCategories class is fully implemented

    public enum FileType
    {
        other,
        folder,
        image,
        video,
        text,
        audio
    }
    */
}
