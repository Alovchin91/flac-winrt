/* libFLAC_winrt - FLAC library for Windows Runtime
 * Copyright (C) 2014  Alexander Ovchinnikov
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * - Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * - Neither the name of copyright holder nor the names of project's
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER
 * OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;

namespace FLAC_WinRT.Example.Streaming
{
    /// <summary>
    /// Wraps FLAC stream with a sequential stream of WAVE data.
    /// </summary>
    internal sealed class FlacWaveInputStream : IInputStream
    {
        private static readonly BufferSegment _noCurrentData = new BufferSegment(new Buffer(0));

        private readonly FlacMediaDecoder _streamDecoder;

        private IEnumerator<BufferSegment> _streamIterator;
        private BufferSegment _currentData;
        private bool _isIteratorFinished;

        private bool _headerRead;
        private FlacMediaStreamInfo _streamInfo;

        /// <summary>
        /// Creates a new instance of the <see cref="FlacWaveInputStream" /> class.
        /// </summary>
        /// <param name="fileStream">File stream open for read.</param>
        public FlacWaveInputStream(IRandomAccessStream fileStream)
        {
            this._streamDecoder = new FlacMediaDecoder();
            this._streamDecoder.Initialize(fileStream);

            this._streamIterator = this.IterateOverStream();
            this._isIteratorFinished = false;
        }

        /// <summary>
        /// Gets the byte offset of the stream.
        /// </summary>
        public ulong Position
        {
            get { return this._streamDecoder.Position; }
        }

        /// <summary>
        /// Gets FLAC stream info.
        /// </summary>
        /// <returns>FLAC stream info.</returns>
        /// <exception cref="EndOfStreamException">This stream contains no data.</exception>
        public FlacMediaStreamInfo GetStreamInfo()
        {
            this.EnsureHeaderRead();
            return this._streamInfo;
        }

        /// <summary>
        /// Converts specified sample's buffer size to a sample's duration.
        /// </summary>
        /// <param name="bufferSize">Sample's buffer size.</param>
        /// <returns>Sample's duration.</returns>
        /// <exception cref="EndOfStreamException">This stream contains no data.</exception>
        public double GetDurationFromBufferSize(uint bufferSize)
        {
            FlacMediaStreamInfo streamInfo = this.GetStreamInfo();

            if (streamInfo.BytesPerSecond == 0)
                return 0;

            return (double)bufferSize / streamInfo.BytesPerSecond;
        }

        /// <summary>
        /// Converts specified sample's duration to a sample's buffer size.
        /// </summary>
        /// <param name="duration">Sample's duration.</param>
        /// <returns>Sample's buffer size.</returns>
        /// <exception cref="System.IO.EndOfStreamException">This stream contains no data.</exception>
        public uint GetBufferSizeFromDuration(double duration)
        {
            FlacMediaStreamInfo streamInfo = this.GetStreamInfo();
            return (uint)(duration * streamInfo.BytesPerSecond);
        }

        /// <summary>
        /// Returns an asynchronous byte reader object.
        /// </summary>
        /// <returns>
        /// The asynchronous operation.
        /// </returns>
        /// <param name="buffer">The buffer into which the asynchronous read operation places the bytes that are read.</param>
        /// <param name="count">The number of bytes to read that is less than or equal to the Capacity value.</param>
        /// <param name="options">Specifies the type of the asynchronous read operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is larger than buffer's Capacity.</exception>
        /// <exception cref="EndOfStreamException">This stream contains no data.</exception>
        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return AsyncInfo.Run<IBuffer, uint>((t, p) => Task.FromResult(this.Read(buffer, count)));
        }

        private IBuffer Read(IBuffer buffer, uint count)
        {
            if (buffer == null)
                throw new ArgumentNullException();

            if (count > buffer.Capacity)
                throw new ArgumentOutOfRangeException();

            this.EnsureHeaderRead();

            if (this._currentData.Count >= count)
            {
                this._currentData.Buffer.CopyTo(this._currentData.Offset, buffer, 0, count);
                this._currentData = new BufferSegment(this._currentData.Buffer,
                    this._currentData.Offset + count, this._currentData.Count - count);
                buffer.Length = count;
                return buffer;
            }

            uint read = this._currentData.Count;
            if (read > 0)
                this._currentData.Buffer.CopyTo(this._currentData.Offset, buffer, 0, this._currentData.Count);
            this._currentData = _noCurrentData;

            while (this._streamIterator.MoveNext())
            {
                uint rest = count - read;
                if (this._streamIterator.Current.Count >= rest)
                {
                    this._streamIterator.Current.Buffer.CopyTo(0, buffer, read, rest);
                    read += rest;
                    this._currentData = new BufferSegment(this._streamIterator.Current.Buffer,
                        rest, this._streamIterator.Current.Count - rest);
                    break;
                }
                this._streamIterator.Current.Buffer.CopyTo(0, buffer, read, this._streamIterator.Current.Count);
                read += this._streamIterator.Current.Count;
            }

            buffer.Length = read;
            return buffer;
        }

        /// <summary>
        /// Sets the position of the stream to the specified value.
        /// </summary>
        /// <param name="position">The new position of the stream.</param>
        public void Seek(ulong position)
        {
            if (this._isIteratorFinished)
            {
                this._streamIterator.Dispose();
                this._streamIterator = this.IterateOverStream();
            }
            this._streamIterator.MoveNext();
            this._streamDecoder.Seek(position);
        }

        /// <summary>
        /// Finishes current decoder.
        /// </summary>
        public void Finish()
        {
            this._streamDecoder.Finish();
        }

        /// <summary>
        /// Releases all resources used by the stream.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IEnumerator<BufferSegment> IterateOverStream()
        {
            this._streamInfo = this._streamDecoder.GetStreamInfo();
            yield return _noCurrentData;

            while (true)
            {
                IBuffer sampleBuffer = this._streamDecoder.GetSample();

                if (sampleBuffer != null)
                {
                    yield return new BufferSegment(sampleBuffer);
                }
                else
                {
                    this._isIteratorFinished = true;
                    break;
                }
            }
        }

        private void EnsureHeaderRead()
        {
            if (!this._headerRead)
            {
                if (!this._streamIterator.MoveNext())
                {
                    throw new EndOfStreamException("The stream doesn't contain any data.");
                }

                this._currentData = this._streamIterator.Current;
                this._headerRead = true;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._streamIterator.Dispose();

                if (this._streamDecoder != null)
                {
                    this._streamDecoder.Finish();
                    this._streamDecoder.Dispose();
                }
            }
        }

        ~FlacWaveInputStream()
        {
            this.Dispose(false);
        }
    }
}
