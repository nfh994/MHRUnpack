using MHRUnpack.Attributes;
using MHRUnpack.Exceptions;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace MHRUnpack.FileStructs
{
    public class FileStruct : IDisposable
    {
        public BinaryReader Reader;
        public BinaryWriter Writer;

        public virtual void Dispose()
        {
            Reader?.Dispose();
            Writer?.Dispose();
        }
        public void CheckAttribute<T>(PropertyInfo property, T value)
        {
            bool verification = false;
            foreach (var attribute in property.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(VerificationAttribute<T>))
                {
                    verification = true;
                    if (attribute.ConstructorArguments[0].Value.Equals(value))
                    {
                        return;
                    }
                }
            }
            if (verification)
            {
                throw new VerificationException();
            }
        }
        public long CanReadCount()
        {
            if (Reader == null)
            {
                return 0;
            }
            return Reader.BaseStream.Length - Reader.BaseStream.Position;
        }
        public bool CanRead(object value)
        {
            var count = CanReadCount();
            return count >= Marshal.SizeOf(value);
        }
        public virtual PropertyInfo[] GetProperties()
        {
            var type = GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties;
        }
        public bool Read(string path)
        {
            return Read(File.OpenRead(path));
        }
        public bool Read(FileStream stream)
        {
            return Read(new BinaryReader(stream));
        }
        public virtual bool Read(BinaryReader reader)
        {
            Reader = reader;
            var properties = GetProperties();
            foreach (var property in properties)
            {
                var property_type = property.PropertyType;
                try
                {
                    if (property_type.IsPrimitive)
                    {
                        if (property_type == typeof(char))
                        {
                            var value = Reader.ReadChar();
                            property.SetValue(this, value);
                            CheckAttribute<char>(property, value);
                        }
                        else if (property_type == typeof(byte))
                        {
                            var value = Reader.ReadByte();
                            property.SetValue(this, value);
                            CheckAttribute<byte>(property, value);
                        }
                        else if (property_type == typeof(short))
                        {
                            var value = Reader.ReadInt16();
                            property.SetValue(this, value);
                            CheckAttribute<short>(property, value);
                        }
                        else if (property_type == typeof(ushort))
                        {
                            var value = Reader.ReadUInt16();
                            property.SetValue(this, value);
                            CheckAttribute<ushort>(property, value);
                        }
                        else if (property_type == typeof(int))
                        {
                            var value = Reader.ReadInt32();
                            property.SetValue(this, value);
                            CheckAttribute<int>(property, value);
                        }
                        else if (property_type == typeof(uint))
                        {
                            var value = Reader.ReadUInt32();
                            property.SetValue(this, value);
                            CheckAttribute<uint>(property, value);
                        }
                        else if (property_type == typeof(long))
                        {
                            var value = Reader.ReadInt64();
                            property.SetValue(this, value);
                            CheckAttribute<long>(property, value);
                        }
                        else if (property_type == typeof(ulong))
                        {
                            var value = Reader.ReadUInt64();
                            property.SetValue(this, value);
                            CheckAttribute<ulong>(property, value);
                        }
                        else if (property_type == typeof(float))
                        {
                            var value = Reader.ReadSingle();
                            property.SetValue(this, value);
                            CheckAttribute<float>(property, value);
                        }
                        else if (property_type == typeof(double))
                        {
                            var value = Reader.ReadDouble();
                            property.SetValue(this, value);
                            CheckAttribute<double>(property, value);
                        }
                        else if (property_type == typeof(bool))
                        {
                            var value = Reader.ReadBoolean();
                            property.SetValue(this, value);
                            CheckAttribute<bool>(property, value);
                        }
                        else if (property_type == typeof(string))
                        {
                            var value = Reader.ReadString();
                            property.SetValue(this, value);
                            CheckAttribute<string>(property, value);
                        }
                    }
                }
                catch (VerificationException)
                {
                    MessageBox.Show("数据校验错误");
                    return false;
                }
                catch (EndOfStreamException)
                {
                    MessageBox.Show("读取文件错误");
                    return false;
                }
                catch (Exception)
                {

                    throw;
                }
            }
            Init();
            return true;
        }
        public virtual void Init()
        {

        }
    }
}
