using System;
using System.Text;

namespace KrKrSceneManager {
    public static class TJS2SManager {
        public static Sector[] Split(byte[] TJS2) {
            uint TJS2Len = Commom.GetDW(TJS2, 0x08);
            if (TJS2Len != TJS2.Length)
                throw new Exception("Corrupted File");
            uint Pointer = 0x0C;
            //First, Data Sector
            Sector Data = new Sector(TJS2, Pointer);
            Pointer += Data.FullLength;

            Sector[] Unk = new Sector[Commom.GetDW(TJS2, Pointer)];//i assume is another sector... 
            Pointer += 4;
            for (int i = 0; i < Unk.Length; i++) {
                Sector sector = new Sector(TJS2, Pointer);
                Pointer += sector.FullLength;
                Unk[i] = sector;
            }

            Sector[] TJS = new Sector[Commom.GetDW(TJS2, Pointer)];
            Pointer += 4;
            for (int i = 0; i < TJS.Length; i++) {
                Sector sector = new Sector(TJS2, Pointer);
                Pointer += sector.FullLength;
                TJS[i] = sector;
            }

            //Merge Results
            Sector[] Sectors = new Sector[Unk.Length + TJS.Length + 1];//+1 = DATA Sector
            Sectors[0] = Data;
            Unk.CopyTo(Sectors, 1);
            TJS.CopyTo(Sectors, Unk.Length + 1);
            return Sectors;
        }

        public static byte[] Merge(Sector[] Sectors) {
            byte[] Header = new byte[] { 0x54, 0x4A, 0x53, 0x32, 0x31, 0x30, 0x30, 0x00 }; //Signature + Version

            //Diff all Sectors
            Sector DATA = null;
            Sector[] Other = new Sector[0];
            Sector[] TJS = new Sector[0];
            foreach (Sector sec in Sectors)
                switch (sec.SectorType) {
                    case SectorType.DATA:
                        DATA = sec;
                        break;
                    case SectorType.Other:
                        Append(ref Other, sec);
                        break;
                    case SectorType.TJS2:
                        Append(ref TJS, sec);
                        break;
                }

            //Data Segment and Header
            byte[] OutTJS2 = new byte[Header.Length + 4];
            Header.CopyTo(OutTJS2, 0);
            Append(ref OutTJS2, DATA.Generate());

            //Others Segments
            Append(ref OutTJS2, Commom.GenDW((uint)Other.Length));
            foreach (Sector sec in Other)
                Append(ref OutTJS2, sec.Generate());
            
            //TJS2 Segments
            Append(ref OutTJS2, Commom.GenDW((uint)TJS.Length));
            foreach (Sector sec in TJS)
                Append(ref OutTJS2, sec.Generate());
            
            //Last Header Information, File Length
            Commom.GenDW((uint)OutTJS2.Length).CopyTo(OutTJS2, 0x08);

            return OutTJS2;
        }

        public static string[] GetContent(Sector sector) {
            if (sector.Type != "DATA")
                throw new Exception("Sector Type Not Supported");
            byte[] Data = sector.Content;

            uint StrPos;
            FindStringPos(out StrPos, Data);

            string[] Strings = new string[Commom.GetDW(Data, StrPos)];
            StrPos += 4;
            for (int i = 0; i < Strings.Length; i++) {
                uint StringLength = Commom.GetDW(Data, StrPos) * 2;
                StrPos += 4;
                Strings[i] = Encoding.Unicode.GetString(Data, (int)StrPos, (int)StringLength);//Fuck the uint, if you need you copy to new array
                StrPos += StringLength;
                StrPos = Round(StrPos, 4);
            }
            return Strings;
        }

        public static void SetContent(ref Sector sector, string[] Strings) {
            if (sector.Type != "DATA")
                throw new Exception("Sector Type Not Supported");
            byte[] Data = sector.Content;

            //Load Positions
            uint StrPos;
            FindStringPos(out StrPos, Data);
            uint EndPos = FindStrEnd(StrPos, Data);

            //Copy Int Non-String Data
            byte[] Values = new byte[StrPos];
            Array.Copy(Data, 0, Values, 0, Values.Length);

            byte[] StringTable = new byte[4];
            Commom.GenDW((uint)Strings.Length).CopyTo(StringTable, 0);//String Count

            //Generate String Table
            foreach (string String in Strings) {
                uint StrLen = Round((uint)String.Length * 2, 4);
                byte[] StrEntry = new byte[StrLen + 4];
                Commom.GenDW((uint)String.Length).CopyTo(StrEntry, 0);
                Encoding.Unicode.GetBytes(String).CopyTo(StrEntry, 4);
                Append(ref StringTable, StrEntry);
            }

            //Copy Object Values
            byte[] OBJS = new byte[Data.Length - EndPos];
            Array.Copy(Data, EndPos, OBJS, 0, OBJS.Length);

            //Generate Sector Content
            byte[] NewContent = new byte[Values.Length + StringTable.Length + OBJS.Length];
            Values.CopyTo(NewContent, 0);
            StringTable.CopyTo(NewContent, Values.Length);
            OBJS.CopyTo(NewContent, Values.Length + StringTable.Length);

            //Return
            sector.Content = NewContent;
        }

        private static void Append(ref byte[] DataTable, byte[] DataToAppend) {
            byte[] Out = new byte[DataTable.Length + DataToAppend.Length];
            DataTable.CopyTo(Out, 0);
            DataToAppend.CopyTo(Out, DataTable.Length);
            DataTable = Out;
        }

        private static void Append(ref Sector[] DataTable, Sector SectorToAppend) {
            Sector[] Out = new Sector[DataTable.Length + 1];
            DataTable.CopyTo(Out, 0);
            Out[DataTable.Length] = SectorToAppend; 
            DataTable = Out;
        }
        private static uint FindStrEnd(uint StrPos, byte[] Data) {
            uint Count = Commom.GetDW(Data, StrPos);
            StrPos += 4;
            for (uint i = 0; i < Count; i++) {
                uint len = Commom.GetDW(Data, StrPos);
                StrPos += 4 + Round(len * 2, 4);
            }
            return StrPos;
        }
        private static void FindStringPos(out uint StrPos, byte[] Data) {
            StrPos = Commom.GetDW(Data, 0) + 4; //Skip 8 Bits Array
            StrPos = Round(StrPos, 4);

            StrPos += (Commom.GetDW(Data, StrPos) * 2) + 4; //Skip 16 Bits Array
            StrPos = Round(StrPos, 4);

            StrPos += (Commom.GetDW(Data, StrPos) * 4) + 4; //Skip 32 Bits Array
            StrPos = Round(StrPos, 4);

            StrPos += (Commom.GetDW(Data, StrPos) * 8) + 4; //Skip 64 Bits Array
            StrPos = Round(StrPos, 4);

            StrPos += (Commom.GetDW(Data, StrPos) * 8) + 4; //Skip 64 Bits wtf (IEEE Float?)
            StrPos = Round(StrPos, 4);
        }

        private static uint Round(uint Value, uint Multiplier) {
            while (Value % Multiplier != 0)
                Value++;
            return Value;
        }
    }

    public class Sector {
        internal string Type = string.Empty;
        public SectorType SectorType { get {
                switch (Type) {
                    case "DATA":
                        return SectorType.DATA;
                    case "TJS2":
                        return SectorType.TJS2;
                    default:
                        return SectorType.Other;
                }
            }
        }

        internal byte[] Content;

        internal uint FullLength { get { return (uint)(Content.Length + 8L); } }

        internal Sector(byte[] Data, uint SectorPos) {
            //Get Signature
            Type = Encoding.ASCII.GetString(Data, (int)SectorPos, 0x04);

            //Get Content
            Content = new byte[Commom.GetDW(Data, SectorPos + 4)];
            Array.Copy(Data, SectorPos + 8, Content, 0, Content.Length);
        }

        internal byte[] Generate() {
            //Create new Variable
            byte[] sector = new byte[FullLength];

            //Write Signature
            Encoding.ASCII.GetBytes(Type).CopyTo(sector, 0);

            //Write Header (Content Length)
            Commom.GenDW((uint)Content.Length).CopyTo(sector, 0x04);

            //Write Content
            Array.Copy(Content, 0, sector, 0x08, Content.Length);

            //Return
            return sector;
        }
    }

    public enum SectorType { DATA, TJS2, Other}
    internal static class Commom {
        internal static uint GetDW(byte[] data, uint pos) {
            byte[] DW = new byte[4];
            Array.Copy(data, pos, DW, 0, DW.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(DW, 0, DW.Length);
            uint val = BitConverter.ToUInt32(DW, 0);
            return val;
        }

        internal static byte[] GenDW(uint value) {
            byte[] DW = BitConverter.GetBytes(value);
            if (DW.Length != 4)
                throw new Exception("WTF");
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(DW, 0, DW.Length);
            return DW;
        }
    }
     
}
