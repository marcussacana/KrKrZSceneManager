using System.IO;

namespace KrKrSceneManager {
    internal class Tools {
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
        

    }

    internal struct StrEntry {
        [AdvancedBinary.CString]
        internal string Content;
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
