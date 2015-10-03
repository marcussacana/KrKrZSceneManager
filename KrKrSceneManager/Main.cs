using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KrKrSceneManager
{
    public class SCENE {
        private int DefaultOffsetSize;
        private int StringTable;
        private int OffsetTable;
        private bool havePosFix = false;
        private string Status = "Not Open";
        private string[] Source = new string[0];
        private string[] posfix = new string[0];
        public string[] Strings = new string[0];
        public byte[] export()
        {
            if (Source.Length == 0)
                throw new Exception("You need import a scene before export.");
            string[] Script = new string[OffsetTable + 4 + DefaultOffsetSize];
            for (int pos = 0; pos < Script.Length; pos++) {
                Status = "Copying Script...";
                Script[pos] = Source[pos];
            }
            string[] Offsets = new string[StringTable-Script.Length];
            string[] strings= new string[0];
            for (int pos = 0; pos < Strings.Length; pos++){
                Status = "Compiling strings... (" + (pos*100)/Strings.Length + "%)";
                string[] hex = Tools.U8StringToHex(Strings[pos]);
                string[] tmp = new string[strings.Length + hex.Length + 1];
                strings.CopyTo(tmp, 0);
                tmp[strings.Length] = "00";
                int offset = strings.Length + 1;
                hex.CopyTo(tmp, strings.Length+1);
                strings = tmp;
               Offsets = writeOffset(Offsets, pos*DefaultOffsetSize, offset);
            }
            if (havePosFix) {
                Status = "Additing aditional content...";
                string[] tmp = new string[strings.Length+posfix.Length];
                strings.CopyTo(tmp, 0);
                for (int i = strings.Length; (i - strings.Length) < posfix.Length; i++) {
                    tmp[i] = posfix[i - strings.Length];
                }
                strings = tmp;
            }
            Status = "Generating new scene...";
            string[] temp = new string[Script.Length + Offsets.Length + strings.Length];
            Script.CopyTo(temp, 0);
            Offsets.CopyTo(temp, Script.Length);
            strings.CopyTo(temp, Script.Length+Offsets.Length);
            Script = temp;
            return Tools.StringToByteArray(Script);
        }

        private string[] writeOffset(string[] offsets, int position, int Value)
        {
            string[] result = offsets;
            string var = Tools.IntToHex(Value);
            if (var.Length % 2 != 0){
                var = 0 + var;
            }
            if (var.Length/2 > DefaultOffsetSize){
                throw new Exception("Edited Strings are too big.");
            }
            string[] hex = new string[var.Length/2];
            int tmp = 0;
            for (int i = var.Length - 2; i > -2; i -= 2){
                hex[tmp] = var.Substring(i, 2);
                tmp++;
            }
            tmp = 0;

            for (int i = position; i < (position+DefaultOffsetSize); i++){
                if (tmp < hex.Length){
                    result[i] = hex[tmp];
                } else {
                    result[i] = "00";
                }
                tmp++;
            }
            return result;
        }

        public SCENE import(byte[] Bin)
        {
            string[] scene = new string[0];
            SCENE scn = new SCENE();
            scene = Tools.ByteArrayToString(Bin).Split('-');
            if (scene[0] + scene[1] + scene[2] != "505342")
                throw new Exception("Invalid KrKrz Scene file");
            scn.Source = scene;
            Status = "Reading Header...";
            int OffsetTablePos = GetOffset(scene, 16, 4, false);
            int StringTablePos = GetOffset(scene, 20, 4, false);
            scn.OffsetTable = OffsetTablePos;
            scn.StringTable = StringTablePos;
            int DefaultOffsetSize = 0;
            Status = "Getting Offsets Size...";
            for (int index = OffsetTablePos + 4; scene[index] == "00"; index++)
            {
                DefaultOffsetSize++;
            }
            scn.DefaultOffsetSize = DefaultOffsetSize;
            string[] strs = new string[0];
            for (int pos = OffsetTablePos + 4 + DefaultOffsetSize; pos < StringTablePos; pos += DefaultOffsetSize)
            {
                Status = "Importing Strings... (" + (pos*100)/StringTablePos + "%)";
                string[] temp = new string[strs.Length + 1];
                strs.CopyTo(temp, 0);
                temp[strs.Length] = GetString(scene, GetOffset(scene, pos, DefaultOffsetSize, false)+StringTablePos);
                strs = temp;
                if (pos + DefaultOffsetSize >= StringTablePos){
                    int EndLast = -1;
                    for (int i = GetOffset(scene, pos, DefaultOffsetSize, false)+StringTablePos; scene[i] != "00" && i < scene.Length; i++){
                        EndLast = i;
                        if (i + 1 > scene.Length)
                            break;
                    }
                    if (EndLast != -1){
                        EndLast++;
                        scn.havePosFix = true;
                        for (int i = EndLast; i < scene.Length; i++){
                            string[] tmp = new string[scn.posfix.Length + 1];
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
        public string GetStatus()
        {
            return Status;
        }

        private string GetString(string[] scene, int pos)
        {
            string hex = "";
            for (int i = pos; scene[i] != "00" && i + 1 < scene.Length; i++)
                hex += scene[i] + "-";
            hex = hex.Substring(0, hex.Length-1);
            return Tools.U8HexToString(hex.Split('-')).Replace("\n", "\\n");
        }

        private int GetOffset(string[] file, int index, int OffsetSize, bool reverse)
        {
            if (reverse)
            {
                string hex = "";
                for (int i = index; i < index + OffsetSize; i++)
                    hex += file[i + index];
                return Tools.HexToInt(hex);
            }
            else
            {
                string hex = "";
                for (int i = (index + OffsetSize - 1); i > (index - 1); i--)
                    hex += file[i];
                return Tools.HexToInt(hex);
            }
        }
    }
    class Tools
    {
        public static string IntToHex(int val)
        {
            return val.ToString("X");
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
