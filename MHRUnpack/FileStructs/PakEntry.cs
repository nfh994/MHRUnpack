using MHRUnpack.Utils;
using System.Reflection;

namespace MHRUnpack.FileStructs
{
    public class PakEntry : FileStruct
    {
        public UInt32 dwHashNameLower { get; set; }
        public UInt32 dwHashNameUpper { get; set; }
        public Int64 dwOffset { get; set; }
        public Int64 dwCompressedSize { get; set; }
        public Int64 dwDecompressedSize { get; set; }
        public Int64 dwAttributes { get; set; }
        public UInt64 dwChecksum { get; set; }

        public Compression wCompressionType;
        public Encryption wEncryptionType;
        public UInt64 Hash;
        public string From;
        public string Path;
        public override void Init()
        {
            wCompressionType = (Compression)(dwAttributes & 0xF);
            wEncryptionType = (Encryption)((dwAttributes & 0x00FF0000) >> 16);
            Hash = (UInt64)dwHashNameUpper << 32 | dwHashNameLower;
        }
    }
    public class PakEntryV2 : PakEntry
    {
        public override PropertyInfo[] GetProperties()
        {
            var properties = base.GetProperties();
            PropertyInfo[] sort = [
                properties.First(properties => properties.Name == "dwOffset"),
                properties.First(properties => properties.Name == "dwDecompressedSize"),
                properties.First(properties => properties.Name == "dwHashNameLower"),
                properties.First(properties => properties.Name == "dwHashNameUpper")
            ];
            return sort;
        }
    }
    public class PakEntryV4 : PakEntry
    {

    }
}
