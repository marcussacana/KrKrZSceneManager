using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KrKrSceneManager {
    public class PSBAnalyzer {

        public bool ExtendStringLimit = true;
        public bool CompressPackget = true;
        public int CompressionLevel = 9;

        const uint ByteCodeStart = 0x28;
        uint ByteCodeLen = 0;
        string[] Strings;
        byte[] Script;
        PSBStrMan StringManager;
        List<uint> Calls = new List<uint>();
        public PSBAnalyzer(byte[] Script) {
            this.Script = Script;
            StringManager = new PSBStrMan(Script) {
                CompressPackget = true,
                ForceMaxOffsetLength = ExtendStringLimit
            };
            if (PSBStrMan.GetPackgetStatus(Script) == PSBStrMan.PackgetStatus.MDF)
                this.Script = PSBStrMan.ExtractMDF(Script);

            ByteCodeLen = ReadOffset(this.Script, 0x10, 4) - ByteCodeStart;
        }

        public string[] Import() {
            Calls = new List<uint>();
            Strings = StringManager.Import();
            bool Recognized = true;
            for (uint i = ByteCodeStart; i < ByteCodeLen + ByteCodeStart; ) {
                long Result = TryAnaylze(Script, ref i, Recognized);
                if (Result < 0) {
                    Recognized = Result != -2;
                    continue;
                }


                if (!Calls.Contains((uint)Result) && Result < Strings.LongLength)
                    Calls.Add((uint)Result);
            }

            //Prevent Missmatch
            for (uint i = 0; i < Strings.LongLength; i++)
                if (!Calls.Contains(i))
                    Calls.Add(i);

            return Desort(Strings, Calls.ToArray());
        }

        public byte[] Export(string[] Strings) {
            string[] Content = Sort(Strings, Calls.ToArray());

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
        private long TryAnaylze(byte[] Script, ref uint Index, bool Recognized) {
            byte Cmd = Script[Index];

            long ID = -1;
            switch (Cmd) {
                //Enums
                case 0x0:
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                case 0x1D:
                    Index++;
                    return -1;

                //Unks
                default:
                    Index++;
                    return -2;
                    
                //Ints
                case 0x5:
                    Index += 2;
                    return -1;
                case 0x6:
                    Index += 3;
                    return -1;
                case 0x7:
                    Index += 4;
                    return -1;
                case 0x8:
                    if (!Recognized)
                        goto default;
                    Index += 5;
                    return -1;
                case 0x9:
                    if (!Recognized)
                        goto default;
                    Index += 6;
                    return -1;
                case 0xA:
                    if (!Recognized)
                        goto default;
                    Index += 7;
                    return -1;
                case 0xB:
                    if (!Recognized)
                        goto default;
                    Index += 8;
                    return -1;
                case 0xC:
                    if (!Recognized)
                        goto default;
                    Index += 9;
                    return -1;

                //Array
                case 0xD:
                    Index += 2;
                    return -1;
                case 0xE:
                    Index += 3;
                    return -1;
                case 0xF:
                    Index += 4;
                    return -1;
                case 0x10:
                    if (!Recognized)
                        goto default;
                    Index += 5;
                    return -1;
                case 0x11:
                    if (!Recognized)
                        goto default;
                    Index += 6;
                    return -1;
                case 0x12:
                    if (!Recognized)
                        goto default;
                    Index += 7;
                    return -1;
                case 0x13:
                    if (!Recognized)
                        goto default;
                    Index += 8;
                    return -1;
                case 0x14:
                    if (!Recognized)
                        goto default;
                    Index += 9;
                    return -1;

                //Resource
                case 0x19:
                    Index += 2;
                    return -1;
                case 0x1A:
                    Index += 3;
                    return -1;
                case 0x1B:
                    Index += 4;
                    return -1;
                case 0x1C:
                    if (!Recognized)
                        goto default;
                    Index += 5;
                    return -1;

                //Decimals
                case 0x1E:
                    if (!Recognized)
                        goto default;
                    Index += 5;
                    return -1;
                case 0x1F:
                    if (!Recognized)
                        goto default;
                    Index += 9;
                    return -1;
                

                //Strings - Hay \o/
                case 0x15:
                    if (!Recognized)
                        goto default;
                    ID = ReadOffset(Script, Index + 1, 1);
                    if (ID >= Strings.LongLength)
                        goto default;
                    Index += 2;
                    return ID;
                case 0x16:
                    if (!Recognized)
                        goto default;
                    ID = ReadOffset(Script, Index + 1, 2);
                    if (ID >= Strings.LongLength)
                        goto default;
                    Index += 3;
                    return ID;
                case 0x17:
                    if (!Recognized)
                        goto default;
                    ID = ReadOffset(Script, Index + 1, 3);
                    if (ID >= Strings.LongLength)
                        goto default;
                    Index += 4;
                    return ID;
                case 0x18:
                    if (!Recognized)
                        goto default;
                    ID = ReadOffset(Script, Index + 1, 4);
                    if (ID >= Strings.LongLength)
                        goto default;
                    Index += 5;
                    return ID;
            }
            
        }

        internal static uint ReadOffset(byte[] Script, uint Index, uint Length) {
            byte[] Value = new byte[4];
            Array.Copy(Script, Index, Value, 0, Length);
            return BitConverter.ToUInt32(Value, 0);
        }
    }
}
