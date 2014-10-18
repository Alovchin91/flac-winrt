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
using System.IO;
using Windows.Storage.Streams;
using libFLAC.Decoder;
using libFLAC.Decoder.Callbacks;
using libFLAC.Format;
using libFLAC.Format.Metadata;

namespace FLAC_WinRT.Example.Streaming
{
    public sealed class FlacMediaDecoder
    {
        private readonly StreamDecoder _streamDecoder;
        private IBuffer _currentSample;

        private IRandomAccessStream _fileStream;

        private bool _isMetadataRead;
        private FlacMediaStreamInfo _streamInfo;

        public FlacMediaDecoder()
        {
            this._streamDecoder = new StreamDecoder();
            this._streamDecoder.WriteCallback += this.WriteCallback;
            this._streamDecoder.MetadataCallback += this.MetadataCallback;
        }

        public ulong Position
        {
            get { return this._fileStream != null ? this._fileStream.Position : 0; }
        }

        public void Dispose()
        {
            this.Finish();

            this._streamDecoder.WriteCallback -= this.WriteCallback;
            this._streamDecoder.MetadataCallback -= this.MetadataCallback;

            this._streamDecoder.Dispose();
        }

        public void Initialize(IRandomAccessStream fileStream)
        {
            if (!this._streamDecoder.IsValid)
                throw new InvalidOperationException("Decoder is not valid.");

            this._fileStream = fileStream;

            StreamDecoderInitStatus decoderInitStatus = this._streamDecoder.Init(this._fileStream);
            if (decoderInitStatus != StreamDecoderInitStatus.OK)
            {
                this._streamDecoder.Finish();
                this._streamDecoder.Dispose();
                throw new InvalidOperationException("Failed to initialize decoder.");
            }
        }

        public FlacMediaStreamInfo GetStreamInfo()
        {
            this.EnsureMetadataRead();
            return this._streamInfo;
        }

        public IBuffer GetSample()
        {
            this._currentSample = null;
            bool result = this._streamDecoder.ProcessSingle();
            if (!result)
                this._currentSample = null;
            return this._currentSample;
        }

        public void Seek(ulong position)
        {
            if (this.Position == position)
                return;

            this.EnsureMetadataRead();
            if (this._streamInfo.BitsPerSample == 0)
                throw new InvalidOperationException("Cannot seek current stream.");

            bool result = this._streamDecoder.SeekAbsolute(position/this._streamInfo.BitsPerSample);
            if (!result)
                throw new ArgumentOutOfRangeException("position", "Position overflow.");
        }

        public void Finish()
        {
            this._streamDecoder.Finish();
            this._fileStream = null;
        }

        private void EnsureMetadataRead()
        {
            if (this._isMetadataRead)
                return;

            bool result = this._streamDecoder.ProcessUntilEndOfMetadata();
            StreamDecoderState state = this._streamDecoder.GetState();

            if (!result || state == StreamDecoderState.EndOfStream)
                throw new EndOfStreamException("No metadata found, or unexpected call.");

            this._isMetadataRead = true;
        }

        private StreamDecoderWriteStatus WriteCallback(Frame frame, StreamDecoderWriteBuffer buffer)
        {
            this._currentSample = buffer.GetBuffer();
            return StreamDecoderWriteStatus.Continue;
        }

        private void MetadataCallback(StreamMetadata metadata)
        {
            if (metadata.Type == MetadataType.StreamInfo && metadata.StreamInfo != null)
            {
                uint blockAlign = metadata.StreamInfo.Channels*(metadata.StreamInfo.BitsPerSample/8);
                uint avgBytesPerSec = metadata.StreamInfo.SampleRate*blockAlign;

                long streamLength = GetWaveStreamLength(metadata.StreamInfo);
                double duration = (double) metadata.StreamInfo.TotalSamples/metadata.StreamInfo.SampleRate;

                this._streamInfo = new FlacMediaStreamInfo(
                    duration, avgBytesPerSec, streamLength,
                    metadata.StreamInfo.BitsPerSample,
                    metadata.StreamInfo.SampleRate,
                    metadata.StreamInfo.Channels);
            }
        }

        private static long GetWaveStreamLength(StreamInfo streamInfo)
        {
            const int dataChunkOffset = 36;
            const int waveHeaderSize = dataChunkOffset + 8;

            long bytesPerInterChannelSample = (streamInfo.Channels*streamInfo.BitsPerSample) >> 3;
            long dataLength = (long) streamInfo.TotalSamples*bytesPerInterChannelSample;
            long totalStreamLength = dataLength + waveHeaderSize;

            return totalStreamLength;
        }
    }
}