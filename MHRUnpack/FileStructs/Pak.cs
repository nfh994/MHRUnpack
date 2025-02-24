using MHRUnpack.Utils;
using System.IO;

namespace MHRUnpack.FileStructs
{
    public class Pak : FileStruct
    {
        public string PakPath;
        public PakHeader Header { get; set; }
        public Pak(string path)
        {
            PakPath = path;
        }
        public override void Dispose()
        {
            base.Dispose();
            Header?.Dispose();
        }
        public bool ReadEntry(out byte[] lpTable)
        {
            lpTable = null;
            Reader = new(File.OpenRead(PakPath));
            Header = new PakHeader();
            if (!Header.Read(Reader))
            {
                return false;
            }
            lpTable = Reader.ReadBytes(Header.EntrySize);
            //解密
            if (Header.wFeature == 8)
            {
                var lpEncryptedKey = Reader.ReadBytes(128);
                lpTable = PakCipher.iDecryptData(lpTable, lpEncryptedKey);
            }

            return true;
        }

    }
}
