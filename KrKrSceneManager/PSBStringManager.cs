using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using AdvancedBinary;

/*
 * KrKrSceneManager (5.6) By Marcussacana
 * Usage:
 * byte[] input = File.ReadAllBytes("C:\\sample.bin");
 * PSBStringManager StrMan = new PSBStringManager(input);
 * string[] scncontent = StrMan.Import(); 
 * ...
 * //save
 * byte[] output = StrMan.Export(scncontent);
 * File.WriteAllBytes("C:\\sample_out.bin", output);
*/

namespace KrKrSceneManager {
    public class PSBStrMan {  
        
        public bool CompressPackage = false;
        public static int CompressionLevel = 9;
        public bool ForceMaxOffsetLength = false;

        byte[] Script;
        int OffLength;
        int StrCount;
        int OldOffTblLen;//Old Offset Table Length
        int OldStrDatLen;//Old String Data Length
        PSBHeader Header;
        public PSBStrMan(byte[] Script) {
            this.Script = new byte[Script.Length];
            Script.CopyTo(this.Script, 0);
        }
        public string[] Import() {
            PackgetStatus Status = GetPackgetStatus(Script);
            switch (Status) {
                case PackgetStatus.Invalid:
                    throw new Exception("Invalid Package");
                case PackgetStatus.MDF:
                    Script = ExtractMDF(Script);
                    CompressPackage = true;
                    break;
            }

            MemoryStream Reader = new MemoryStream(Script);
            Header = new PSBHeader();
            Header = Reader.ReadStruct<PSBHeader>();

            Reader.Position = Header.StrOffPos;
            OffLength = ConvertSize((byte)Reader.ReadByte());
            StrCount = ReadOffset(Reader.ReadBytes(OffLength), 0, OffLength);
            OffLength = ConvertSize((byte)Reader.ReadByte());

            int[] Offsets = new int[StrCount];
            for (int i = 0; i < StrCount; i++)
                Offsets[i] = ReadOffset(Reader.ReadBytes(OffLength), 0, OffLength);

            OldOffTblLen = (int)(Reader.Position - Header.StrOffPos);

            string[] Strings = new string[StrCount];
            for (int i = 0; i < StrCount; i++) {
                Reader.Position = Header.StrDataPos + Offsets[i];
                Strings[i] = Reader.ReadCString();
            }

            OldStrDatLen = (int)(Reader.Position - Header.StrDataPos);

            Reader.Close();
            return Strings;
        }
        public byte[] Export(string[] Strings) {
            if (Strings.Length != StrCount)
                throw new Exception("You can't add or remove a string entry");
			
			int[] Offsets;
			byte[] StringData, OffsetData;
            BuildStringData(Strings, out StringData, out Offsets);
            BuildOffsetTable(Offsets, out OffsetData);

            int OffTblDiff = OffsetData.Length - OldOffTblLen;
            int StrDatDiff = StringData.Length - OldStrDatLen;
            PSBHeader Header = this.Header;
            
            UpdateOffsets(ref Header, OffTblDiff, StrDatDiff);

            byte[] OutScript = new byte[Script.Length];
            Script.CopyTo(OutScript, 0);

            OverwriteRange(ref OutScript, Header.StrOffPos, OldOffTblLen, OffsetData);
            OverwriteRange(ref OutScript, Header.StrDataPos, OldStrDatLen, StringData);

            Header.ParseStruct().CopyTo(OutScript, 0);

            return CompressPackage ? CompressMDF(OutScript) : OutScript;
        }

        private void OverwriteRange(ref byte[] OriginalData, uint Start, int Length, byte[] DataToOverwrite) {
            byte[] First = new byte[Start];
            Array.Copy(OriginalData, First, Start);

            byte[] Second = new byte[OriginalData.Length - (Start + Length)];
            Array.Copy(OriginalData, Start + Length, Second, 0, Second.Length);

            OriginalData = new byte[First.Length + DataToOverwrite.Length + Second.Length];
            First.CopyTo(OriginalData, 0);
            DataToOverwrite.CopyTo(OriginalData, First.Length);
            Second.CopyTo(OriginalData, First.Length + DataToOverwrite.Length);
        }

        void UpdateOffsets(ref PSBHeader Header, int OffTblDiff, int StrDatDiff) {
            UpdateOffset(ref Header.ResOffPos, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.ResDataPos, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.ResLenPos, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.ResIndexTree, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.StrDataPos, Header.StrOffPos, OffTblDiff);

            UpdateOffset(ref Header.ResOffPos, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.ResDataPos, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.ResLenPos, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.ResIndexTree, Header.StrDataPos, StrDatDiff);
        }

        void UpdateOffset(ref uint Offset, uint ChangeBaseOffset, int Diff) {
            if (Offset < ChangeBaseOffset)
                return;
            Offset = (uint)(Offset + Diff);
        }

        void BuildStringData(string[] Strings, out byte[] StringTable, out int[] Offsets) {
            Offsets = new int[StrCount];
            MemoryStream Writer = new MemoryStream();

            for (int i = 0; i < StrCount; i++) {
                Offsets[i] = (int)Writer.Length;
                Writer.WriteCString(Strings[i]);
            }
            StringTable = Writer.ToArray();
            Writer.Close();
        }

        void BuildOffsetTable(int[] Offsets, out byte[] OffsetData) {
            MemoryStream OffData = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(OffData);

            //Offset Count
            int OffsetSize = ForceMaxOffsetLength ? 4 : GetMinIntLen(StrCount);
            Writer.Write(ConvertSize(OffsetSize));
            Writer.Write(CreateOffset(OffsetSize, StrCount));

            //Offset's Size
            OffsetSize = ForceMaxOffsetLength ? 4 : GetMinIntLen(Offsets[StrCount - 1]);
            Writer.Write(ConvertSize(OffsetSize));

            for (int i = 0; i < StrCount; i++)
                Writer.Write(CreateOffset(OffsetSize, Offsets[i]));

            OffsetData = OffData.ToArray();
            Writer.Close();
            OffData?.Close();
        }

        internal static byte[] CompressMDF(byte[] Psb) {
            byte[] CompressedScript;
            Tools.CompressData(Psb, CompressionLevel, out CompressedScript);

            byte[] RetData = new byte[8 + CompressedScript.Length];
            (new byte[] { 0x6D, 0x64, 0x66, 0x00 }).CopyTo(RetData, 0);//Signature
            CreateOffset(4, Psb.Length).CopyTo(RetData, 4);//Decompressed Length
            CompressedScript.CopyTo(RetData, 8);//ZLIB Data

            return RetData;
        }

        public byte[] TryRecovery()
        {
            var Script = new byte[this.Script.Length];
            this.Script.CopyTo(Script, 0);

            var Status = GetPackgetStatus(Script);
            if (Status == PackgetStatus.Invalid)
                throw new Exception("Invalid Package");

            bool MDF = Status == PackgetStatus.MDF;
            if (MDF)
                Script = ExtractMDF(Script);

            int StrOff = ReadOffset(Script, 0x10, 4);
            int StrData = ReadOffset(Script, 0x14, 4);
            int CntSize = ConvertSize(Script[StrOff]);
            int Count = ReadOffset(Script, StrOff + 1, CntSize);
            int Size = ConvertSize(Script[StrOff + 1 + CntSize]);
            int EndStr = (StrOff + 2 + CntSize) + ((Count - 1) * Size);
            EndStr = ReadOffset(Script, EndStr, Size) + StrData;
            while (Script[EndStr] != 0x00)
                EndStr++;
            byte[] Seq = new byte[] { 0xD, 0x0, 0xD };
            if (EqualsAt(Script, Seq, EndStr + 1) && EqualsAt(Script, Seq, EndStr + 1 + Seq.Length)) {
                OverwriteRange(ref Script, 0x18, 4, CreateOffset(4, EndStr + 1));
                OverwriteRange(ref Script, 0x1C, 4, CreateOffset(4, EndStr + 4));//+3
                OverwriteRange(ref Script,  0x20, 4, CreateOffset(4, EndStr + 7));//+6
                return MDF ? CompressMDF(Script) : Script;
            } else {
                try {
                    int tmp = ConvertSize(Script[ReadOffset(Script, 0x18, 4)]);
                    tmp = ConvertSize(Script[ReadOffset(Script, 0x1C, 4)]);
                    return MDF ? CompressMDF(Script) : Script; //Looks all Rigth
                }
                catch {
                    throw new Exception("You Can't Recovery because this package contains data.");
                }
            }
        }
        bool EqualsAt(byte[] Data, byte[] CompareData, int Pos) {
            if (CompareData.Length + Pos > Data.Length)
                return false;
            for (int i = 0; i < CompareData.Length; i++)
                if (Data[i + Pos] != CompareData[i])
                    return false;
            return true;
        }

        private static int GetMinIntLen(int Value) {
            int MinLen = 0;
            while (Value >> (MinLen * 8) > 0)
                MinLen++;
            return MinLen;
        }
        internal static byte[] CreateOffset(int Length, int Value) {
            byte[] Buffer = new byte[GetMinIntLen(Value)];
            Array.Copy(BitConverter.GetBytes(Value), Buffer, Buffer.Length);


            if (Buffer.Length > Length)
                throw new Exception("Edited Strings are too big.");
            if (Buffer.Length < Length)
                Array.Resize(ref Buffer, Length);
            return Buffer;
        }
        
        internal static byte ConvertSize(int s) {
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
            throw new Exception("Unknown Offset Size ("+s+")");
        }
        internal static int ConvertSize(byte b) {
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
            throw new Exception("Unknown Offset Size (" + b.ToString("X2") + ")");
        }
        public static byte[] ExtractMDF(byte[] MDF) {
            byte[] Zlib = new byte[MDF.Length - 8];
            Array.Copy(MDF, 8, Zlib, 0, MDF.Length - 8);

            byte[] PSB;
            Tools.DecompressData(Zlib, out PSB);
            if (ReadOffset(MDF, 4, 4) != PSB.Length)
                throw new Exception("Corrupted MDF Header or Zlib Data");

            return PSB;
        }
        public enum PackgetStatus {
            MDF, PSB, Invalid
        }
        public static PackgetStatus GetPackgetStatus(byte[] Packget) {
            if (ReadOffset(Packget, 0, 3) == 0x66646D)
                return PackgetStatus.MDF;
            if (ReadOffset(Packget, 0, 3) == 0x425350)
                return PackgetStatus.PSB;
            return PackgetStatus.Invalid;
        }        
        private static string ReadString(byte[] Script, int pos) {          
            List<byte> Array = new List<byte>();
            while (Script[pos] != 0x00)
                Array.Add(Script[pos++]);

            return Encoding.UTF8.GetString(Array.ToArray());
        }

        internal static int ReadOffset(byte[] Script, int Index, int Length) {
            byte[] Value = new byte[4];
            Array.Copy(Script, Index, Value, 0, Length);
            return BitConverter.ToInt32(Value, 0);
        }
    }
}

