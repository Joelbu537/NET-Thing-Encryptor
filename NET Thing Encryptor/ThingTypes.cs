using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor
{
    public abstract class ThingObject
    {
        public string? Name;
        public ulong ID;
        public ulong ParentID;
    }
    public class ThingFolder : ThingObject
    {
        public List<ulong> Content;
        public ThingFolder(string name)
        {
            Name = name;
            ID = ThingData.GenerateID();

            Content = new List<ulong>();
        }
    }
    public class ThingFile : ThingObject
    {
        public byte[] Content {
            get { return Content; }
            set
            {
                if(value == null || value.Length == 0)
                {
                    throw new ArgumentException("Content cannot be null or empty.", nameof(value));
                }
                Content = value;
                MD5Hash = ThingData.ComputeMD5Hash(value);
            }
        }
        public string MD5Hash { get; private set; }
        public FileType Type;
        public FileExtension Extension;
        public ThingFile(string name, byte[] content)
        {
            Name = name;
            ID = ThingData.GenerateID();
            ParentID = 0;
            MD5Hash = string.Empty;
            Content = content;
        }
    }
    public class ThingRoot : ThingObject
    {
        public byte[] Salt;
        public string? SaveLocation;
        public string ContentEncrypted; // magic-value + List<ThingObject>
        public List<ulong>? Content; // List of ThingObjects (Folders and Files)
        public ThingRoot(byte[] salt)
        {
            Name = "Root";
            ID = 0;
            ParentID = 0;

            Salt = salt;
            SaveLocation = null;
            ContentEncrypted = string.Empty;
            Content = new List<ulong>();
        }
    }
    public enum FileType
    {
        video,
        audio,
        image,
        text,
        other
    }
    public enum  FileExtension
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
