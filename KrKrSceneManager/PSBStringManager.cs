using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
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
        
        public bool CompressPackget = false;
        public static int CompressionLevel = 9;
        public bool ForceMaxOffsetLength = false;

        private byte[] Script;
        private int OffLength;
        private int StrCount;
        private int OldOffTblLen;//Old Offset Table Length
        private int OldStrDatLen;//Old String Data Length
        private PSBHeader Header;
        public PSBStrMan(byte[] Script) {
            this.Script = Script;
        }
        public string[] Import() {
            PackgetStatus Status = GetPackgetStatus(Script);
            switch (Status) {
                case PackgetStatus.Invalid:
                    throw new Exception("Invalid Packget");
                case PackgetStatus.MDF:
                    Script = ExtractMDF(Script);
                    CompressPackget = true;
                    break;
            }

            MemoryStream In = new MemoryStream(Script);
            Header = new PSBHeader();
            StructReader Reader = new StructReader(In);
            Reader.ReadStruct(ref Header);

            Reader.BaseStream.Position = Header.StrOffPos;
            OffLength = ConvertSize(Reader.ReadByte());
            StrCount = ReadOffset(Reader.ReadBytes(OffLength), 0, OffLength);
            OffLength = ConvertSize(Reader.ReadByte());

            int[] Offsets = new int[StrCount];
            for (int i = 0; i < StrCount; i++)
                Offsets[i] = ReadOffset(Reader.ReadBytes(OffLength), 0, OffLength);

            OldOffTblLen = (int)(Reader.BaseStream.Position - Header.StrOffPos);

            string[] Strings = new string[StrCount];
            for (int i = 0; i < StrCount; i++) {
                Reader.BaseStream.Position = Header.StrDataPos + Offsets[i];
                StrEntry Entry = new StrEntry();
                Reader.ReadStruct(ref Entry);
                Strings[i] = Entry.Content;
            }

            OldStrDatLen = (int)(Reader.BaseStream.Position - Header.StrDataPos);

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
            PSBHeader Header = new PSBHeader();
            AdvancedBinary.Tools.CopyStruct(this.Header, ref Header);
            
            UpdateOffsets(ref Header, OffTblDiff, StrDatDiff);

            byte[] OutScript = new byte[Script.Length];
            Script.CopyTo(OutScript, 0);

            OverwriteRange(ref OutScript, this.Header.StrOffPos, OldOffTblLen, OffsetData);
            OverwriteRange(ref OutScript, Header.StrDataPos, OldStrDatLen, StringData);

            AdvancedBinary.Tools.BuildStruct(ref Header).CopyTo(OutScript, 0);

            return CompressPackget ? CompressMDF(OutScript) : OutScript;
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

        private void UpdateOffsets(ref PSBHeader Header, int OffTblDiff, int StrDatDiff) {
            UpdateOffset(ref Header.ResOffPos, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.ResDataPos, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.ResLenPos, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.ResIndexTree, Header.StrOffPos, OffTblDiff);
            UpdateOffset(ref Header.StrDataPos, Header.StrOffPos, OffTblDiff);

            UpdateOffset(ref Header.ResOffPos, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.ResDataPos, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.ResLenPos, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.ResIndexTree, Header.StrDataPos, StrDatDiff);
            UpdateOffset(ref Header.StrOffPos, Header.StrDataPos, StrDatDiff);
        }

        private void UpdateOffset(ref uint Offset, uint ChangeBaseOffset, int Diff) {
            if (Offset < ChangeBaseOffset)
                return;
            Offset = (uint)(Offset + Diff);
        }

        private void BuildStringData(string[] Strings, out byte[] StringTable, out int[] Offsets) {
            Offsets = new int[StrCount];
            MemoryStream StrData = new MemoryStream();
            StructWriter Writer = new StructWriter(StrData);

            for (int i = 0; i < StrCount; i++) {
                Offsets[i] = (int)Writer.BaseStream.Length;
                StrEntry Entry = new StrEntry();
                Entry.Content = Strings[i];
                Writer.WriteStruct(ref Entry);
            }
            StringTable = StrData.ToArray();
            Writer.Close();
            StrData?.Close();
        }

        private void BuildOffsetTable(int[] Offsets, out byte[] OffsetData) {
            MemoryStream OffData = new MemoryStream();
            StructWriter Writer = new StructWriter(OffData);

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


        private bool EqualsAt(byte[] data, byte[] CompareData, int pos) {
            if (CompareData.Length + pos > data.Length)
                return false;
            for (int i = 0; i < CompareData.Length; i++)
                if (data[i + pos] != CompareData[i])
                    return false;
            return true;
        }

        internal static byte[] CompressMDF(byte[] psb) {
            byte[] CompressedScript;
            Tools.CompressData(psb, CompressionLevel, out CompressedScript);

            byte[] RetData = new byte[8 + CompressedScript.Length];
            (new byte[] { 0x6D, 0x64, 0x66, 0x00 }).CopyTo(RetData, 0);//Signature
            CreateOffset(4, psb.Length).CopyTo(RetData, 4);//Decompressed Length
            CompressedScript.CopyTo(RetData, 8);//ZLIB Data

            return RetData;
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
            throw new Exception("Unknow Offset Size");
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
            throw new Exception("Unknow Offset Size");
        }
        internal static byte[] ExtractMDF(byte[] MDF) {
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

