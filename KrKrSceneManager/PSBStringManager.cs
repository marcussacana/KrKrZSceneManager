using System;
using System.IO;
using System.Text;


/*
 * KrKrSceneManager (4.0) By Marcussacana
 * Usage:
 * PSBStringManager StrMan = new PSBStringManager();
 * byte[] input = File.ReadAllBytes("C:\\sample.bin");
 * StrMan.Import(input);
 * string[] scncontent = StrMan.Strigs; 
 * ...
 * //save
 * StrMan.Strings = scncontent;
 * byte[] output = StrMan.Export();
 * File.WriteAllBytes("C:\\sample_out.bin", output);
*/

namespace KrKrSceneManager {
    public class PSBStringManager {
        private int DefaultOffsetSize;
        private int StringTable;
        private int OffsetTable;
        private int StrCount;
        private string Status = "Not Open";
        private byte[] Source = new byte[0];
        private byte[] sufix = new byte[0];
        private int TablePrefixSize = 0;

        //settings
        public bool CompressPackget = false;
        public int CompressionLevel = 9;
        public string[] Strings = new string[0];
        public bool ResizeOffsets = false;

        public bool Initialized { get; private set; }

        public byte[] Export() {
            if (!Initialized)
                throw new Exception("You need import a scene before export.");
            byte[] Script = new byte[OffsetTable + TablePrefixSize];
            for (int pos = 0; pos < Script.Length; pos++) {
                Status = "Copying Script...";
                Script[pos] = Source[pos];
            }
            int OffsetSize = DefaultOffsetSize;
            if (ResizeOffsets) {
                Script[Script.Length - 1] = ConvertSize(4);
                OffsetSize = 4;
                writeOffset(Script, 0x14, OffsetTable + TablePrefixSize + (StrCount * OffsetSize), OffsetSize);
            }

            byte[] Offsets = new byte[StrCount * OffsetSize];
            byte[] strings = new byte[0];
            int diff = 0;
            byte[] tmp;
            for (int pos = diff; pos < Strings.Length; pos++) {
                Status = "Compiling strings... (" + (pos * 100) / Strings.Length + "%)";
                byte[] hex = Tools.U8StringToByte(Strings[pos]);
                tmp = new byte[strings.Length + hex.Length + 1];
                strings.CopyTo(tmp, 0);
                tmp[strings.Length] = 0x00;
                int offset = strings.Length;
                hex.CopyTo(tmp, offset);
                strings = tmp;
                Offsets = writeOffset(Offsets, pos * OffsetSize, offset, OffsetSize);
            }
            Status = "Additing Others Resources...";
            tmp = new byte[strings.Length + sufix.Length];
            strings.CopyTo(tmp, 0);
            for (int i = strings.Length; (i - strings.Length) < sufix.Length; i++) {
                tmp[i] = sufix[i - strings.Length];
            }
            strings = tmp;
            Status = "Generating new scene...";
            byte[] temp = new byte[Script.Length + Offsets.Length + strings.Length];
            Script.CopyTo(temp, 0);
            Offsets.CopyTo(temp, Script.Length);
            strings.CopyTo(temp, Script.Length + Offsets.Length);
            Script = temp;

            //offsets fix
            int StartPos = GetOffset(Source, 0x20, 4),
            ResOffPos = GetOffset(Source, 0x18, 4),
            ResSizePos = GetOffset(Source, 0x1C, 4);
            int Diff = Script.Length - Source.Length;
            if (StartPos > StringTable)//If is after string table...
                Script = OverWrite(Script, genOffset(4, StartPos + Diff), 0x20);//Update the Difference
            if (ResOffPos > StringTable)
                Script = OverWrite(Script, genOffset(4, ResOffPos + Diff), 0x18);
            if (ResSizePos > StringTable)
                Script = OverWrite(Script, genOffset(4, ResSizePos + Diff), 0x1C);
            return CompressPackget ? MakeMDF(Script) : Script;
        }
        internal byte[] OverWrite(byte[] Main, byte[] NewData, int Postion) {
            for (int i = 0; i < NewData.Length; i++) {
                Main[Postion + i] = NewData[i];
            }
            return Main;
        }
        internal byte[] MakeMDF(byte[] psb) {
            byte[] CompressedScript;
            Tools.CompressData(psb, CompressionLevel, out CompressedScript);
            byte[] RetData = new byte[8 + CompressedScript.Length];
            (new byte[] { 0x6D, 0x64, 0x66, 0x00 }).CopyTo(RetData, 0);
            genOffset(4, psb.Length).CopyTo(RetData, 4);
            CompressedScript.CopyTo(RetData, 8);
            return RetData;
        }

        #region res
        internal byte[] genOffset(int size, int Value) {
            string[] result = new string[0];
            for (int i = 0; i < size; i++) {
                string[] temp = new string[result.Length + 1];
                result.CopyTo(temp, 0);
                temp[result.Length] = "00";
                result = temp;
            }
            string var = Tools.IntToHex(Value);
            if (var.Length % 2 != 0) {
                var = 0 + var;
            }
            string[] hex = new string[var.Length / 2];
            int tmp = 0;
            for (int i = var.Length - 2; i > -2; i -= 2) {
                hex[tmp] = var.Substring(i, 2);
                tmp++;
            }
            tmp = 0;
            for (int i = 0; i < size; i++) {
                if (tmp < hex.Length) {
                    result[i] = hex[tmp];
                }
                else {
                    result[i] = "00";
                }
                tmp++;
            }
            return Tools.StringToByteArray(result);
        }
        internal byte[] writeOffset(byte[] offsets, int position, int Value, int OffsetSize) {
            byte[] result = offsets;
            byte[] var = Tools.IntToByte(Value);
            if (var.Length > OffsetSize) {
                throw new Exception("Edited Strings are too big.");
            }
            byte[] hex = new byte[var.Length];
            int tmp = 0;
            for (int i = var.Length - 1; i >= 0; i--) {
                hex[tmp] = var[i];
                tmp++;
            }
            tmp = 0;

            for (int i = position; i < (position + OffsetSize); i++) {
                if (tmp < hex.Length) {
                    result[i] = hex[tmp];
                }
                else {
                    result[i] = 0x00;
                }
                tmp++;
            }
            return result;
        }

        private int GetOffsetSize(byte[] file) {
            int pos = GetOffset(file, 0x10, 4);
            int FirstSize = ConvertSize(file[pos++]);
            return ConvertSize(file[FirstSize + pos]);
        }
        private int GetStrCount(byte[] file) {
            int pos = GetOffset(file, 0x10, 4);
            int Size = ConvertSize(file[pos++]);
            return GetOffset(file, pos, Size);
        }
        private int GetPrefixSize(byte[] file) {
            return ConvertSize(file[GetOffset(file, 0x10, 4)]) + 2;
        }
        #endregion

        internal byte ConvertSize(int s) {
            switch (s) {
                case 1:
                    return 0xD;
                case 2:
                    return 0xE;
                case 3:
                    return 0xF;
                case 4:
                    return 0x10;
            }
            throw new Exception("Unknow Offset Size");
        }
        internal int ConvertSize(byte b) {
            switch (b) {
                case 0xD:
                    return 1;
                case 0xE:
                    return 2;
                case 0xF:
                    return 3;
                case 0x10:
                    return 4;
            }
            throw new Exception("Unknow Offset Size");
        }
        internal string getRange(byte[] file, int pos, int length) {
            byte[] rest = new byte[length];
            for (int i = 0; i < length; i++) {
                rest[i] = file[pos + i];
            }
            return Tools.ByteArrayToString(rest).Replace("-", "");
        }
        internal byte[] GetMDF(byte[] mdf) {
            object tmp = new byte[mdf.Length - 8];
            for (int i = 8; i < mdf.Length; i++)
                ((byte[])tmp)[i - 8] = mdf[i];
            byte[] DecompressedMDF;
            Tools.DecompressData((byte[])tmp, out DecompressedMDF);
            if (GetOffset(mdf, 4, 4) != DecompressedMDF.Length)
                throw new Exception("Corrupted MDF Header or Zlib Data");
            return DecompressedMDF;
        }
        public void Import(byte[] Packget) {
            if (getRange(Packget, 0, 4) == "6D646600")
                Packget = GetMDF(Packget);
            if (getRange(Packget, 0, 3) != "505342")
                throw new Exception("Invalid KrKrZ Scene binary");
            Source = Packget;
            Status = "Reading Header...";
            OffsetTable = GetOffset(Packget, 16, 4);
            StringTable = GetOffset(Packget, 20, 4);
            DefaultOffsetSize = GetOffsetSize(Packget);
            TablePrefixSize = GetPrefixSize(Packget);
            StrCount = GetStrCount(Packget);
            Strings = new string[StrCount];
            for (int str = -1, pos = OffsetTable + TablePrefixSize; pos < StringTable; pos += DefaultOffsetSize) {
                str++;
                Status = "Importing Strings... (" + (str * 100) / StrCount + "%)";
                int index = GetOffset(Packget, pos, DefaultOffsetSize) + StringTable;
                if (Packget[index] == 0x00)
                    Strings[str] = string.Empty;
                else
                    Strings[str] = GetString(Packget, index);

                if (pos + DefaultOffsetSize >= StringTable) //if the for loop ends now
                {//get end of file
                    int Size = Encoding.UTF8.GetBytes(Strings[str]).Length + 1;
                    if (index + Size <= Packget.Length) {
                        sufix = new byte[Packget.Length - (index + Size)];
                        for (int i = index + Size, b = 0; i < Packget.Length; i++, b++)
                            sufix[b] = Packget[i];
                    }
                }
            }
            Status = "Imported";
            Initialized = true;
        }
        
        public string GetStatus() {
            return Status;
        }

        private string GetString(byte[] scene, int pos) {
            string hex = "";
            for (int i = pos; scene[i] != 0x00 && i + 1 < scene.Length; i++)
                hex += scene[i].ToString("x").ToUpper() + "-";
            hex = hex.Substring(0, hex.Length - 1);
            return Tools.U8HexToString(hex.Split('-')).Replace("\n", "\\n");
        }

        internal int GetOffset(byte[] file, int index, int OffsetSize) {
            string hex = "";
            for (int i = (index + OffsetSize - 1); i > (index - 1); i--) {
                string var = file[i].ToString("x").ToUpper();
                if (var.Length % 2 != 0) {
                    var = 0 + var;
                }
                hex += var;
            }
            return Tools.HexToInt(hex);
        }
    }
    internal class Tools {
        internal static void CompressData(byte[] inData, int compression, out byte[] outData) {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, compression))
            using (Stream inMemoryStream = new MemoryStream(inData)) {
                CopyStream(inMemoryStream, outZStream);
                outZStream.Finish();
                outData = outMemoryStream.ToArray();
            }
        }
        internal static void CopyStream(Stream input, Stream output) {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0) {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
        internal static void DecompressData(byte[] inData, out byte[] outData) {
            try {
                using (Stream inMemoryStream = new MemoryStream(inData))
                using (ZInputStream outZStream = new ZInputStream(inMemoryStream)) {
                    MemoryStream outMemoryStream = new MemoryStream();
                    CopyStream(outZStream, outMemoryStream);
                    outData = outMemoryStream.ToArray();
                }
            }
            catch {
                outData = new byte[0];
            }
        }

        public static string IntToHex(int val) {
            return val.ToString("X");
        }
        public static byte[] IntToByte(int val) {
            string var = IntToHex(val);
            if (var.Length % 2 != 0) {
                var = 0 + var;
            }
            return StringToByteArray(var);
        }        
        public static string U8HexToString(string[] hex) {
            byte[] str = StringToByteArray(hex);
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetString(str);
        }

        public static byte[] U8StringToByte(string text) {
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetBytes(text.ToCharArray());
        }

        public static byte[] StringToByteArray(string hex) {
            try {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static byte[] StringToByteArray(string[] hex) {
            try {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars];
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] = Convert.ToByte(hex[i], 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static string ByteArrayToString(byte[] ba) {
            string hex = BitConverter.ToString(ba);
            return hex;
        }

        public static int HexToInt(string hex) {
            int num = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return num;
        }
        
    }
}