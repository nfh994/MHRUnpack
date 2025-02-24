using MHRUnpack.Attributes;

namespace MHRUnpack.FileStructs
{
    public class PakHeader : FileStruct
    {
        [Verification<uint>(0x414B504B)]
        public UInt32 dwMagic { get; set; } //0x414B504B (KPKA)

        [Verification<byte>(2)]
        [Verification<byte>(4)]
        public byte bMajorVersion { get; set; } // 2 (Kitchen Demo PS4), 4
        [Verification<byte>(0)]
        [Verification<byte>(1)]
        public byte bMinorVersion { get; set; } // 0
        [Verification<Int16>(0)]
        [Verification<Int16>(8)]
        public Int16 wFeature { get; set; } // 0, 8 (Encrypted -> PKC)
        public Int32 dwTotalFiles { get; set; }
        public UInt32 dwFingerprint { get; set; }

        public int dwEntrySize = 48;
        public int EntrySize;
        public override void Init()
        {
            if (bMajorVersion == 2)
            {
                dwEntrySize = 24;
            }
            EntrySize = dwTotalFiles * dwEntrySize;
        }
    }
}
