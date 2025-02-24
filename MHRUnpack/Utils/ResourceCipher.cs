using System.IO;
using System.Numerics;
using System.Text;

namespace MHRUnpack.Utils
{
    class ResourceCipher
    {
        private static readonly Byte[] lpModulus = {
            0x13, 0xD7, 0x9C, 0x89, 0x88, 0x91, 0x48, 0x10, 0xD7, 0xAA, 0x78, 0xAE, 0xF8, 0x59, 0xDF, 0x7D,
            0x3C, 0x43, 0xA0, 0xD0, 0xBB, 0x36, 0x77, 0xB5, 0xF0, 0x5C, 0x02, 0xAF, 0x65, 0xD8, 0x77, 0x03,
            0x00
        };

        private static readonly Byte[] lpExponent = {
            0xC0, 0xC2, 0x77, 0x1F, 0x5B, 0x34, 0x6A, 0x01, 0xC7, 0xD4, 0xD7, 0x85, 0x2E, 0x42, 0x2B, 0x3B,
            0x16, 0x3A, 0x17, 0x13, 0x16, 0xEA, 0x83, 0x30, 0x30, 0xDF, 0x3F, 0xF4, 0x25, 0x93, 0x20, 0x01,
            0x00
        };

        public static Byte[] iDecryptResource(Byte[] lpBuffer)
        {
            using (var TMemoryReader = new MemoryStream(lpBuffer))
            {
                Int32 dwOffset = 0;
                Int32 dwBlockCount = (lpBuffer.Length - 8) / 128;
                Int64 dwDecryptedSize = TMemoryReader.ReadInt64();

                var lpResult = new Byte[dwDecryptedSize + 1];

                for (Int32 i = 0; i < dwBlockCount; i++, dwOffset += 8)
                {
                    BigInteger m_Key = new BigInteger(TMemoryReader.ReadBytes(64));
                    BigInteger m_Data = new BigInteger(TMemoryReader.ReadBytes(64));

                    BigInteger m_Modulus = new BigInteger(lpModulus);
                    BigInteger m_Exponent = new BigInteger(lpExponent);

                    BigInteger m_Mod = BigInteger.ModPow(m_Key, m_Exponent, m_Modulus);
                    BigInteger m_Result = BigInteger.Divide(m_Data, m_Mod);

                    var lpDecryptedBlock = m_Result.ToByteArray();

                    Array.Copy(lpDecryptedBlock, 0, lpResult, dwOffset, lpDecryptedBlock.Length);
                }

                TMemoryReader.Dispose();

                return lpResult;
            }
        }
    }
    public static class Helpers
    {
        public static byte[] ReadBytes(this Stream stream, int count)
        {
            var result = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int bytesRead = stream.Read(result, offset, count - offset);
                if (bytesRead <= 0)
                    throw new IOException();
                offset += bytesRead;
            }
            return result;
        }

        public static byte[] ReadBytes(this Stream stream)
        {
            return ReadBytes(stream, (int)stream.Length);
        }

        public static Int16 ReadInt16(this Stream stream)
        {
            return BitConverter.ToInt16(stream.ReadBytes(2), 0);
        }

        public static Int32 ReadInt32(this Stream stream)
        {
            return BitConverter.ToInt32(stream.ReadBytes(4), 0);
        }

        public static Int64 ReadInt64(this Stream stream)
        {
            return BitConverter.ToInt64(stream.ReadBytes(8), 0);
        }

        public static UInt16 ReadUInt16(this Stream stream)
        {
            return BitConverter.ToUInt16(stream.ReadBytes(2), 0);
        }

        public static UInt32 ReadUInt32(this Stream stream)
        {
            return BitConverter.ToUInt32(stream.ReadBytes(4), 0);
        }

        public static UInt64 ReadUInt64(this Stream stream)
        {
            return BitConverter.ToUInt64(stream.ReadBytes(8), 0);
        }

        public static Single ReadSingle(this Stream stream)
        {
            return BitConverter.ToSingle(stream.ReadBytes(4), 0);
        }

        public static string ReadStringUnicodeLength(this Stream stream, Int32 length)
        {
            var result = stream.ReadBytes(length * 2);
            return Encoding.Unicode.GetString(result);
        }

        public static string ReadStringLength(this Stream stream)
        {
            var length = stream.ReadInt32();
            var result = stream.ReadBytes(length);
            return Encoding.ASCII.GetString(result);
        }

        public static string ReadString(this Stream stream, int length, Encoding encoding = null, bool trim = true)
        {
            encoding = encoding ?? Encoding.ASCII;
            var result = encoding.GetString(stream.ReadBytes(length));
            return trim ? result.Trim() : result;
        }

        public static string ReadString(this Stream stream, Encoding encoding = null, bool trim = true)
        {
            encoding = encoding ?? Encoding.ASCII;

            int count = 0;
            int b;
            var data = new List<byte>();
            while ((b = stream.ReadByte()) > 0)
            {
                data.Add((byte)b);
                count++;
            }
            if (b < 0)
                throw new IOException();

            var result = encoding.GetString(data.ToArray(), 0, count);
            return trim ? result.Trim() : result;
        }

        public static string ReadStringByOffset(this Stream stream, uint offset, Encoding encoding = null, bool trim = true)
        {
            stream.Position = offset;
            return ReadString(stream, encoding, trim);
        }

        public static string[] ReadStringList(this Stream stream, Encoding encoding = null, bool trim = true)
        {
            var result = new List<string>();
            while (stream.Position < stream.Length)
                result.Add(ReadString(stream, encoding, trim));
            return result.ToArray();
        }

        public static void CopyTo(this Stream source, Stream target)
        {
            const int bufferSize = 32768;

            if (source == null)
                throw new ArgumentNullException("source");
            if (target == null)
                throw new ArgumentNullException("target");

            var buffer = new byte[bufferSize];
            int read;
            int count = 0;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                target.Write(buffer, 0, read);
                count += read;
            }
        }
    }
}
