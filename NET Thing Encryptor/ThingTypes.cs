using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ThingFolder), "folder")]
    [JsonDerivedType(typeof(ThingFile), "file")]
    public abstract class ThingObject
    {
        public string? Name { get; set; }
        public ulong ID { get; set; }
        public ulong ParentID { get; set; }
    }
    public class ThingFolder : ThingObject
    {
        public List<ThingObjectLink> Content { get; set; }
        public ThingFolder(string name)
        {
            Name = name;
            ID = ThingData.GenerateID();
            ParentID = 0;

            Content = new List<ThingObjectLink>();

            Debug.WriteLine($"Instantiating ThingFolder {Name}");
        }
    }
    public class ThingFile : ThingObject
    {
        private byte[]? _content;
        public byte[]? Content {
            get { return _content; }
            set
            {
                if(value == null || value.Length == 0)
                {
                    throw new ArgumentException("Content cannot be null or empty.", nameof(value));
                }
                _content = value;
                MD5Hash = ThingData.ComputeMD5Hash(value);
            }
        }
        public string MD5Hash { get; private set; }
        public FileType Type { get; set; }
        public FileExtension Extension { get; set; }
        public ThingFile(string name, byte[] content)
        {
            Name = name;
            ID = ThingData.GenerateID();
            ParentID = 0;
            MD5Hash = string.Empty;
            Content = content;

            Debug.WriteLine($"Instantiating ThingFile {Name}");
        }
    }
    public class ThingRoot : ThingObject, ICloneable
    {
        public byte[] Salt { get; set; }
        private string? _saveLocation;
        public string SaveLocation 
        { 
            get 
            { 
                if(_saveLocation == null)
                {
                    return Path.Combine(Directory.GetCurrentDirectory(), "Data");
                }
                else
                {
                    return _saveLocation;
                }
            } 
            set 
            { 
                _saveLocation = value; 
            } 
        }
        public string ContentEncrypted { get; set; }
        public List<ThingObjectLink>? Content { get; set; }

        public ThingRoot()
        {
            Name = "Root";
            ID = 0;
            ParentID = 0;

            Salt = new byte[32];
            SaveLocation = null;
            ContentEncrypted = string.Empty;
            Content = new();
        }
        public object Clone()
        {
            var clone = (ThingRoot)this.MemberwiseClone();

            clone.Salt = (byte[])this.Salt.Clone();
            clone.Content = this.Content != null ? new List<ThingObjectLink>(this.Content) : null;

            return clone;
        }
    }
    public class ThingObjectLink
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
        public FileType Type { get; set; }
        public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public long Size { get; set; } = 0;
        public byte[]? PreviewContent { get; set; }
        public ThingObjectLink(ulong id, string name, FileType type, long size, byte[]? previewContent = null)
        {
            ID = id;
            Name = name;
            Type = type;
            Size = size;
            PreviewContent = previewContent;
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
    public enum FileExtension
    {
        //Videos
        mp4,
        mov,
        avi,

        //Audio
        mp3,
        wav,
        ogg,

        //Images
        png,
        jpeg,
        gif,

        //Text
        txt,
        json,
        xml,

        //Other
        other
    }
}
