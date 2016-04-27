using System;
using System.IO;
using System.Text;


/*
 * KrKrSceneManager (4.2) By Marcussacana
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
                Script = writeOffset(ref Script, 0x14, OffsetTable + TablePrefixSize + (StrCount * OffsetSize), OffsetSize);
            }

            byte[] Offsets = new byte[StrCount * OffsetSize];
            byte[] strings = new byte[0];
            int diff = 0;
            byte[] tmp;
            for (int pos = diff; pos < Strings.Length; pos++) {
                Status = "Compiling strings... (" + (pos * 100) / Strings.Length + "%)";
                byte[] hex = Encoding.UTF8.GetBytes(Strings[pos]);
                tmp = new byte[strings.Length + hex.Length + 1];
                strings.CopyTo(tmp, 0);
                tmp[strings.Length] = 0x00;
                int offset = strings.Length;
                hex.CopyTo(tmp, offset);
                strings = tmp;
                Offsets = writeOffset(ref Offsets, pos * OffsetSize, offset, OffsetSize);
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
        private byte[] shortdword(byte[] off) {
            int length = off.Length-1;
            while (off[length] == 0x0 && length > 0)
                length--;
            byte[] rst = new byte[length+1];
            for (int i = 0; i < rst.Length; i++) {
                rst[i] = off[i];
            }
            return rst;
        }
        internal byte[] genOffset(int size, int Value) {
            byte[] Off = BitConverter.GetBytes(Value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Off);
            Off = shortdword(Off);
            if (Off.Length > size)
                throw new Exception("Edited Strings are too big.");
            if (Off.Length < size) {
                byte[] rst = new byte[size];
                Off.CopyTo(rst, 0);
                Off = rst;
            } 
            return Off;
        }
        internal byte[] writeOffset(ref byte[] offsets, int position, int Value, int OffsetSize) {
            byte[] Offset = genOffset(OffsetSize, Value);
            Offset.CopyTo(offsets, position);
            return offsets;
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
            string rst = string.Empty;
            for (int i = 0; i < length; i++) {
                string hex = file[pos + i].ToString("x").ToUpper();
                if (hex.Length == 1)
                    hex = "0" + hex;
                rst += hex;
            }
            return rst;
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
        public bool IsValidPackget(byte[] Packget) {
            if (getRange(Packget, 0, 4) == "6D646600")
                return true;
            if (getRange(Packget, 0, 3) == "505342")
                return true;
            return false;
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
            MemoryStream arr = new MemoryStream();
            for (int i = pos; scene[i] != 0x00 && i + 1 < scene.Length; i++)
                arr.Write(new byte[] { scene[i] }, 0, 1);
            arr.Seek(0, SeekOrigin.Begin);
            byte[] rst = new byte[arr.Length];
            arr.Read(rst, 0, (int)arr.Length);
            arr.Close();
            return Encoding.UTF8.GetString(rst);
        }

        internal int GetOffset(byte[] file, int index, int OffsetSize) {
            byte[] value = new byte[4];
            for (int i = 0; i < OffsetSize; i++) {
                value[i] = file[i + index];
            }
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(value, 0, 4);
            return BitConverter.ToInt32(value, 0);
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
                        
    }
}