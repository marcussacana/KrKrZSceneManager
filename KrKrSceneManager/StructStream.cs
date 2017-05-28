using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

/// <summary>
/// Advanced Binary Tools - By Marcussacana
/// </summary>
namespace AdvancedBinary {

    enum StringStyle {
        /// <summary>
        /// C-Style String (null terminated)
        /// </summary>
        CString,
        /// <summary>
        /// Unicode C-Style String (null terminated 2x)
        /// </summary>
        UCString,
        /// <summary>
        /// Pascal-Style String (int32 Length Prefix)
        /// </summary>
        PString
    }


    /// <summary>
    /// InvokeMethod While Reading
    /// </summary>
    /// <param name="Stream">Stream Instance</param>
    /// <param name="FromReader">Determine if the method is invoked from the StructReader or StructWriter</param>
    /// <param name="StructInstance">Struct instance reference</param>
    /// <return>Struct Instance</return>
    public delegate dynamic FieldInvoke(Stream Stream, bool FromReader, dynamic StructInstance);

    /// <summary>
    /// Ignore Struct Field
    /// </summary>
    public class Ignore : Attribute { }

    /// <summary>
    /// C-Style String (null terminated)
    /// </summary>
    public class CString : Attribute { }

    /// <summary>
    /// Unicode C-Style String (null terminated 2x)
    /// </summary>
    public class UCString : Attribute { }
    /// <summary>
    /// Pascal-Style String (int32 Length Prefix)
    /// </summary>
    public class PString : Attribute {
        public string PrefixType = Const.UINT32;
        public bool UnicodeLength;
    }

    public class FString : Attribute {
        public long Length;
    }
    /// <summary>
    /// Struct Field Type (required only to sub structs)
    /// </summary>
    public class StructField : Attribute { }
    internal class Const {
        //Types
        public const string INT8 = "System.SByte";
        public const string UINT8 = "System.Byte";
        public const string INT16 = "System.Int16";
        public const string UINT16 = "System.UInt16";
        public const string INT32 = "System.Int32";
        public const string UINT32 = "System.UInt32";
        public const string DOUBLE = "System.Double";
        public const string FLOAT = "System.Single";
        public const string INT64 = "System.Int64";
        public const string UINT64 = "System.UInt64";
        public const string STRING = "System.String";
        public const string DELEGATE = "System.MulticastDelegate";

        //Attributes
        public const string PSTRING = "PString";
        public const string CSTRING = "CString";
        public const string UCSTRING = "UCString";
        public const string FSTRING = "FString";
        public const string STRUCT = "StructField";
        public const string IGNORE = "Ignore";
    }

    static class Tools {
        /*
        public static dynamic GetAttributePropertyValue<T>(T Struct, string FieldName, string AttributeName, string PropertyName) {
            Type t = Struct.GetType();
            FieldInfo[] Fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo Fld in Fields) {
                if (Fld.Name != FieldName)
                    continue;
                foreach (Attribute tmp in Fld.GetCustomAttributes(true)) {
                    Type Attrib = tmp.GetType();
                    if (Attrib.Name != AttributeName)
                        continue;
                    foreach (FieldInfo Field in Attrib.GetFields()) {
                        if (Field.Name != PropertyName)
                            continue;
                        return Field.GetValue(tmp);
                    }
                    throw new Exception("Property Not Found");
                }
                throw new Exception("Attribute Not Found");
            }
            throw new Exception("Field Not Found");
        }*/


        public static dynamic Reverse(dynamic Data) {
            byte[] Arr = BitConverter.GetBytes(Data);
            Array.Reverse(Arr, 0, Arr.Length);
            string type = Data.GetType().FullName;
            switch (type) {
                case Const.INT8:
                case Const.UINT8:
                    return Data;
                case Const.INT16:
                    return BitConverter.ToInt16(Arr, 0);
                case Const.UINT16:
                    return BitConverter.ToUInt16(Arr, 0);
                case Const.INT32:
                    return BitConverter.ToInt32(Arr, 0);
                case Const.UINT32:
                    return BitConverter.ToUInt32(Arr, 0);
                case Const.INT64:
                    return BitConverter.ToInt64(Arr, 0);
                case Const.UINT64:
                    return BitConverter.ToUInt64(Arr, 0);
                case Const.DOUBLE:
                    return BitConverter.ToDouble(Arr, 0);
                case Const.FLOAT:
                    return BitConverter.ToSingle(Arr, 0);
                default:
                    throw new Exception("Unk Data Type.");
            }
        }

        public static dynamic GetAttributePropertyValue(FieldInfo Fld, string AttributeName, string PropertyName) {
            foreach (Attribute tmp in Fld.GetCustomAttributes(true)) {
                Type Attrib = tmp.GetType();
                if (Attrib.Name != AttributeName)
                    continue;
                foreach (FieldInfo Field in Attrib.GetFields()) {
                    if (Field.Name != PropertyName)
                        continue;
                    return Field.GetValue(tmp);
                }
                throw new Exception("Property Not Found");
            }
            throw new Exception("Attribute Not Found");
        }

        public static long GetStructLength<T>(T Struct) {
            Type type = Struct.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            long Length = 0;
            foreach (FieldInfo field in fields) {
                if (HasAttribute(field, Const.IGNORE))
                    continue;
                switch (field.FieldType.ToString()) {
                    case Const.INT8:
                    case Const.UINT8:
                        Length += 1;
                        break;
                    case Const.INT32:
                    case Const.FLOAT:
                    case Const.UINT32:
                        Length += 4;
                        break;
                    case Const.UINT64:
                    case Const.INT64:
                    case Const.DOUBLE:
                        Length += 8;
                        break;
                    case Const.STRING:
                        if (!HasAttribute(field, Const.FSTRING))
                            throw new Exception("You can't calculate struct length with strings");
                        else
                            Length += GetAttributePropertyValue(field, Const.FSTRING, "Length");
                        break;
                    default:
                        if (field.FieldType.BaseType.ToString() == Const.DELEGATE)
                            break;
                        if (HasAttribute(field, Const.IGNORE))
                            break;
                        throw new Exception("Unk Struct Field: " + field.FieldType.ToString());
                }
            }
            return Length;
        }

        internal static bool HasAttribute(FieldInfo Field, string Attrib) {
            foreach (Attribute attrib in Field.GetCustomAttributes(true))
                if (attrib.GetType().Name == Attrib)
                    return true;
            return false;
        }
        public static void ReadStruct(byte[] Array, ref object Struct, bool IsBigEnddian = false, Encoding Encoding = null) {
            MemoryStream Stream = new MemoryStream(Array);
            StructReader Reader = new StructReader(Stream, IsBigEnddian, Encoding);
            Reader.ReadStruct(ref Struct);
            Reader.Close();
            Stream?.Close();
        }

        public static byte[] BuildStruct<T>(ref T Struct, bool BigEndian = false, Encoding Encoding = null) {
            MemoryStream Stream = new MemoryStream();
            StructWriter Writer = new StructWriter(Stream, BigEndian, Encoding);
            Writer.WriteStruct(ref Struct);
            byte[] Result = Stream.ToArray();
            Writer.Close();
            Stream?.Close();
            return Result;
        }

        internal static void CopyStruct<T>(T Input, ref T Output) {
            Type type = Input.GetType();
            object tmp = Output;
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++) {
                object value = fields[i].GetValue(Input);
                fields[i].SetValue(tmp, value);
            }
            Output = (T)tmp;
        }
    }

    class StructWriter : BinaryWriter {

        private bool BigEndian = false;
        private Encoding Encoding;
        public StructWriter(Stream Input, bool BigEndian = false, Encoding Encoding = null) : base(Input) {
            if (Encoding == null)
                Encoding = Encoding.UTF8;
            this.BigEndian = BigEndian;
            this.Encoding = Encoding;
        }

        public void WriteStruct<T>(ref T Struct) {
            Type type = Struct.GetType();
            object tmp = Struct;
            WriteStruct(type, ref tmp);
            Struct = (T)tmp;
        }

        private void WriteStruct(Type type, ref object Instance) {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields) {
                if (HasAttribute(field, Const.IGNORE))
                    break;
                dynamic Value = field.GetValue(Instance);
                switch (field.FieldType.ToString()) {
                    case Const.STRING:
                        if (HasAttribute(field, Const.CSTRING)) {
                            Write(Value, StringStyle.CString);
                            break;
                        }
                        if (HasAttribute(field, Const.UCSTRING)) {
                            Write(Value, StringStyle.UCString);
                            break;
                        }
                        if (HasAttribute(field, Const.PSTRING)) {
                            Write(field, Value, StringStyle.PString);
                            break;
                        }
                        if (HasAttribute(field, Const.FSTRING)) {
                            byte[] Buffer = new byte[Tools.GetAttributePropertyValue(field, Const.FSTRING, "Length")];
                            Encoding.GetBytes((string)Value).CopyTo(Buffer, 0);
                            Write(Buffer);
                            break;
                        }
                        throw new Exception("String Attribute Not Specified.");
                    default:
                        if (HasAttribute(field, Const.STRUCT)) {
                            WriteStruct(field.FieldType, ref Value);
                        } else {
                            if (field.FieldType.BaseType.ToString() == Const.DELEGATE) {
                                FieldInvoke Invoker = ((FieldInvoke)Value);
                                if (Invoker == null)
                                    break;
                                Instance = Invoker.Invoke(BaseStream, false, Instance);
                                field.SetValue(Instance, Invoker);
                            } else if (BigEndian)
                                Write(Tools.Reverse(Value));
                            else
                                Write(Value);
                        }
                        break;
                }
            }
        }


        public void Write(string String, StringStyle Style) {
            switch (Style) {
                case StringStyle.UCString:
                case StringStyle.CString:
                    List<byte> Buffer = new List<byte>(Encoding.GetBytes(String + "\x0"));
                    base.Write(Buffer.ToArray(), 0, Buffer.Count);
                    break;
                default:
                    base.Write(String);
                    break;
            }
        }
        public void Write(FieldInfo Field, dynamic Value, StringStyle Style) {
            switch (Style) {
                case StringStyle.UCString:
                case StringStyle.CString:
                    List<byte> Buffer = new List<byte>(Encoding.GetBytes(Value + "\x0"));
                    base.Write(Buffer.ToArray(), 0, Buffer.Count);
                    break;
                case StringStyle.PString:
                    WritePString(Field, Value);
                    break;
            }
        }

        private void WritePString(FieldInfo Field, dynamic Value) {
            byte[] Arr = Encoding.GetBytes(Value);

            string Prefix = Tools.GetAttributePropertyValue(Field, Const.PSTRING, "PrefixType");
            bool UnicodeLength = Tools.GetAttributePropertyValue(Field, Const.PSTRING, "UnicodeLength");


            long Length = Arr.LongLength;
            if (UnicodeLength)
                Length /= 2;

            switch (Prefix) {
                case Const.INT16:
                    if (BigEndian)
                        base.Write((short)Tools.Reverse((short)Length));
                    else
                        base.Write((short)Length);
                    break;
                case Const.UINT16:
                    if (BigEndian)
                        base.Write((ushort)Tools.Reverse((ushort)Length));
                    else
                        base.Write((ushort)Length);
                    break;
                case Const.UINT8:
                    if (BigEndian)
                        base.Write((byte)Tools.Reverse((byte)Length));
                    else
                        base.Write((byte)Length);
                    break;
                case Const.INT8:
                    if (BigEndian)
                        base.Write((sbyte)Tools.Reverse((sbyte)Length));
                    else
                        base.Write((sbyte)Length);
                    break;
                case Const.INT32:
                    if (BigEndian)
                        base.Write((int)Tools.Reverse((int)Length));
                    else
                        base.Write((int)Length);
                    break;
                case Const.UINT32:
                    if (BigEndian)
                        base.Write((uint)Tools.Reverse((uint)Length));
                    else
                        base.Write((uint)Length);
                    break;
                case Const.INT64:
                    if (BigEndian)
                        base.Write((long)Tools.Reverse(Length));
                    else
                        base.Write(Length);
                    break;
                default:
                    throw new Exception("Invalid Data Type");
            }
            base.Write(Arr);
        }

        private bool HasAttribute(FieldInfo Field, string Attrib) {
            foreach (Attribute attrib in Field.GetCustomAttributes(true))
                if (attrib.GetType().Name == Attrib)
                    return true;
            return false;
        }

        internal void Seek(long Index, SeekOrigin Origin) {
            base.BaseStream.Seek(Index, Origin);
            base.BaseStream.Flush();
        }
    }

    class StructReader : BinaryReader {

        private bool BigEndian = false;
        private Encoding Encoding;
        public StructReader(Stream Input, bool BigEndian = false, Encoding Encoding = null) : base(Input) {
            if (Encoding == null)
                Encoding = Encoding.UTF8;
            this.BigEndian = BigEndian;
            this.Encoding = Encoding;
        }

        public void ReadStruct<T>(ref T Struct) {
            Type type = Struct.GetType();
            object tmp = Struct;
            ReadStruct(type, ref tmp);
            Struct = (T)tmp;
        }

        private void ReadStruct(Type type, ref object Instance) {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++) {
                FieldInfo field = fields[i];
                if (Tools.HasAttribute(field, Const.IGNORE))
                    continue;
                dynamic Value = null;
                bool IsNumber = true;
                switch (field.FieldType.ToString()) {
                    case Const.INT8:
                        Value = base.ReadSByte();
                        break;
                    case Const.INT16:
                        Value = base.ReadInt16();
                        break;
                    case Const.UINT16:
                        Value = base.ReadUInt16();
                        break;
                    case Const.UINT8:
                        Value = base.ReadByte();
                        break;
                    case Const.INT32:
                        Value = base.ReadInt32();
                        break;
                    case Const.UINT32:
                        Value = base.ReadUInt32();
                        break;
                    case Const.DOUBLE:
                        Value = base.ReadDouble();
                        break;
                    case Const.FLOAT:
                        Value = base.ReadSingle();
                        break;
                    case Const.INT64:
                        Value = base.ReadInt64();
                        break;
                    case Const.UINT64:
                        Value = base.ReadUInt64();
                        break;
                    case Const.STRING:
                        IsNumber = false;
                        if (Tools.HasAttribute(field, Const.CSTRING) && Tools.HasAttribute(field, Const.PSTRING))
                            throw new Exception("You can't use CString and PString Attribute into the same field.");
                        if (Tools.HasAttribute(field, Const.CSTRING)) {
                            Value = ReadString(StringStyle.CString);
                            break;
                        }
                        if (Tools.HasAttribute(field, Const.UCSTRING)) {
                            Value = ReadString(StringStyle.UCString);
                            break;
                        }
                        if (Tools.HasAttribute(field, Const.PSTRING)) {
                            Value = ReadString(StringStyle.PString, field);
                            break;
                        }
                        if (Tools.HasAttribute(field, Const.FSTRING)) {
                            byte[] Buffer = new byte[Tools.GetAttributePropertyValue(field, Const.FSTRING, "Length")];
                            if (Read(Buffer, 0, Buffer.Length) != Buffer.Length)
                                throw new Exception("Failed to Read a String");
                            Value = Encoding.GetString(Buffer);
                            break;
                        }
                        throw new Exception("String Attribute Not Specified.");
                    default:
                        IsNumber = false;
                        if (Tools.HasAttribute(field, Const.STRUCT)) {
                            Value = Activator.CreateInstance(field.FieldType);
                            ReadStruct(field.FieldType, ref Value);
                        } else {
                            if (field.FieldType.BaseType.ToString() == Const.DELEGATE) {
                                FieldInvoke Invoker = (FieldInvoke)field.GetValue(Instance);
                                Value = Invoker;
                                if (Invoker == null)
                                    break;
                                Instance = Invoker.Invoke(BaseStream, true, Instance);
                                break;
                            }
                            throw new Exception("Unk Struct Field: " + field.FieldType.ToString());
                        }
                        break;
                }
                if (IsNumber && BigEndian) {
                    Value = Tools.Reverse(Value);
                }
                field.SetValue(Instance, Value);
            }
        }


        public string ReadString(StringStyle Style, FieldInfo Info = null) {
            List<byte> Buffer = new List<byte>();
            switch (Style) {
                case StringStyle.CString:
                    while (true) {
                        byte Byte = base.ReadByte();
                        if (Byte < 1)
                            break;
                        Buffer.Add(Byte);
                    }
                    return Encoding.GetString(Buffer.ToArray());
                case StringStyle.UCString:
                    while (true) {
                        byte Byte1 = base.ReadByte();
                        byte Byte2 = base.ReadByte();
                        if (Byte1 == 0x00 && Byte2 == 0x00)
                            break;
                        Buffer.Add(Byte1);
                        Buffer.Add(Byte2);
                    }
                    return Encoding.GetString(Buffer.ToArray());
                case StringStyle.PString:
                    if (Info != null) {
                        long Len;
                        string Prefix = Tools.GetAttributePropertyValue(Info, Const.PSTRING, "PrefixType");
                        bool UnicodeLength = Tools.GetAttributePropertyValue(Info, Const.PSTRING, "UnicodeLength");
                        switch (Prefix) {
                            case Const.INT16:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadInt16());
                                else
                                    Len = ReadInt16();
                                break;
                            case Const.UINT16:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadUInt16());
                                else
                                    Len = ReadUInt16();
                                break;
                            case Const.UINT8:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadByte());
                                else
                                    Len = ReadByte();
                                break;
                            case Const.INT8:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadSByte());
                                else
                                    Len = ReadSByte();
                                break;
                            case Const.INT32:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadInt32());
                                else
                                    Len = ReadInt32();
                                break;
                            case Const.UINT32:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadUInt32());
                                else
                                    Len = ReadUInt32();
                                break;
                            case Const.INT64:
                                if (BigEndian)
                                    Len = Tools.Reverse(ReadInt64());
                                else
                                    Len = ReadInt64();
                                break;
                            default:
                                throw new Exception("Invalid Data Type");
                        }
                        if (UnicodeLength)
                            Len *= 2;
                        if (Len > BaseStream.Length - BaseStream.Position)
                            throw new Exception("Invalid Length");
                        byte[] Buff = new byte[Len];
                        while (Len > 0)
                            Len -= BaseStream.Read(Buff, 0, Len > int.MaxValue ? int.MaxValue : (int)Len);
                        return Encoding.GetString(Buff);
                    } else
                        return ReadString();
                default:
                    throw new Exception("Unk Value Type");
            }
        }

        internal void Seek(long Index, SeekOrigin Origin) {
            base.BaseStream.Seek(Index, Origin);
            base.BaseStream.Flush();
        }

        internal int Peek() {
            int b = BaseStream.ReadByte();
            BaseStream.Position--;
            return b;
        }
    }
}
