// Copyright (c) 2006, ComponentAce
// http://www.componentace.com
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
// Neither the name of ComponentAce nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission. 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

/*
Copyright (c) 2001 Lapo Luchini.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in 
the documentation and/or other materials provided with the distribution.

3. The names of the authors may not be used to endorse or promote products
derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS
OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
/*
* This program is based on zlib-1.1.3, so all credit should go authors
* Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
* and contributors of zlib.
*/
using System;
using System.IO;

namespace ZLib
{
	
    /// <summary>
    /// This class uses the Deflate algorithm (an industry standard algorithm for lossless file compression and decompression) to compress data. When <see cref="ZOutputStream(Stream,int)">creating</see> an instance of the class you passes a stream and an integer value indicating the compression level you want to use. The stream passed to the <see cref="ZOutputStream(Stream,int)">constructor</see> is used to store compressed data.
    /// </summary>    
    /// <example> The following code demonstrates how to use the <c>ZOutputStream</c> to compress data
    /// <code language="C#">
    /// [C#]
    /// private void compressFile(string inFile, string outFile)
    ///	{
    ///	    /* Create a file to store compressed data */
    ///		System.IO.FileStream compressedFile = new System.IO.FileStream(@"c:\data\compressed.dat", System.IO.FileMode.Create);
    ///		/* Open a file containing source data */
    ///		System.IO.FileStream sourceFile = new System.IO.FileStream(@"c:\data\source.dat", System.IO.FileMode.Open);	
    ///		/* Create ZOutputStream for compression */
    ///		ZOutputStream compressionStream = new ZOutputStream(compressedFile);
    ///
    ///		try
    ///		{
    ///				byte[] buffer = new byte[2000];
    ///				int len;
    ///				/* Read and compress data */
    ///				while ((len = sourceFile.Read(buffer, 0, 2000)) > 0)
    ///				{
    ///				  /* Store compressed data */
    ///					compressionStream.Write(buffer, 0, len);
    ///				}
    ///		}
    ///		finally
    ///		{
    ///			compressionStream.Close();
    ///			sourceFile.Close();
    ///			compressedFile.Close();
    ///		}
    ///	}
    /// </code>
    /// </example>
	internal class ZOutputStream : System.IO.Stream
    {
        #region Fields

        /// <summary>
        /// A ZStream object
        /// </summary>
	protected internal ZStream z = new ZStream();

	protected internal int bufsize = 4096;

        /// <summary>
        /// returns actual size of buffer, or set size of buffer between initial (4096) and maximum (2^15). Size of buffer can be only increased to prevent actual data loss
        /// </summary>
        virtual internal int BufferSize
        {
            get
            {
                return bufsize;
            }
            set
            {
                if (value >= 4096 && value <= 131072 && value > bufsize)
                {
                    byte[] tmpbuf = new byte[value];
                    Array.Copy(buf, tmpbuf, bufsize);
                    buf = new byte[value];
                    Array.Copy(tmpbuf, buf,bufsize);
                    bufsize = value;
                }
            }
        }

        /// <summary>
        /// Current internalFlush strategy
        /// </summary>
        private FlushStrategy flush;

        /// <summary>
        /// Buffer byte arrays
        /// </summary>
        private byte[] buf, buf1 = new byte[1];

        /// <summary>
        /// Out stream
        /// </summary>
        private Stream _stream;
        private MemoryStream outMemoryStream;

        #endregion

        #region Methods

        private void  InitBlock()
		{
            flush = FlushStrategy.Z_NO_FLUSH;
			buf = new byte[ZLibUtil.zLibBufSize];
		}

        /// <summary>
        /// Gets/Sets the <see creg="FlushStrategy">flush</see> strategy to use during compression.
        /// </summary>
		internal FlushStrategy FlushMode
		{
			get
			{
				return this.flush;
			}
			
			set
			{
				this.flush = value;
			}
			
		}


		/// <summary>
		/// Constructor which takes two parameters: the <paramref name="stream"/> to store compressed data in and the desired compression level.
		/// </summary>
		/// <param name="stream">A stream to be used to store compressed data.</param>
		/// <param name="level">An integer value indicating the desired compression level. The compression level can take values from 0 to 9. The maximum value indicates that the maximum compression should be achieved (but this method will be the slowest one). 0 means that no compression should be used at all. If you want to use the default compression level you can pass -1. Also you can use the constants from the <see cref="ZLibCompressionLevel" /> class.</param>
		internal ZOutputStream(System.IO.Stream stream, int level)
		{
			InitBlock();
			this._stream = stream;
			z.deflateInit(level);
		}

        internal ZOutputStream(MemoryStream outMemoryStream)
        {
            this.outMemoryStream = outMemoryStream;
        }

        /// <summary>
        /// Writes a byte array to the stream. This block of data is compressed and stored in the stream passed as a parameter to the <see cref="ZOutputStream(Stream,int)">class constructor</see>.
        /// </summary>
        /// <param name="buffer">A byte array to compress.</param>
        /// <param name="offset">Offset of the first byte to compress.</param>
        /// <param name="count">The number of bytes to compress from the buffer.</param>
        /// <example> The following code demonstrates how to use the <c>ZOutputStream</c> to compress data
        /// <code>
        /// [C#]
        /// private void compressFile(string inFile, string outFile)
        ///	{
        ///	    /* Create a file to store compressed data */
        ///		System.IO.FileStream compressedFile = new System.IO.FileStream(@"c:\data\compressed.dat", System.IO.FileMode.Create);
        ///		/* Open a file containing source data */
        ///		System.IO.FileStream sourceFile = new System.IO.FileStream(@"c:\data\source.dat", System.IO.FileMode.Open);	
        ///		/* Create ZOutputStream for compression */
        ///		ZOutputStream compressionStream = new ZOutputStream(compressedFile);
        ///
        ///		try
        ///		{
        ///				byte[] buffer = new byte[2000];
        ///				int len;
        ///				/* Read and compress data */
        ///				while ((len = sourceFile.Read(buffer, 0, 2000)) > 0)
        ///				{
        ///				  /* Store compressed data */
        ///					compressionStream.Write(buffer, 0, len);
        ///				}
        ///		}
        ///		finally
        ///		{
        ///			compressionStream.Close();
        ///			sourceFile.Close();
        ///			compressedFile.Close();
        ///		}
        ///	}
        /// </code>
        /// </example>
        public override void Write(byte[] buffer, int offset, int count)
		{

			if (count == 0)
				return ;

            if (buffer == null)
                throw new ArgumentNullException("buffer");

			int err;
			byte[] b = new byte[buffer.Length];
			System.Array.Copy(buffer, 0, b, 0, buffer.Length); 
			z.next_in = b;
			z.next_in_index = offset;
			z.avail_in = count;
			do 
			{
				z.next_out = buf;
				z.next_out_index = 0;
				z.avail_out = ZLibUtil.zLibBufSize;
				err = z.deflate(flush);
                if (err != (int)ZLibResultCode.Z_OK && err != (int)ZLibResultCode.Z_STREAM_END) 
					throw new ZStreamException("deflating: " + z.msg);
				_stream.Write(buf, 0, ZLibUtil.zLibBufSize - z.avail_out);

				//fixed infinite loop where z.istate.mode == 12, but z.avail_in != 0.
                if (z.istate != null)
                  if (z.istate.mode == InflateMode.DONE)
                    if (z.avail_in > 0) { z.avail_in = 0; }
			}
			while (z.avail_in > 0 || z.avail_out == 0);
		}
		
        /// <summary>
        /// Finishes compression.
        /// </summary>
		internal void Finish()
		{
			int err;
			do 
			{
				z.next_out = buf;
				z.next_out_index = 0;
				z.avail_out = ZLibUtil.zLibBufSize;
                err = z.deflate(FlushStrategy.Z_FINISH);
                if (err != (int)ZLibResultCode.Z_STREAM_END && err != (int)ZLibResultCode.Z_OK)
					throw new ZStreamException("deflating: " + z.msg);
				if (ZLibUtil.zLibBufSize - z.avail_out > 0)
				{
					_stream.Write(buf, 0, ZLibUtil.zLibBufSize - z.avail_out);
				}
			}
			while (z.avail_in > 0 || z.avail_out == 0);
			try
			{
				Flush();
			}
			catch
			{
			}
		}

        /// <summary>
        /// Frees allocated resources.
        /// </summary>
		internal void End()
		{
			z.deflateEnd();
			z.free();
			z = null;
		}

        /// <summary>
        /// Close the current and the underying streams.
        /// </summary>
		public override void  Close()
		{
			try
			{
				try
				{
					this.Finish();
				}
				catch
				{
				}
			}
			finally
			{
				End();
				_stream.Close();
				_stream = null;
			}
		}
		
        /// <summary>
        /// Flushes the underlying stream.
        /// </summary>
		public override void  Flush()
		{
			_stream.Flush();
		}

        /// <summary>
        /// Read data from the stream. Please note, that this method throws the <see cref="NotSupportedException"/> exception since <see cref="ZOutputStream" /> doesn't support <see cref="Stream.Read">reading</see>.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("ZOutputStream doesn't support reading");
        }

        /// <summary>
        /// Sets the length of the stream. This method throws the <see cref="NotSupportedException" /> exception since the <see cref="ZOutputStream" /> class doesn't support <see cref="Stream.SetLength">the operation</see>.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("ZOutputStream doesn't support seeking");
        }

        /// <summary>
        /// Sets the current position in the stream. This method throws the <see cref="NotSupportedException" /> exception since the <see cref="ZOutputStream" /> class doesn't support <see cref="Stream.Seek">the operation</see>.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("ZOutputStream doesn't support seeking");
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports reading.
        /// </summary>
        /// <remarks>Always returns <c>false</c> since the <see cref="ZOutputStream" /> doesn't support reading.</remarks>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Whether we can seek to a position in the stream.
        /// </summary>
        /// <returns>Always returns false since the <see cref="ZOutputStream" /> doesn't support <see creg="Stream.Seek">seeking</see>.</returns>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Whether the stream supports the writing operation.
        /// </summary>
        /// <returns>This property always returns <c>true</c>.</returns>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Returns the length of the stream. Please note that this property always throws the <see cref="NotSupportedException" /> exception since the stream doesn't support the <see cref="Stream.Length">property</see>.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException("ZLibStream doesn't support the Length property");
            }
        }

        /// <summary>
        /// Returns the current position in the compressed stream. This property throws the <see cref="NotSupportedException" /> exception since the stream doesn't support <see cref="Stream.Position">this property</see>.
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException("ZOutputStream doesn't support the Position property");
            }
            set
            {
                throw new NotSupportedException("ZOutputStream doesn't support seeking");
            }
        }

        #endregion
    }
}