using System;
using System.Collections.Generic;

namespace KrKrSceneManager {

    /// <summary>
    /// A Proxy to the StringManager that tries desort the stirng order.
    /// </summary>
    public class PSBAnalyzer {
        bool Warning = false;
        bool EmbeddedReferenced = false;

        /// <summary>
        /// Says if he analyzer found a unk opcode
        /// </summary>
        public bool UnkOpCodes {
            get {
                return Warning;
            }
        }

        /// <summary>
        /// Says if the analyzer found a Embedded content in this PSB
        /// </summary>
        public bool HaveEmbedded {
            get {
                return EmbeddedReferenced;
            }

        }

        public bool ExtendStringLimit = true;
        public bool CompressPackget = true;
        public int CompressionLevel = 9;

        uint ByteCodeStart;
        uint ByteCodeLen = 0;
        string[] Strings;
        byte[] Script;
        PSBStrMan StringManager;
        List<uint> Calls = new List<uint>();
        public PSBAnalyzer(byte[] Script) {
            var Status = PSBStrMan.GetPackgetStatus(Script);
            if (Status == PSBStrMan.PackgetStatus.MDF)
                Script = PSBStrMan.ExtractMDF(Script);
            Status = PSBStrMan.GetPackgetStatus(Script);
            if (Status != PSBStrMan.PackgetStatus.PSB)
                throw new Exception("Bad File Format");

            this.Script = new byte[Script.Length];
            Script.CopyTo(this.Script, 0);

            StringManager = new PSBStrMan(Script) {
                CompressPackage = true,
                ForceMaxOffsetLength = ExtendStringLimit
            };

            ByteCodeStart = ReadOffset(this.Script, 0x24, 4);
            ByteCodeLen   = ReadOffset(this.Script, 0x10, 4) - ByteCodeStart;

            if (ByteCodeLen + ByteCodeStart > Script.Length)
                throw new Exception("Corrupted Script");
        }

        public string[] Import() {
            EmbeddedReferenced = false;
            Warning = false;

            Calls = new List<uint>();
            Strings = StringManager.Import();
            for (uint i = ByteCodeStart; i < ByteCodeLen + ByteCodeStart; ) {
                try {
                    var Result = Analyze(Script, ref i);

                    foreach (uint ID in Result)
                        if (ID < Strings.LongLength && !Calls.Contains(ID))
                            Calls.Add(ID);
                } catch { }
            }

            //Prevent Missmatch
            for (uint i = 0; i < Strings.LongLength; i++)
                if (!Calls.Contains(i))
                    Calls.Add(i);

            return Desort(Strings, Calls.ToArray());
        }

        public byte[] Export(string[] Strings) {
            string[] Content = Sort(Strings, Calls.ToArray());

            StringManager.CompressPackage = CompressPackget;
            PSBStrMan.CompressionLevel = CompressionLevel;

            return StringManager.Export(Content);
        }

        private string[] Desort(string[] Strings, uint[] Map) {
            if (Map.Length != Strings.Length)
                throw new Exception("String Calls Count Missmatch");

            string[] Result = new string[Strings.LongLength];
            for (uint i = 0; i < Map.LongLength; i++) {
                Result[i] = Strings[Map[i]];
            }

            return Result;
        }

        private string[] Sort(string[] Strings, uint[] Map) {
            if (Map.Length != Strings.Length)
                throw new Exception("String Calls Count Missmatch");


            string[] Result = new string[Strings.LongLength];
            for (uint i = 0; i < Map.LongLength; i++) {
                Result[Map[i]] = Strings[i];
            }

            return Result;

        }

        //Here a shit sample how to detect the string order, now plz, don't send-me more emails about this.
        private uint[] Analyze(byte[] Script, ref uint Index) {
            byte Cmd = Script[Index];

            uint ID = 0;
            List<uint> IDs = new List<uint>();
            switch (Cmd) {
                //Strings - Hay \o/
                case 0x15:
                    IDs.Add(ReadOffset(Script, Index + 1, 1));
                    Index += 2;
                    break;
                case 0x16:
                    IDs.Add(ReadOffset(Script, Index + 1, 2));
                    Index += 3;
                    break;
                case 0x17:
                    IDs.Add(ReadOffset(Script, Index + 1, 3));
                    Index += 4;
                    break;
                case 0x18:
                    IDs.Add(ReadOffset(Script, Index + 1, 4));
                    Index += 5;
                    break;

                //Numbers
                case 0x4:
                case 0x5:
                case 0x6:
                case 0x7:
                case 0x8:
                case 0x9:
                case 0xA:
                case 0xB:
                case 0xC:
                    Index++;
                    Index += (uint)Cmd - 0x4;
                    break;

                case 0x1D:
                    Index++;
                    break;
                case 0x1E:
                    Index++;
                    Index += 4;
                    break;
                case 0x1F:
                    Index++;
                    Index += 8;
                    break;

                //Constants
                case 0x0:
                case 0x1:
                case 0x2:
                case 0x3:
                    Index++;
                    break;

                //Arrays
                case 0x0D:
                case 0x0E:
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                    Index++;
                    uint CLen = (uint)Cmd - 0xC;
                    uint Count = ReadOffset(Script, Index, CLen);
                    Index += CLen;

                    uint ELen = (uint)Script[Index++] - 0xC;
                    Index += ELen * Count;

                    break;

                case 0x20:
                    Index++;
                    IDs.AddRange(Analyze(Script, ref Index));
                    break;

                case 0x21:
                    Index++;
                    IDs.AddRange(Analyze(Script, ref Index));
                    IDs.AddRange(Analyze(Script, ref Index));
                    break;

                //References to the Embedded Content
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                    EmbeddedReferenced = true;
                    Index++;
                    Index += (uint)Cmd - 0x18;
                    break;

                //Fuck
                default:
                    Warning = true;
                    Index++;
                    break;
            }

            return IDs.ToArray();
        }

        internal static uint ReadOffset(byte[] Script, uint Index, uint Length) {
            byte[] Value = new byte[8];
            Array.Copy(Script, Index, Value, 0, Length);
            return (uint)BitConverter.ToUInt64(Value, 0);//.Net Only works with array of Int.MaxValue of Length;
        }
    }
}
