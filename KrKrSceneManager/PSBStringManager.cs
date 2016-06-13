using System;
using System.IO;
using System.Text;
using System.Linq;

/*
 * KrKrSceneManager (4.7) By Marcussacana
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
        private int OffsetLength;
        private int StringTable;
        private int OffsetTable;
        private int StrCount;
        private byte[] Source = new byte[0];
        private byte[] Sufix = new byte[0];

        /// <summary>
        /// Table Header Length
        /// </summary>
        private int TblHrdLen = 0;

        //settings
        public bool CompressPackget = false;
        public int CompressionLevel = 9;
        public string[] Strings = new string[0];
        public bool ResizeOffsets = false;

        public bool Initialized { get; private set; }

        public byte[] Export() {
            if (!Initialized)
                throw new Exception("You need import a scene before export.");

            //Copy Script Backup;
            byte[] Script = new byte[OffsetTable + TblHrdLen];
            Array.Copy(Source, 0, Script, 0, Script.Length);

            //Get offset size or update if needed
            int OffsetSize = OffsetLength;
            if (ResizeOffsets) {
                Script[Script.Length - 1] = ConvertSize(4);
                OffsetSize = 4;
                genOffset(OffsetSize, OffsetTable + TblHrdLen + (StrCount * OffsetSize)).CopyTo(Script, 0x14);
            }


            //Generate String and Offset Table
            byte[] Offsets = new byte[StrCount * OffsetSize];
            MemoryStream Buffer = new MemoryStream();
            for (int pos = 0; pos < Strings.Length; pos++) {
                genOffset(OffsetSize, (int)Buffer.Length).CopyTo(Offsets, pos * OffsetSize);//Write Offset

                //Append String
                byte[] str = Encoding.UTF8.GetBytes(Strings[pos] + "\x0");
                Buffer.Write(str, 0, str.Length);
            }
            byte[] strings = Buffer.ToArray();
            Buffer.Close();

            //Merge all data
            System.Collections.Generic.IEnumerable<byte> OutScript = Script;
            OutScript = OutScript.Concat(Offsets);
            OutScript = OutScript.Concat(strings);
            OutScript = OutScript.Concat(Sufix);
            Script = OutScript.ToArray();
            OutScript = new byte[0]; //Free Memory

            //Update Offsets
            int StartPos = GetOffset(Source, 0x20, 4),
            ResOffPos = GetOffset(Source, 0x18, 4),
            ResSizePos = GetOffset(Source, 0x1C, 4);
            int Diff = Script.Length - Source.Length;

            if (StartPos > StringTable)//If is after string table...
                genOffset(4, StartPos + Diff).CopyTo(Script, 0x20);//Update the Difference
            if (ResOffPos > StringTable)
                genOffset(4, ResOffPos + Diff).CopyTo(Script, 0x18);
            if (ResSizePos > StringTable)
                genOffset(4, ResSizePos + Diff).CopyTo(Script, 0x1C);

            //Return compressed packget if request...
            return CompressPackget ? MakeMDF(Script) : Script;
        }
        public byte[] TryRecovery(byte[] data) {
            PackgetStatus Status = GetPackgetStatus(data);
            if (Status == PackgetStatus.Invalid)
                throw new Exception("Invalid Packget");
            bool mdf = false;
            if (Status == PackgetStatus.MDF) {
                mdf = true;
                data = GetMDF(data);
            }
            int StrOff = GetOffset(data, 0x10, 4);
            int StrData = GetOffset(data, 0x14, 4);
            int CntSize = ConvertSize(data[StrOff]);
            int Count = GetOffset(data, StrOff + 1, CntSize);
            int Size = ConvertSize(data[StrOff + 1 + CntSize]);
            int EndStr = (StrOff + 2 + CntSize) + ((Count - 1) * Size);
            EndStr = GetOffset(data, EndStr, Size) + StrData;
            while (data[EndStr] != 0x00)
                EndStr++;
            byte[] seq = new byte[] { 0xD, 0x0, 0xD };
            if (EqualsAt(data, seq, EndStr + 1) && EqualsAt(data, seq, EndStr + 1 + seq.Length)) {
                genOffset(4, EndStr + 1).CopyTo(data, 0x18);
                genOffset(4, EndStr + 4).CopyTo(data, 0x1C);//+3
                genOffset(4, EndStr + 7).CopyTo(data, 0x20);//+6
                return mdf ? MakeMDF(data) : data;
            }
            else {
                try {
                    int tmp = ConvertSize(data[GetOffset(data, 0x18, 4)]);
                    tmp = ConvertSize(data[GetOffset(data, 0x1C, 4)]);
                    return mdf ? MakeMDF(data) : data; //Looks all Rigth
                }
                catch {
                    throw new Exception("You Can't Recovery because this packget contains data.");
                }
            }
        }

        private bool EqualsAt(byte[] data, byte[] CompareData, int pos) {
            if (CompareData.Length + pos > data.Length)
                return false;
            for (int i = 0; i < CompareData.Length; i++)
                if (data[i + pos] != CompareData[i])
                    return false;
            return true;
        }
        internal byte[] MakeMDF(byte[] psb) {
            byte[] CompressedScript;
            Tools.CompressData(psb, CompressionLevel, out CompressedScript);

            byte[] RetData = new byte[8 + CompressedScript.Length];
            (new byte[] { 0x6D, 0x64, 0x66, 0x00 }).CopyTo(RetData, 0);//Signature
            genOffset(4, psb.Length).CopyTo(RetData, 4);//Decompressed Length
            CompressedScript.CopyTo(RetData, 8);//ZLIB Data

            return RetData;
        }

        #region res

        /// <summary>
        /// Resize DWORD to your minimal length
        /// </summary>
        /// <param name="off">DWORD to resize</param>
        /// <returns>Shorted DWORD</returns>
        private byte[] shortdword(byte[] off) {
            int length = off.Length - 1;
            while (off[length] == 0x0 && length > 0)
                length--;
            byte[] rst = new byte[length + 1];
            for (int i = 0; i < rst.Length; i++) {
                rst[i] = off[i];
            }
            return rst;
        }

        /// <summary>
        /// Generate a DWORD with specifed value and length
        /// </summary>
        /// <param name="size">DWORD Length</param>
        /// <param name="Value">DWORD Value</param>
        /// <returns>Result DWORD</returns>
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
            byte[] zlib = new byte[mdf.Length - 8];
            Array.Copy(mdf, 8, zlib, 0, mdf.Length - 8);

            byte[] DecompressedMDF;
            Tools.DecompressData(zlib, out DecompressedMDF);
            if (GetOffset(mdf, 4, 4) != DecompressedMDF.Length)
                throw new Exception("Corrupted MDF Header or Zlib Data");

            return DecompressedMDF;
        }
        public enum PackgetStatus {
            MDF, PSB, Invalid
        }
        public PackgetStatus GetPackgetStatus(byte[] Packget) {
            if (getRange(Packget, 0, 4) == "6D646600")
                return PackgetStatus.MDF;
            if (getRange(Packget, 0, 3) == "505342")
                return PackgetStatus.PSB;
            return PackgetStatus.Invalid;
        }
        public void Import(byte[] Packget) {
            PackgetStatus Status = GetPackgetStatus(Packget);
            switch (Status) {
                case PackgetStatus.Invalid:
                    throw new Exception("Invalid Packget");
                case PackgetStatus.MDF:
                    Source = GetMDF(Packget);
                    CompressPackget = true;
                    break;
                case PackgetStatus.PSB:
                    Source = Packget;
                    break;
            }

            //Initialize Variables
            OffsetTable = GetOffset(Source, 16, 4);
            StringTable = GetOffset(Source, 20, 4);
            OffsetLength = GetOffsetSize(Source);
            TblHrdLen = GetPrefixSize(Source);
            StrCount = GetStrCount(Source);

            //Get Strings
            Strings = new string[StrCount];
            int Offset = 0;
            for (int i = 0; i < StrCount; i++) {
                Offset = OffsetTable + TblHrdLen + (i * OffsetLength);
                Offset = GetOffset(Source, Offset, OffsetLength) + StringTable;
                Strings[i] = Source[Offset] == 0x00 ? string.Empty : GetString(Source, Offset);
            }

            //Get End position of the last string
            while (Source[Offset] != 0x00)
                Offset++;

            //Get All data after string Table
            int SufixPos = Offset + 1;
            if (SufixPos < Source.Length) {
                int Length = Source.Length - SufixPos;
                Sufix = new byte[Length];
                Array.Copy(Source, SufixPos, Sufix, 0, Length);
            }

            Initialized = true;
        }
        private string GetString(byte[] scene, int pos) {
            MemoryStream arr = new MemoryStream();
            for (int i = pos; scene[i] != 0x00 && i + 1 < scene.Length; i++)
                arr.Write(new byte[] { scene[i] }, 0, 1);

            byte[] rst = new byte[arr.Length];
            arr.Position = 0;
            arr.Read(rst, 0, (int)arr.Length);
            arr.Close();

            return Encoding.UTF8.GetString(rst);
        }

        internal int GetOffset(byte[] file, int index, int OffsetSize) {
            byte[] value = new byte[4];
            Array.Copy(file, index, value, 0, OffsetSize);

            if (!BitConverter.IsLittleEndian)//Force Little Endian DWORD
                Array.Reverse(value, 0, 4);

            return BitConverter.ToInt32(value, 0);
        }
    }
}

