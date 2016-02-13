using System;
using System.IO;
using System.Text;

namespace KrKrSceneManager
{  
    public class SCENE
    {
        private bool blankstr;
        private int DefaultOffsetSize;
        private int StringTable;
        private int OffsetTable;
        private bool havePosFix = false;
        private string Status = "Not Open";
        private byte[] Source = new byte[0];
        private byte[] posfix = new byte[0];
        private int TablePrefixSize = 0;
        public bool CompressScene = false;
        public int CompressionLevel = 9;
        public string[] Strings = new string[0];

        public byte[] export()
        {
            if (Source.Length == 0)
                throw new Exception("You need import a scene before export.");
            byte[] Script = new byte[OffsetTable + TablePrefixSize];
            for (int pos = 0; pos < Script.Length; pos++)
            {
                Status = "Copying Script...";
                Script[pos] = Source[pos];
            }
            byte[] Offsets = new byte[StringTable - Script.Length];
            byte[] strings = new byte[0];
            int diff = 0;
            if (blankstr)//this make a more compact script. Yes, 1 byte less can make difference (because the limited offset size)
            {
                byte[] hex = Tools.U8StringToByte(Strings[0]);
                byte[] tmp = new byte[strings.Length + hex.Length];
                strings.CopyTo(tmp, 0);
                hex.CopyTo(tmp, strings.Length);
                strings = tmp;
                Offsets = writeOffset(Offsets, 0 * DefaultOffsetSize, 0);
                diff = 1;
            }
            for (int pos = diff; pos < Strings.Length; pos++)
            {
                Status = "Compiling strings... (" + (pos * 100) / Strings.Length + "%)";
                byte[] hex = Tools.U8StringToByte(Strings[pos]);
                byte[] tmp = new byte[strings.Length + hex.Length + 1];
                strings.CopyTo(tmp, 0);
                tmp[strings.Length] = 0x00;
                int offset = (strings.Length + 1);
                hex.CopyTo(tmp, strings.Length + 1);
                strings = tmp;
                Offsets = writeOffset(Offsets, pos * DefaultOffsetSize, offset);
            }
            if (havePosFix)
            {
                Status = "Additing aditional content...";
                byte[] tmp = new byte[strings.Length + posfix.Length];
                strings.CopyTo(tmp, 0);
                for (int i = strings.Length; (i - strings.Length) < posfix.Length; i++)
                {
                    tmp[i] = posfix[i - strings.Length];
                }
                strings = tmp;
            }
            Status = "Generating new scene...";
            byte[] temp = new byte[Script.Length + Offsets.Length + strings.Length];
            Script.CopyTo(temp, 0);
            Offsets.CopyTo(temp, Script.Length);
            strings.CopyTo(temp, Script.Length + Offsets.Length);
            Script = temp;
            if (CompressScene)
            {
                byte[] CompressedScript;
                Tools.CompressData(Script, CompressionLevel, out CompressedScript);
                byte[] RetData = new byte[8 + CompressedScript.Length];
                (new byte[] { 0x6D, 0x64, 0x66, 0x00 }).CopyTo(RetData, 0);
                genOffset(4, Script.Length).CopyTo(RetData, 4);
                CompressedScript.CopyTo(RetData, 8);
                return RetData;
            }
            return Script;
        }

        internal byte[] genOffset(int size, int Value)
        {
            string[] result = new string[0];
            for (int i = 0; i < size; i++)
            {
                string[] temp = new string[result.Length + 1];
                result.CopyTo(temp, 0);
                temp[result.Length] = "00";
                result = temp;
            }
            string var = Tools.IntToHex(Value);
            if (var.Length % 2 != 0)
            {
                var = 0 + var;
            }
            string[] hex = new string[var.Length / 2];
            int tmp = 0;
            for (int i = var.Length - 2; i > -2; i -= 2)
            {
                hex[tmp] = var.Substring(i, 2);
                tmp++;
            }
            tmp = 0;
            for (int i = 0; i < size; i++)
            {
                if (tmp < hex.Length)
                {
                    result[i] = hex[tmp];
                }
                else
                {
                    result[i] = "00";
                }
                tmp++;
            }
            return Tools.StringToByteArray(result);
        }
        internal byte[] writeOffset(byte[] offsets, int position, int Value)
        {
            byte[] result = offsets;
            byte[] var = Tools.IntToByte(Value);
            if (var.Length > DefaultOffsetSize)
            {
                throw new Exception("Edited Strings are too big.");
            }
            byte[] hex = new byte[var.Length];
            int tmp = 0;
            for (int i = var.Length - 1; i >= 0; i--)
            {
                hex[tmp] = var[i];
                tmp++;
            }
            tmp = 0;

            for (int i = position; i < (position + DefaultOffsetSize); i++)
            {
                if (tmp < hex.Length)
                {
                    result[i] = hex[tmp];
                }
                else
                {
                    result[i] = 0x00;
                }
                tmp++;
            }
            return result;
        }

        internal string getRange(byte[] file, int pos, int length)
        {
            byte[] rest = new byte[length];
            for (int i = 0; i < length; i++)
            {
                rest[i] = file[pos + i];
            }
            return Tools.ByteArrayToString(rest).Replace("-", "");
        }
        public SCENE import(byte[] Bin)
        {
            byte[] scene = new byte[0];
            SCENE scn = new SCENE();
            scene = Bin;
            if (getRange(scene, 0, 4) == "6D646600")
            {
                object tmp = new byte[scene.Length - 8];
                for (int i = 8; i < scene.Length; i++)
                    ((byte[])tmp)[i - 8] = scene[i];
                byte[] DecompressedScene;
                Tools.DecompressData((byte[])tmp, out DecompressedScene);
                if (GetOffset(scene, 4, 4, false) != DecompressedScene.Length)
                    throw new Exception("Corrupted MDF Header or Zlib Data");
                scene = DecompressedScene;
            }
            if (getRange(scene, 0, 3) != "505342")
                throw new Exception("Invalid KrKrZ Scene binary");
            scn.Source = scene;
            Status = "Reading Header...";
            int OffsetTablePos = GetOffset(scene, 16, 4, false);
            int StringTablePos = GetOffset(scene, 20, 4, false);
            scn.OffsetTable = OffsetTablePos;
            scn.StringTable = StringTablePos;
            int DefaultOffsetSize = 0;
            Status = "Getting Offsets Size...";
            int BRUTEFORCEBUFFER = 4;//Start of the best way to detect offset size and position
            int minimalsize = 0;
            while (scene.Length - StringTablePos > elevate(0xFF, minimalsize)) //calculate the minimal size to strings offset by file size
                minimalsize++;
            int[] offsets = new int[0];
            if (scene[StringTablePos] != 0x00)//to detect scripts without blank strings (01_05_c.ks.scn from nekopara for sample)
            {
                blankstr = true;
                int[] tmp = new int[offsets.Length + 1];
                offsets.CopyTo(tmp, 0);
                tmp[offsets.Length] = 0;
                offsets = tmp;
            }
            for (int pos = 0; pos + StringTablePos < scene.Length; pos++) //generate string position tree
                if (scene[StringTablePos + pos] == 0x00)
                {
                    int[] tmp = new int[offsets.Length + 1];
                    offsets.CopyTo(tmp, 0);
                    tmp[offsets.Length] = pos + 1;
                    offsets = tmp;
                }
            if (offsets.Length <= BRUTEFORCEBUFFER)
                BRUTEFORCEBUFFER = offsets.Length - 1;
            for (int size = minimalsize; ; size++)//find offsets position with diff offset size
            {
                bool okay = false;
                byte[] OffTable = genOffsetTable(offsets, BRUTEFORCEBUFFER, size);//generate table with specifed offset size
                for (int i = 0; i < size * BRUTEFORCEBUFFER; i++)//find offset table in (size * BRUTEFORCEBUFFER) range
                    if (EqualsAt(scene, OffTable, OffsetTablePos + i))
                    {
                        TablePrefixSize = i;
                        DefaultOffsetSize = size;
                        okay = true;
                        break;
                    }
                if (okay)
                    break;
            }

            scn.DefaultOffsetSize = DefaultOffsetSize;
            scn.TablePrefixSize = TablePrefixSize;
            scn.blankstr = blankstr;
            string[] strs = new string[0];
            for (int pos = OffsetTablePos + TablePrefixSize; pos < StringTablePos; pos += DefaultOffsetSize)
            {
                Status = "Importing Strings... (" + (pos * 100) / StringTablePos + "%)";
                string[] temp = new string[strs.Length + 1];
                strs.CopyTo(temp, 0);
                int index = GetOffset(scene, pos, DefaultOffsetSize, false) + StringTablePos;
                if (scene[index] == 0x00)
                {
                    temp[strs.Length] = string.Empty;
                    strs = temp;
                }
                else
                {
                    temp[strs.Length] = GetString(scene, index);
                    strs = temp;
                }
                if (pos + DefaultOffsetSize >= StringTablePos)
                {
                    int EndLast = -1;
                    for (int i = GetOffset(scene, pos, DefaultOffsetSize, false) + StringTablePos; scene[i] != 0x00 && i < scene.Length; i++)
                    {
                        EndLast = i;
                        if (i + 1 > scene.Length)
                            break;
                    }
                    if (EndLast != -1)
                    {
                        EndLast++;
                        scn.havePosFix = true;
                        for (int i = EndLast; i < scene.Length; i++)
                        {
                            byte[] tmp = new byte[scn.posfix.Length + 1];
                            scn.posfix.CopyTo(tmp, 0);
                            tmp[scn.posfix.Length] = scene[i];
                            scn.posfix = tmp;
                        }
                    }
                }
            }
            scn.Strings = strs;
            Status = "Imported";
            return scn;
        }

        internal int elevate(int ValueToElevate, int ElevateTimes) {
            if (ElevateTimes == 0)
                return 0;
            int elevate = 1;
            int value = ValueToElevate;
            while (elevate < ElevateTimes)
            {
                value *= ValueToElevate;
                elevate++;
            }
            return value;
        }

        internal bool EqualsAt(byte[] OriginalData, byte[] DataToCompare, int PositionToStartCompare)
        {
            if (PositionToStartCompare + DataToCompare.Length > OriginalData.Length)
                return false;
            for (int pos = 0; pos < DataToCompare.Length; pos++)
            {
                if (OriginalData[PositionToStartCompare + pos] != DataToCompare[pos])
                    return false;
            }
            return true;
        }

        internal byte[] genOffsetTable(int[] offsets, int Count, int size)
        {
            byte[] table = new byte[size * Count];
            for (int i = 0; i < Count; i++)
            {
                byte[] offset = genOffset(size, offsets[i]);
                offset.CopyTo(table, i*size);
            }
            return table;
        }

        public string GetStatus()
        {
            return Status;
        }

        private string GetString(byte[] scene, int pos)
        {
            string hex = "";
            for (int i = pos; scene[i] != 0x00 && i + 1 < scene.Length; i++)
                hex += scene[i].ToString("x").ToUpper() + "-";
            hex = hex.Substring(0, hex.Length - 1);
            return Tools.U8HexToString(hex.Split('-')).Replace("\n", "\\n");
        }

        internal int GetOffset(byte[] file, int index, int OffsetSize, bool reverse)
        {
            if (reverse)
            {
                string hex = "";
                for (int i = index; i < index + OffsetSize; i++) { 
                    string var = file[i + index].ToString("x").ToUpper();
                if (var.Length % 2 != 0)
                {
                    var = 0 + var;
                }
                hex += var;
            }
                return Tools.HexToInt(hex);
            }
            else
            {
                string hex = "";
                for (int i = (index + OffsetSize - 1); i > (index - 1); i--)
                {
                    string var = file[i].ToString("x").ToUpper();
                    if (var.Length % 2 != 0)
                    {
                        var = 0 + var;
                    }
                    hex += var;
                }
                return Tools.HexToInt(hex);
            }
        }
    }
    internal class Tools
    {
        internal static void CompressData(byte[] inData, int compression, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, compression))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.Finish();
                outData = outMemoryStream.ToArray();
            }
        }
        internal static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
        internal static void DecompressData(byte[] inData, out byte[] outData)
        {
            try
            {
                using (Stream inMemoryStream = new MemoryStream(inData))
                using (ZInputStream outZStream = new ZInputStream(inMemoryStream))
                {
                    MemoryStream outMemoryStream = new MemoryStream();
                    CopyStream(outZStream, outMemoryStream);
                    outData = outMemoryStream.ToArray();
                }
            }
            catch
            {
                outData = new byte[0];
            }
        }

        internal static void DecompressData(byte[] inData, int OutSize, out byte[] outData)
        {
            outData = new byte[OutSize];
            try
            {
                using (Stream inMemoryStream = new MemoryStream(inData))
                using (ZInputStream outZStream = new ZInputStream(inMemoryStream))
                {
                    int leng = (int)outZStream.Length;
                    for (int i = 0; i < outData.Length; i++)
                        outData[i] = (byte)outZStream.ReadByte();
                }
            }
            catch
            {
                outData = new byte[0];
            }
        }
        public static string IntToHex(int val)
        {
            return val.ToString("X");
        }
        public static byte[] IntToByte(int val)
        {
            string var = IntToHex(val);
            if (var.Length % 2 != 0)
            {
                var = 0 + var;
            }
            return StringToByteArray(var);
        }
        public static string StringToHex(string _in)
        {
            string input = _in;
            char[] values = input.ToCharArray();
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = String.Format("{0:X}", value);
                if (value > 255)
                    return UnicodeStringToHex(input);
                r += value + " ";
            }
            string[] bytes = r.Split(' ');
            byte[] b = new byte[bytes.Length - 1];
            int index = 0;
            foreach (string val in bytes)
            {
                if (index == bytes.Length - 1)
                    break;
                if (int.Parse(val) > byte.MaxValue)
                {
                    b[index] = byte.Parse("0");
                }
                else
                    b[index] = byte.Parse(val);
                index++;
            }
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");
        }
        public static string UnicodeStringToHex(string _in)
        {
            string input = _in;
            char[] values = Encoding.Unicode.GetChars(Encoding.Unicode.GetBytes(input.ToCharArray()));
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = String.Format("{0:X}", value);
                r += value + " ";
            }
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] b = unicode.GetBytes(input);
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");

        }
        public static string U8HexToString(string[] hex)
        {
            byte[] str = StringToByteArray(hex);
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetString(str);
        }
        public static string[] U8StringToHex(string text)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            byte[] cnt = encoder.GetBytes(text.ToCharArray());
            return ByteArrayToString(cnt).Split('-');
        }

        public static byte[] U8StringToByte(string text)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetBytes(text.ToCharArray());
        }

        public static byte[] StringToByteArray(string hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static byte[] StringToByteArray(string[] hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars];
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] = Convert.ToByte(hex[i], 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex;
        }

        public static int HexToInt(string hex)
        {
            int num = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return num;
        }

        public static string HexToString(string hex)
        {
            string[] hexValuesSplit = hex.Split(' ');
            string returnvar = "";
            foreach (string hexs in hexValuesSplit)
            {
                int value = Convert.ToInt32(hexs, 16);
                char charValue = (char)value;
                returnvar += charValue;
            }
            return returnvar;
        }

        public static string UnicodeHexToUnicodeString(string hex)
        {
            string hexString = hex.Replace(@" ", "");
            int length = hexString.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return Encoding.Unicode.GetString(bytes);
        }

    }
}
