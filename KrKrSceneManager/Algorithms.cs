using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace KrKrSceneManager {
    internal static class Tools {
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
            byte[] Buffer = new byte[2000];
            int len;
            while ((len = input.Read(Buffer, 0, Buffer.Length)) > 0) {
                output.Write(Buffer, 0, len);
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

        public static string ReadCString(this Stream Stream, Encoding Encoding = null) {
            if (Encoding == null)
                Encoding = Encoding.UTF8;

            int LastByte = 0;
            List<byte> Buffer = new List<byte>();
            do {
                LastByte = Stream.ReadByte();
                if (LastByte < 0)
                    throw new InternalBufferOverflowException();
                if (LastByte == 0)
                    continue;
                Buffer.Add((byte)LastByte);
            } while (LastByte != 0);

            return Encoding.GetString(Buffer.ToArray());
        }
        public static void WriteCString(this Stream Stream, string String, Encoding Encoding = null)
        {
            if (Encoding == null)
                Encoding = Encoding.UTF8;
            var Buffer = Encoding.GetBytes(String + "\x0");
            Stream.WriteBytes(Buffer);
        }

        public static byte[] ReadBytes(this Stream Stream, int Count) {
            byte[] Buffer = new byte[Count];
            if (Stream.Read(Buffer, 0, Buffer.Length) != Count)
                throw new Exception("Buffer Overflow Exception");
            return Buffer;
        }
        public static void WriteBytes(this Stream Stream, byte[] Data)
        {
            Stream.Write(Data, 0, Data.Length);
        }

        public static T ReadStruct<T>(this Stream Stream) where T : struct {
            byte[] Buffer = Stream.ReadBytes(Marshal.SizeOf(typeof(T)));
            return ParseStruct<T>(Buffer);
        }

        public static void WriteStruct<T>(this Stream Stream, T Struct) where T : struct
        {
            var Data = ParseStruct(Struct);
            Stream.Write(Data, 0, Data.Length);
        }

        public static T ParseStruct<T>(this byte[] Data) where T : struct {
            var pStruct = Marshal.AllocHGlobal(Data.Length);
            Marshal.Copy(Data, 0, pStruct, Data.Length);
            T Struct = Marshal.PtrToStructure<T>(pStruct);
            Marshal.FreeHGlobal(pStruct);
            return Struct;
        }
        public static byte[] ParseStruct<T>(this T Struct) where T : struct {
            byte[] Buffer = new byte[Marshal.SizeOf(Struct)];
            var pStruct = Marshal.AllocHGlobal(Buffer.Length);
            Marshal.StructureToPtr(Struct, pStruct, true);
            Marshal.Copy(pStruct, Buffer, 0, Buffer.Length);
            Marshal.FreeHGlobal(pStruct);
            return Buffer;
        }

    }

    internal struct PSBHeader {
        internal uint Signature;
        internal uint Version;//Tested: 1, 2, 3
        uint Unk;
        uint Unk2;
        internal uint StrOffPos;
        internal uint StrDataPos;
        internal uint ResOffPos;
        internal uint ResLenPos;
        internal uint ResDataPos;
        internal uint ResIndexTree;
    }
}
