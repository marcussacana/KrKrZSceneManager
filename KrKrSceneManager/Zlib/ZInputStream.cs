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

namespace KrKrSceneManager
{
	
    /// <summary>
    /// <para>The <c>ZInputStream</c> class is used for decompression of data. For decompression the inflate algorithm is used.</para>
    /// <para>To compress data you can use either the <see cref="ZOutputStream" /> class or the <see cref="ZLibStream" /> class.</para>
    /// </summary>
    /// <example> The following code demonstrates how to use the <c>ZInputStream</c> to decompresses data
    /// <code>
    /// [C#]
    /// private void decompressFile(string inFile, string outFile)
	///	{
	///	  /* Create a file to store decompressed data */
	///		System.IO.FileStream decompressedFile = new System.IO.FileStream(@"c:\data\decompressed.dat", System.IO.FileMode.Create);
	///		/* Open a file containing compressed data */
    ///		System.IO.FileStream compressedFile = new System.IO.FileStream(@"c:\data\compressed.dat", System.IO.FileMode.Open);	
	///		/* Create ZInputStream for decompression */
	///		ZInputStream decompressionStream = new ZInputStream(compressedFile);
	///
	///		try
	///		{
	///				byte[] buffer = new byte[2000];
	///				int len;
	///				/* Read and decompress data */
	///				while ((len = decompressionStream.Read(buffer, 0, 2000)) > 0)
	///				{
	///				  /* Store decompressed data */
	///					decompressedFile.Write(buffer, 0, len);
	///				}
	///		}
	///		finally
	///		{
	///			decompressionStream.Close();
	///			decompressedFile.Close();
	///			compressedFile.Close();
	///		}
    ///	}
    /// </code>
    /// </example>
	internal class ZInputStream : System.IO.Stream
    {
        #region Fields

        /// <summary>
        /// ZStream object
        /// </summary>
        private ZStream z = new ZStream();

        /// <summary>
        /// Flush strategy
        /// </summary>
        private FlushStrategy flush;

        /// <summary>
        /// Buffers
        /// </summary>
        private byte[] buf, buf1 = new byte[1];

        /// <summary>
        /// Stream to decompress data from
        /// </summary>
        private Stream _stream = null;

        /// <summary>
        /// True if no more input is available
        /// </summary>
        private bool nomoreinput = false;

        private bool needCopyArrays = false;

        #endregion

        #region Methods
        
        /// <summary>
        /// Initializes a block
        /// </summary>
        private void  InitBlock()
		{
			flush = FlushStrategy.Z_NO_FLUSH;
			buf = new byte[ZLibUtil.zLibBufSize];
		}

        /// <summary>
        /// Gets/Sets the current <see cref="FlushStrategy">flush strategy</see>.
        /// </summary>
		internal FlushStrategy FlushMode
		{
			get
			{
				return flush;
			}
			
			set
			{
				this.flush = value;
			}
			
		}

		/// <summary>
		/// Constructor which takes one argument - the <paramref name="stream"/> containing data to decompress.
		/// </summary>
		/// <param name="stream">A stream to decompress data from.</param>
		internal ZInputStream(Stream stream)
		{
			InitBlock();
			this._stream = stream;
			z.inflateInit();
			z.next_in = buf;
			z.next_in_index = 0;
			z.avail_in = 0;
		}
		
        /// <summary>
        /// Reads a byte of decompressed data from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream. 
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream. </returns>
        public override int ReadByte()
        {
            if (Read(buf1, 0, 1) == -1)
                return (-1);
            return (buf1[0] & 0xFF);
        }

        /// <summary>
        /// Reads a number of decompressed bytes into the specified byte array. 
        /// </summary>
        /// <param name="buffer">The array used to store decompressed bytes.</param>
        /// <param name="offset">The location in the array to begin reading.</param>
        /// <param name="count">The number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were decompressed into the byte array.</returns>
        /// <example> The following code demonstrates how to use the <c>ZInputStream</c> to decompresses data
        /// <code>
        /// [C#]
        /// private void decompressFile(string inFile, string outFile)
        ///	{
        ///	  /* Create a file to store decompressed data */
        ///		System.IO.FileStream decompressedFile = new System.IO.FileStream(@"c:\data\decompressed.dat", System.IO.FileMode.Create);
        ///		/* Open a file containing compressed data */
        ///		System.IO.FileStream compressedFile = new System.IO.FileStream(@"c:\data\compressed.dat", System.IO.FileMode.Open);	
        ///		/* Create ZInputStream for decompression */
        ///		ZInputStream decompressionStream = new ZInputStream(compressedFile);
        ///
        ///		try
        ///		{
        ///				byte[] buffer = new byte[2000];
        ///				int len;
        ///				/* Read and decompress data */
        ///				while ((len = decompressionStream.Read(buffer, 0, 2000)) > 0)
        ///				{
        ///				  /* Store decompressed data */
        ///					decompressedFile.Write(buffer, 0, len);
        ///				}
        ///		}
        ///		finally
        ///		{
        ///			decompressionStream.Close();
        ///			decompressedFile.Close();
        ///			compressedFile.Close();
        ///		}
        ///	}
        /// </code>
        /// </example>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;

            if (this.needCopyArrays && ZLibUtil.CopyLargeArrayToSmall.GetRemainingDataSize() > 0)
                return ZLibUtil.CopyLargeArrayToSmall.CopyData();
            else
                this.needCopyArrays = false;

            bool call_finish = false;
            int err;
            z.next_out = buffer;
            z.next_out_index = offset;
            z.avail_out = count;
            do
            {
                if ((z.avail_in == 0) && (!nomoreinput))
                {
                    // if buffer is empty and more input is available, refill it
                    z.next_in_index = 0;
                    z.avail_in = ZLibUtil.ReadInput(_stream, buf, 0, ZLibUtil.zLibBufSize); //(ZLibUtil.zLibBufSize<z._avail_out ? ZLibUtil.zLibBufSize : z._avail_out));
                    if (z.avail_in == -1)
                    {
                        z.avail_in = 0;
                        nomoreinput = true;
                    }
                }
                if ((z.avail_in == 0) && nomoreinput)
                {
                    call_finish = true;
                    break;
                }
 
                err = z.inflate(flush);
                if (nomoreinput && (err == (int)ZLibResultCode.Z_BUF_ERROR))
                    return -1;
                if (err != (int)ZLibResultCode.Z_OK && err != (int)ZLibResultCode.Z_STREAM_END)
                    throw new ZStreamException("inflating: " + z.msg);
                if (nomoreinput && (z.avail_out == count))
                    return -1;
            }
            while (z.avail_out == count && err == (int)ZLibResultCode.Z_OK);
            if (call_finish)
                return Finish(buffer, offset, count);
            return (count - z.avail_out);
        }

        /// <summary>
        /// Writes a byte to the stream. Please note, that this method throws the <see cref="NotSupportedException" /> since <see cref="ZInputStream" /> doesn't support writing.
        /// </summary>
        public override void Write(System.Byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("ZInputStream doesn't support writing");
        }

        /// <summary>
        /// Skips n decompressed bytes in the stream.
        /// </summary>
        /// <param name="n">The number of bytes to skip.</param>
        public long Skip(long n)
        {
            int len = 512;
            if (n < len)
                len = (int)n;
            byte[] tmp = new byte[len];
            return ((long)ZLibUtil.ReadInput(this, tmp, 0, tmp.Length));
        }

        /// <summary>
        /// Reads the final block of decompressed data and finishes decompression.
        /// </summary>
        /// <param name="buffer">The array used to store decompressed bytes.</param>
        /// <param name="offset">The location in the array to begin reading.</param>
        /// <param name="count">The number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were decompressed into the byte array.</returns>
        public virtual int Finish(byte[] buffer, int offset, int count)
        {

            int err;
            int nWritten = 0;
            do
            {
                //copy to buf, emulating File.Write()
                z.next_out = buf;
                z.next_out_index = 0;
                z.avail_out = ZLibUtil.zLibBufSize;
                err = z.inflate(FlushStrategy.Z_FINISH);
                if (err != (int)ZLibResultCode.Z_STREAM_END && err != (int)ZLibResultCode.Z_OK)
                    throw new ZStreamException("inflating: " + z.msg);
                if (ZLibUtil.zLibBufSize - z.avail_out > 0) //
                {
                    this.needCopyArrays = true;
                    ZLibUtil.CopyLargeArrayToSmall.Initialize(buf, 0, ZLibUtil.zLibBufSize - z.avail_out, buffer, offset + nWritten, count);
                    int nWrittenNow = ZLibUtil.CopyLargeArrayToSmall.CopyData();
                    if (ZLibUtil.CopyLargeArrayToSmall.GetRemainingDataSize() > 0)
						return nWrittenNow;
					nWritten += ZLibUtil.zLibBufSize - z.avail_out; //1

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
			return nWritten;
		}
		
        /// <summary>
        /// Frees allocated resources.
        /// </summary>
		internal virtual void End()
		{
		    z.inflateEnd();
			z.free();
			z = null;
		}
		
        /// <summary>
        /// Closes the stream and the underlying stream.
        /// </summary>
		public override void  Close()
		{
			End();
			_stream.Close();
			_stream = null;
		}


		/// <summary>
		/// Flushes the stream
		/// </summary>
		public override void Flush()
		{
			_stream.Flush();
		}

        /// <summary>
        /// Sets the length of the stream. Please note that the <see cref="ZInputStream" /> class doesn't support the <see cref="Stream.SetLength">SetLength</see> operation and thus the <see cref="NotSupportedException" /> is thrown.
        /// </summary>
        /// <param name="value">A new length of the stream.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("ZInputStream doesn't support SetLength");
        }
        
        /// <summary>
        /// Gets the length of the stream in bytes. Please note that the <see cref="ZInputStream" /> class doesn't support the <see cref="Stream.Length">Length</see> property and thus the <see cref="NotSupportedException" /> is thrown.
        /// </summary>
        public override long Length
        {
            get 
            {
                throw new NotSupportedException("ZLibStream doesn't support the Length property");
            }
        }

        /// <summary>
        /// Gets/Sets the current position in the stream.  Please note that the <see cref="ZInputStream" /> class doesn't support the <see cref="Stream.Position">Position</see> property and thus the <see cref="NotSupportedException" /> is thrown.
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException("ZInputStream doesn't support the Position property");
            }
            set
            {
                throw new NotSupportedException("ZInputStream doesn't support seeking");
            }
        }
        
        /// <summary>
        /// Seek to the offset position (from the beginning or from the current position, etc. see the available values of the <paramref name="origin" /> parameter)in the stream. This method throws an exception since ZInpitStream doesn't support <see cref="Stream.Seek">seeking</see> operation
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("ZInputStream doesn't support seeking");
        }
        
        /// <summary>
        /// Gets a value indicating whether the stream supports reading while decompressing a file. 
        /// </summary>
        /// <returns>
        /// Always returns <c>true</c>.
        /// </returns>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports writing. 
        /// </summary>
        /// <returns>For the <see cref="ZInputStream" /> always returns <c>false</c>.</returns>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports seeking.
        /// </summary>
        /// <returns>Always returns <c>false</c>.</returns>
        public override bool CanSeek
        {
            get { return false; }
        }

        #endregion
    }
}