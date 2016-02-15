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
        private int Header;
        private bool Initialized;
        private int OffsetPos;
        private int OffsetSize;
        private int OffsetTablePos;
        private int StartPos;

        public bool CompressPackget = false;
        public int CompressionLevel = 9;
        public FileEntry[] Import(byte[] script)
        {
            if (DataTools.getRange(script, 0, 4) == "6D646600")
                script = DataTools.GetMDF(script);
            if (DataTools.getRange(script, 0, 4) != "50534200")
                throw new Exception("Bad File Format");
            packget = script;
            StartPos = DataTools.GetOffset(packget, 0x20, 4, false);
            OffsetPos = DataTools.GetOffset(packget, 0x18, 4, false);
            Header = DataTools.ConvertSize(packget[OffsetPos]);
            EntryCount = DataTools.GetOffset(packget, OffsetPos + 1, Header, false);
            OffsetSize = DataTools.ConvertSize(packget[OffsetPos + Header + 1]);
            OffsetTablePos = OffsetPos + Header + 2;
            int[] Offsets = new int[EntryCount];
            for (int i = 0; i < EntryCount; i++)
                Offsets[i] = DataTools.GetOffset(packget, OffsetTablePos + (i * OffsetSize), OffsetSize, false);
            FileEntry[] Files = new FileEntry[EntryCount];
            for (int i = 0; i < EntryCount; i++)
            {
                //if is last file, he ends in last byte of the psb, if not, he ends before start next file.
                int EndPos = (i == EntryCount - 1) ? packget.Length - StartPos : Offsets[i + 1]; 
                byte[] data = new byte[EndPos - Offsets[i]];
                for (int ind = Offsets[i]; ind < EndPos; ind++)
                    data[ind - Offsets[i]] = packget[ind + StartPos];
                Files[i] = new FileEntry() { Data = data };
            }
            Initialized = true;
            return Files;
        }

        public byte[] Export(FileEntry[] Resources)
        {
            if (!Initialized)
                throw new Exception("You Need Import Before Export");
            if (Resources.Length != EntryCount)
                throw new Exception("You Can't Add or Delete Resources.");
            int TotalSize = 0;
            int[] Offsets = new int[EntryCount];
            for (int i = 0; i < EntryCount; i++) {
                Offsets[i] = TotalSize;
                TotalSize += Resources[i].Data.Length;
            }
            byte[] ResTable = new byte[TotalSize];
            for (int i = 0; i < EntryCount; i++) {
                byte[] File = Resources[i].Data;
                int Pos = Offsets[i];
                for (int ind = 0; ind < File.Length; ind++) {
                    ResTable[Pos + ind] = File[ind];
                }
            }
            byte[] ResultFile = new byte[packget.Length];
            packget.CopyTo(ResultFile, 0);
            for (int i = 0; i < EntryCount; i++) {
                byte[] Offset = DataTools.genOffset(OffsetSize, Offsets[i]);
                ResultFile = OverWrite(ResultFile, Offset, OffsetTablePos + (i * OffsetSize));
            }
            ResultFile = CutAt(ResultFile, StartPos);
            byte[] ResultPackget = new byte[ResultFile.Length + ResTable.Length];
            ResultFile.CopyTo(ResultPackget, 0);
            ResTable.CopyTo(ResultPackget, ResultFile.Length);
            return (CompressPackget) ? DataTools.MakeMDF(ResultPackget) : ResultPackget;
        }
        private byte[] CutAt(byte[] Original, int Pos) {
            byte[] rst = new byte[Pos];
            for (int i = 0; i < Pos; i++)
                rst[i] = Original[i];
            return rst;
        }
        private byte[] OverWrite(byte[] Main, byte[] NewData, int Postion) {
            for (int i = 0; i < NewData.Length; i++) {
                Main[Postion + i] = NewData[i];
            }
            return Main;
        }
    }
    public class FileEntry
    {
        public byte[] Data;
    }
}
