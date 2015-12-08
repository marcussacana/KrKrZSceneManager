using System;
using System.Text;
using System.IO;

namespace ComponentAce.Compression.Libs.ZLib
{
    /// <summary>
    /// Compression directions: compression or decompression. This enumeration is used to specify the direction of compression for the <see cref="ZLibStream" />.
    /// </summary>
    public enum CompressionDirection
    {
        /// <summary>
        /// The <c>CompressionDirection.Compression</c> item means compression of data
        /// </summary>
        Compression,
        /// <summary>
        /// The <c>CompressionDirection.Decompression</c> item means decompression of data
        /// </summary>
        Decompression
    }


    /// <summary>
    /// <para>This class represents the Deflate algorithm, an industry standard algorithm for lossless file compression and decompression. It uses a combination of the LZ77 algorithm and Huffman coding. Data can be produced or consumed, even for an arbitrarily long, sequentially presented input data stream, using only previously bound amount of intermediate storage. The format can be implemented readily in a manner not covered by patents. For more information, see RFC 1951. <see href="http://go.microsoft.com/fwlink/?linkid=45286">DEFLATE Compressed Data Format Specification version 1.3.</see></para>
    /// <para>The compression functionality in ZLibStream is exposed as a stream. Data is read in on a byte-by-byte basis, so it is not possible to perform multiple passes to determine the best method for compressing entire files or large blocks of data.</para>
    /// </summary>
    /// <example> Sample code to compress data
    /// <code>
    /// [C#]
    /// public class Test
    /// {
    ///    public static void Main()
    ///    {
    ///         /* Open the file containing source data */
    ///         FileStream sourceStream = new FileStream(@"c:\data\sourceFile.dat", FileMode.Open);
    ///         /* Create an output stream to store compressed data */
    ///         FileStream targetStream = new FileStream(@"c:\data\compressedFile.dat", FileMode.CreateNew);
    ///         /* Create a ZLibStream for compression of data containing in the sourceStream */
    ///         ZLibStream compressionStream = new ZLibStream(targetStream, CompressionDirection.Compression, false);
    ///         /* Create a buffer */
    ///         byte[] buffer = new byte[2000];
    ///         int len;
    ///         /* Read source data */
    ///         while ((len = sourceStream.Read(buffer, 0, 2000)) > 0)
    ///         {
    ///             /* Compress the source data and write compressed data to the targetStream */
    ///             compressionStream.Write(buffer, 0, len);
    ///         }
    ///         /* Close streams */
    ///         sourceStream.Close();
    ///         compressionStream.Close();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <example> Sample code to decompress data
    /// <code>
    /// [C#]
    /// public class Test
    /// {
    ///    public static void Main()
    ///    {
    ///         /* Open the file containing compressed data */
    ///         FileStream sourceStream = new FileStream(@"c:\data\compressedFile.dat", FileMode.Open);
    ///         /* Create a ZLibStream for decompression of data containing from the sourceStream */
    ///         ZLibStream decompressionStream = new ZLibStream(sourceStream, CompressionDirection.Decompression, false);
    ///         /* Create an output stream to store decompressed data */
    ///         FileStream targetStream = new FileStream(@"c:\data\decompressedFile.dat", FileMode.CreateNew);
    ///         /* Create a buffer */
    ///         byte[] buffer = new byte[2000];
    ///         int len;
    ///         /* Read data from the decompression stream */
    ///         while ((len = decompressionStream.Read(buffer, 0, 2000)) > 0)
    ///         {
    ///             /* Write decompressed data to the output stream */
    ///             targetStream.Write(buffer, 0, len);
    ///         }
    ///         /* Close streams */
    ///         targetStream.Close();
    ///         decompressionStream.Close();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ZInputStream"/>
    /// <seealso cref="ZOutputStream"/>
    public sealed class ZLibStream : Stream
    {
        #region Fields

        /// <summary>
        /// Whether the stream is used for compression or it is used for decompression
        /// </summary>
        private CompressionDirection compressionDirection;

        /// <summary>
        /// A stream which will be compressed or decompressed
        /// </summary>
        private Stream _stream;

        /// <summary>
        /// Stream that will be used for decompression
        /// </summary>
        private ZInputStream decompressionStream = null;
        
        /// <summary>
        /// Stream that will be used for compression
        /// </summary>
        private ZOutputStream compressionStream = null;

        /// <summary>
        /// Compression level
        /// </summary>
        private int compLevel = (int)ZLibCompressionLevel.Z_DEFAULT_COMPRESSION;

        /// <summary>
        /// True if we need to leave the underlying stream open when closing the current stream
        /// </summary>
        private bool leaveOpen = true;

        #endregion

        #region Implementation of the abstact class

        /// <summary>
        /// CanRead returns <c>false</c> if the current stream is a compression stream. If the current stream is a decompression stream then the property returns the value of the <see cref ="Stream.CanRead" /> property of the underlying stream
        /// </summary>
        public override bool CanRead
        {
            get 
            {
                if (this.compressionDirection == CompressionDirection.Decompression)
                    return this._stream.CanRead;
                return false;
            }
        }

        /// <summary>
        /// <see cref="ZLibStream" /> doesn't support the <see cref="Stream.Seek" /> operation and thus this property always returns <c>false</c>
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// The property returns <c>false</c> if the current stream is a decompression stream and the value of the <see cref="Stream.CanWrite" /> property of the underlying stream in case of the stream is a compression stream
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                if (this.compressionDirection == CompressionDirection.Compression)
                    return this._stream.CanWrite;
                return false;
            }
        }

        /// <summary>
        /// If the current stream was created as a compression stream (<see cref="ZLibStream(Stream,CompressionDirection,bool)" />) this method calls the <see cref="Stream.Flush" /> method for the underlying stream. If the current stream is a decompression stream this method does nothing.
        /// </summary>
        public override void Flush()
        {
            if (this.compressionDirection == CompressionDirection.Compression)
                this._stream.Flush();
        }

        /// <summary>
        /// ZLibStream doesn't support the <see cref="Stream.Length">Length</see> property and thus the <see cref="NotSupportedException" /> is always thrown.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException("ZLibStream doesn't support the Length property");
            }
        }

        /// <summary>
        /// ZLibStream doesn't allow you to get or set the position in the stream and thus the <see cref="NotSupportedException" /> is always thrown
        /// </summary>
        public override long Position
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        /// <summary>
        /// <para>The <c>Read</c> method allows you to read data from the <c>ZLibStream</c>. The method is supported for a decompression stream only (<see cref="ZLibStream(Stream,CompressionDirection,bool)" />).</para>
        /// <para>If the current stream is a decompression stream this method reads a block of data from the underlying stream (the one passed to the <see cref="ZLibStream(Stream,CompressionDirection,bool)" /> constructor), decompresses it using the inflate algorithm and returns decompressed data block.</para>
        /// </summary>
        /// <remarks>
        /// <para>When calling for a compression stream this method throws the <cref see="NotSupportedException" /> exception.</para>
        /// <para>Use the <see cref="CanRead" /> property to determine whether the current instance supports reading.</para>
        /// </remarks>
        /// <param name="buffer">An array of byte in which we want to decompress data. When this method returns the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the decompressed data bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the decompressed data.</param>
        /// <param name="count">The maximum number of bytes to read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <example>This example shows how to decompress data with the ZLibStream.Read method:
        /// <code>
        /// public class Test
        /// {
        ///    public static void Main()
        ///    {
        ///         /* Open the file containing compressed data */
        ///         FileStream sourceStream = new FileStream(@"c:\data\compressedFile.dat", FileMode.Open);
        ///         /* Create a ZLibStream for decompression of data containing in the sourceStream */
        ///         ZLibStream decompressionStream = new ZLibStream(sourceStream, CompressionDirection.Decompression, false);
        ///         /* Create an output stream to store decompressed data */
        ///         FileStream targetStream = new FileStream(@"c:\data\decompressedFile.dat", FileMode.CreateNew);
        ///         /* Create a buffer */
        ///         byte[] buffer = new byte[2000];
        ///         int len;
        ///         /* Read data from the decompression stream */
        ///         while ((len = decompressionStream.Read(buffer, 0, 2000)) > 0)
        ///         {
        ///             /* Write decompressed data to the output stream */
        ///             targetStream.Write(buffer, 0, len);
        ///         }
        ///         /* Close streams */
        ///         targetStream.Close();
        ///         decompressionStream.Close();
        ///     }
        /// }
        /// </code>
        /// </example>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.compressionDirection == CompressionDirection.Compression)
                throw new NotSupportedException("You cannot read from the compression stream");
            return this.decompressionStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// ZLibStream doesn't support the <see cref="Stream.Seek">seeking</see> and thus the <see cref="NotSupportedException" /> is always thrown.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("ZLibStream doesn't support the Seek operation");
        }

        /// <summary>
        /// ZLibStream doesn't support the <see cref="Stream.SetLength">SetLength</see> property and thus the <see cref="NotSupportedException" /> is always thrown.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("ZLibStream doesn't support the SetLength operation");
        }

        /// <summary>
        /// <para>The <c>Write</c> method allows you to write data to the <c>ZLibStream</c>. The method is supported for a compression stream only (<see cref="ZLibStream(Stream,CompressionDirection,bool)" />).</para>
        /// <para>If the current stream is a compression stream this method reads a block of data from the <paramref name="buffer" />, compresses it and writes to the underlying stream (the one passed to the <see cref="ZLibStream(Stream,CompressionDirection,bool)" /> constructor).</para>
        /// </summary>
        /// <remarks>
        /// <para>When calling for a decompression stream this method throws the <cref see="NotSupportedException" /> exception.</para>
        /// <para>Use the <see cref="CanWrite" /> property to determine whether the current instance supports writing.</para>
        /// </remarks>
        /// <param name="buffer">An array of byte containing source data. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <example language="C#">This example shows how to compress data with the ZLibStream.Write method:
        /// <code>
        /// public class Test
        /// {
        ///    public static void Main()
        ///    {
        ///         /* Open the file containing source data */
        ///         FileStream sourceStream = new FileStream(@"c:\data\sourceFile.dat", FileMode.Open);
        ///         /* Create an output stream to store compressed data */
        ///         FileStream targetStream = new FileStream(@"c:\data\compressedFile.dat", FileMode.CreateNew);
        ///         /* Create a ZLibStream for compression of data containing in the sourceStream */
        ///         ZLibStream compressionStream = new ZLibStream(targetStream, CompressionDirection.Compression, false);
        ///         /* Create a buffer */
        ///         byte[] buffer = new byte[2000];
        ///         int len;
        ///         /* Read source data */
        ///         while ((len = sourceStream.Read(buffer, 0, 2000)) > 0)
        ///         {
        ///             /* Compress the source data and write compressed data to the targetStream */
        ///             compressionStream.Write(buffer, 0, len);
        ///         }
        ///         /* Close streams */
        ///         sourceStream.Close();
        ///         compressionStream.Close();
        ///     }
        /// }
        /// </code>
        /// </example>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.compressionDirection == CompressionDirection.Decompression)
                throw new NotSupportedException("You cannot write to the decompression stream");
            this.compressionStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Closes the current stream and the underlying stream depending on the <see cref="ZLibStream(Stream,CompressionDirection,bool)">leaveOpen</see> parameter passed to the ZLibStream constructor.
        /// </summary>
        public override void Close()
        {
            if (this.compressionDirection == CompressionDirection.Compression)
                this.compressionStream.Close();
            else
                this.decompressionStream.Close();
            if (!this.leaveOpen)
                this._stream.Close();
        }

        #endregion

        #region ZLibStream class methods

        /// <summary>
        /// Constructor which creates a new <see cref="ZLibStream" /> object and initializes it as a compression or a decompression one depending on the <paramref name="dir"/> parameter.
        /// </summary>
        /// <param name="stream">The stream containing compressed data for decompression or the stream to store compressed data for compression.</param>
        /// <param name="dir">One of the <see cref="CompressionDirection" /> values that indicates the action to take (compression or decompression).</param>
        /// <param name="leaveOpen">Whether we need to leave the underlying stream open when <see cref="ZLibStream.Close">closing</see> the current stream.</param>
        public ZLibStream(Stream stream, CompressionDirection dir, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException("Stream to decompression cannot be null", "stream");

            this.compressionDirection = dir;
            this._stream = stream;
            this.leaveOpen = leaveOpen;

            if (dir == CompressionDirection.Compression)
            {
                if (!this._stream.CanWrite)
                    throw new ArgumentException("The stream is not writable", "stream");
                this.compressionStream = new ZOutputStream(this._stream, this.compLevel);
            }
            else
            {
                if (!this._stream.CanRead)
                    throw new ArgumentException("The stream is not readable", "stream");
                this.decompressionStream = new ZInputStream(this._stream);
            }
        }

        /// <summary>
        /// Returns the current <see cref="CompressionDirection">compression direction</see>.
        /// </summary>
        public CompressionDirection GetCompressionDirection()
        {
            return this.compressionDirection;
        }

        /// <summary>
        /// Sets compression level for compression stream. If the current stream was created as a decompression stream the <see cref="NotSupportedException" /> is thrown.
        /// </summary>
        /// <param name="level">An integer value indicating the compression level. The parameter can take values from 0 to 9. You can pass -1 as a parameter to use the default compression level.</param>
        /// <exception cref="ArgumentException">The <c>ArgumentException</c> exception is thrown if the specified compression level is less then -1 or greater than 9.</exception>
        /// <exception cref="NotSupportedException">The NotSupportedException exception is thrown if we call this method for the decompression stream.</exception>
        public void SetCompressionLevel(int level)
        {
            if (level < -1 || level > 9)
                throw new ArgumentException("Invalid compression level is specified", "level");

            if (this.compressionDirection == CompressionDirection.Decompression)
                throw new NotSupportedException("The compression level cannot be set for decompression stream");

            this.compLevel = level;
            this.compressionStream = new ZOutputStream(this._stream, this.compLevel);
        }

        /// <summary>
        /// Gets the base stream for the current ZLibStream. The base stream is a stream passed to the <see cref="ZLibStream(Stream,CompressionDirection,bool)">ZLibStream constructor</see>.
        /// </summary>
        public Stream BaseStream
        {
            get
            {
                return this._stream;
            }
        }

        #endregion

    }
}
