using System.IO;
using System.IO.Compression;
using Zstandard.Net;

namespace MHRUnpack.Utils
{
    [Flags]
    public enum Compression : Int32
    {
        NONE = 0,
        DEFLATE = 1,
        ZSTD = 2,
    }

    public enum Encryption : Int32
    {
        None = 0,
        Type_1 = 0x1, // pkc_key::c1n & pkc_key::c1d
        Type_2 = 0x2, // pkc_key::c2n & pkc_key::c2d
        Type_3 = 0x3, // pkc_key::c3n & pkc_key::c3d
        Type_4 = 0x4, // pkc_key::c4n & pkc_key::c4d
        Type_Invalid = 0x5,
    }
    class CompressionUtil
    {
        public static Byte[] ZSTDDecompress(Byte[] lpSrcBuffer)
        {
            Byte[] lpDstBuffer;
            using (MemoryStream TSrcStream = new MemoryStream(lpSrcBuffer))
            {
                using (var TZstandardStream = new ZstandardStream(TSrcStream, CompressionMode.Decompress))
                using (var TDstStream = new MemoryStream())
                {
                    TZstandardStream.CopyTo(TDstStream);
                    lpDstBuffer = TDstStream.ToArray();
                }
            }
            return lpDstBuffer;
        }

        public static Byte[] iDecompress(Byte[] lpBuffer)
        {
            using var TOutMemoryStream = new MemoryStream();
            using (MemoryStream TMemoryStream = new MemoryStream(lpBuffer))
            {
                using (DeflateStream TDeflateStream = new DeflateStream(TMemoryStream, CompressionMode.Decompress, false))
                {
                    TDeflateStream.CopyTo(TOutMemoryStream);
                    TDeflateStream.Dispose();
                }
                TMemoryStream.Dispose();
            }

            return TOutMemoryStream.ToArray();
        }
    }
}
