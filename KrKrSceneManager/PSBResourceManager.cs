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
            StartPos = DataTools.GetOffset(packget, 0x20, 4, false);
            OffsetPos = DataTools.GetOffset(packget, 0x18, 4, false);
            int[] tmp = GetOffsetInfo(packget, OffsetPos);
            OffsetSize = tmp[0];
            OffsetTablePos = tmp[1];
            ResSizePos = DataTools.GetOffset(packget, 0x1C, 4, false);
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
            int Count = DataTools.GetOffset(file, pos + 1, OffSize, false);
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
                Result[i] = DataTools.GetOffset(file, pos + (i * OffSize), OffSize, false);
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
}
