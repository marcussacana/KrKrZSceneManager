using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KrKrSceneManager
{

    public class PSBResourceManager
    {

        private byte[] packget;
        private PSBStringManager DataTools = new PSBStringManager();
        public int EntryCount { get; private set; }
        public int ResSizePos { get; private set; }

        private bool Initialized;
        private int OffsetPos;
        private int OffsetSize;
        private int OffsetTablePos;
        private int StartPos;
        private int ResSizeOffSize;
        private int ResSizeOffTablePos;

        public bool CompressPackget = false;
        public int CompressionLevel = 9;
        public bool FixOffsets = true;
        public FileEntry[] Import(byte[] script)
        {
            if (DataTools.getRange(script, 0, 4) == "6D646600")
                script = DataTools.GetMDF(script);
            if (DataTools.getRange(script, 0, 4) != "50534200")
                throw new Exception("Bad File Format");
            packget = script;
            StartPos = DataTools.GetOffset(packget, 0x20, 4);
            OffsetPos = DataTools.GetOffset(packget, 0x18, 4);
            int[] tmp = GetOffsetInfo(packget, OffsetPos);
            OffsetSize = tmp[0];
            OffsetTablePos = tmp[1];
            ResSizePos = DataTools.GetOffset(packget, 0x1C, 4);
            tmp = GetOffsetInfo(packget, ResSizePos);
            ResSizeOffSize = tmp[0];
            ResSizeOffTablePos = tmp[1];
            int[] Offsets = GetValues(packget, OffsetPos);
            int[] Sizes = GetValues(packget, ResSizePos);
            EntryCount = Offsets.Length;
            FileEntry[] Files = new FileEntry[EntryCount];
            for (int i = 0; i < EntryCount; i++)
            {
                int EndPos = Offsets[i] + Sizes[i];
                byte[] data = new byte[Sizes[i]];
                for (int ind = Offsets[i]; ind < EndPos; ind++)
                    data[ind - Offsets[i]] = packget[ind + StartPos];
                Files[i] = new FileEntry() { Data = data };
            }
            Initialized = true;
            return Files;
        }
        
        
        private int[] GetOffsetInfo(byte[] file, int pos)
        {
            int OffSize = DataTools.ConvertSize(file[pos]);
            int Count = DataTools.GetOffset(file, pos + 1, OffSize);
            pos += 1 + OffSize;
            //return[0] = OffsetSize;
            //return[1] = StartOffsetPos;
            //return[2] = EntryCount;
            return new int[] { DataTools.ConvertSize(file[pos]), pos + 1, Count };
        }

        private int[] GetValues(byte[] file, int pos)
        {
            int[] tmp = GetOffsetInfo(file, pos);
            int[] Result = new int[tmp[2]];
            pos = tmp[1];
            int OffSize = tmp[0];
            for (int i = 0; i < Result.Length; i++)
            {
                Result[i] = DataTools.GetOffset(file, pos + (i * OffSize), OffSize);
            }
            return Result;
        }
        public byte[] Export(FileEntry[] Resources)
        {
            if (!Initialized)
                throw new Exception("You Need Import Before Export");
            if (Resources.Length != EntryCount)
                throw new Exception("You Can't Add or Delete Resources.");
            int TotalSize = 0;
            for (int i = 0; i < Resources.Length; i++)
                TotalSize += Resources[i].Data.Length + (FixOffsets && i + 1 != Resources.Length ? 4 - ((StartPos + TotalSize + Resources[i].Data.Length) % 4) : 0);
            byte[] ResTable = new byte[TotalSize];
            TotalSize = 0;
            byte[] MainData = CutAt(packget, StartPos);
            for (int i = 0; i < Resources.Length; i++)
            {
                byte[] file = Resources[i].Data;
                file.CopyTo(ResTable, TotalSize);
                MainData = DataTools.OverWrite(MainData, DataTools.genOffset(OffsetSize, TotalSize), OffsetTablePos + (i * OffsetSize));
                MainData = DataTools.OverWrite(MainData, DataTools.genOffset(ResSizeOffSize, file.Length), ResSizeOffTablePos + (i * ResSizeOffSize));
                TotalSize += file.Length + (FixOffsets && i + 1 != Resources.Length ? 4 - ((StartPos + TotalSize + file.Length) % 4) : 0);
            }
            byte[] ResultPackget = new byte[MainData.Length + ResTable.Length];
            MainData.CopyTo(ResultPackget, 0);
            ResTable.CopyTo(ResultPackget, MainData.Length);
            return (CompressPackget) ? DataTools.MakeMDF(ResultPackget) : ResultPackget;
        }
        private byte[] CutAt(byte[] Original, int Pos)
        {
            byte[] rst = new byte[Pos];
            for (int i = 0; i < Pos; i++)
                rst[i] = Original[i];
            return rst;
        }
        
    }
    public class FileEntry
    {
        public byte[] Data;
    }

    public class HuffmanTool {

        public static byte[] DecompressBitmap(byte[] data) {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();

            for (int i = 0; i < data.Length;) {
                byte cmd = data[i];
                if (Repeat(cmd)) {
                    int Times = GetInt(cmd) + 3; //I Don't know the reason, but with my test in dracu-riot this value increase 3 times...
                    byte[] Data = GetDword(data, i + 1);
                    i += 5;
                    for (int count = 0; count < Times; count++)
                        stream.Write(Data, 0, Data.Length);
                } else {
                    int Length = (GetInt(cmd)+1)*4;//+1 because the first 4 bytes don't is counted in the offset... and *4 because 1 is equal a 4 bytes
                    byte[] Data = SubArray(data, i+1, Length);
                    i += Length + 1;
                    stream.Write(Data, 0, Data.Length);
                }
            }
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] outdata = new byte[stream.Length];
            stream.Read(outdata, 0, outdata.Length);
            return outdata;
        }

        public static byte[] CompressBitmap(byte[] data, bool JumpHeader) {
            if (getRange(data, 0, 2) == "BM" && JumpHeader) {
                byte[] cnt = new byte[data.Length - 0x36];
                for (int i = 0x36; i < data.Length; i++)
                    cnt[i - 0x36] = data[i];
                data = cnt;
            }
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            int MaxInt = 0x7F;
            int MinVal = 3;
            for (int pos = 0; pos < data.Length;) {
                if (HaveLoop(data, pos)) {
                    int Loops = CountLoops(data, pos);
                    byte[] DW = GetDword(data, pos);
                    while (Loops > 0) {
                        int len = (Loops - MinVal > MaxInt) ? MaxInt : Loops - MinVal;
                        Loops -= len + MinVal;
                        stream.Write(new byte[] { CreateInt(len) }, 0, 1);
                        stream.Write(DW, 0, 4);
                        pos += (len + MinVal) * 4;
                    }
                } else {
                    int len = 0;
                    while (!HaveLoop(data, pos + (len*4))) {
                        len++;
                        if (pos + (len * 4) >= data.Length)
                            break;
                    }
                    while (len > 0) {
                        int off = (len > MaxInt) ? MaxInt : len;
                        stream.Write(new byte[] { (byte)(off-1) }, 0, 1);
                        stream.Write(data, pos, off * 4);
                        len -= off;
                        pos += off * 4;
                    }
                }
            }
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] outdata = new byte[stream.Length];
            stream.Read(outdata, 0, outdata.Length);
            return outdata;
        }
        
        private static int CountLoops(byte[] data, int Pos) {
            byte[] Find = GetDword(data, Pos);
            int Loops = 0;
            while (true) {
                if (EqualsAt(data, Find, (Loops * 4) + Pos)) {
                    Loops++;
                }
                else {
                    break;
                }
            }
            return Loops;
        }

        private static bool HaveLoop(byte[] data, int pos) {
            byte[] dword = GetDword(data, pos);
            return CountLoops(data, pos) > 2;

        }
        private static byte[] GetDword(byte[] data, int pos) {
            return new byte[] { data[pos], data[pos + 1], data[pos + 2], data[pos + 3] };
        }
        private static bool EqualsAt(byte[] data, byte[] CompareData, int pos) {
            if (CompareData.Length + pos > data.Length)
                return false;
            for (int i = 0; i < CompareData.Length; i++)
                if (CompareData[i] != data[i + pos])
                    return false;
            return true;
        }

        private static string getRange(byte[] file, int pos, int length) {
            byte[] rest = new byte[length];
            for (int i = 0; i < length; i++) {
                rest[i] = file[pos + i];
            }
            return Tools.ByteArrayToString(rest).Replace("-", "");
        }

        private static byte[] SubArray(byte[] data, int Pos, int length) {
            byte[] rst = new byte[length];
            for (int i = 0; i < length; i++)
                rst[i] = data[i + Pos];
            return rst;
        }
        private static bool Repeat(byte value) {
            byte mask = 0x80;
            return ((value & mask) > 0);
        }
        private static int GetInt(byte value) {
            byte mask = 0x7F;
            return value & mask;
        }

        private static byte CreateInt(int value) {
            if (value > 0x7F)
                throw new Exception("Max Allowed value is: " + 0x7F);
            return (byte)(value | 0x80);
        }
    }
}
