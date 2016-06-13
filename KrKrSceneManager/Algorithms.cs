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
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0) {
                output.Write(buffer, 0, len);
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
}
