﻿/* libFLAC_winrt - FLAC library for Windows Runtime
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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;

namespace FLAC_WinRT.Example.Streaming
{
    public sealed class FlacMediaSourceAdapter : IDisposable
    {
        private readonly MediaStreamSource _mediaSource;

        private const int SAMPLE_BUFFER_SIZE = 2048;

        private FlacWaveInputStream _waveStream;

        private TimeSpan _currentTime;

        private ConcurrentQueue<IBuffer> _buffersQueue;

        public static async Task<FlacMediaSourceAdapter> CreateAsync(string filePath)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
            var fileStream = await storageFile.OpenAsync(FileAccessMode.Read);

            return new FlacMediaSourceAdapter(fileStream);
        }

        private FlacMediaSourceAdapter(IRandomAccessStream fileStream)
        {
            this._buffersQueue = new ConcurrentQueue<IBuffer>();
            this._currentTime = TimeSpan.Zero;

            this._waveStream = new FlacWaveInputStream(fileStream);
            var streamInfo = this._waveStream.GetStreamInfo();

            var encodingProperties = AudioEncodingProperties.CreatePcm(
                streamInfo.SampleRate, streamInfo.ChannelCount, streamInfo.BitsPerSample);

            this._mediaSource = new MediaStreamSource(new AudioStreamDescriptor(encodingProperties));
            this._mediaSource.Starting += this.OnMediaSourceStarting;
            this._mediaSource.SampleRequested += this.OnMediaSourceSampleRequested;
            this._mediaSource.Closed += this.OnMediaSourceClosed;
        }

        public IMediaSource MediaSource
        {
            get { return this._mediaSource; }
        }

        /// <summary>
        /// Raised when streaming is completed.
        /// </summary>
        public event EventHandler StreamComplete;

        private void OnMediaSourceStarting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs e)
        {
            var deferral = e.Request.GetDeferral();

            var streamInfo = this._waveStream.GetStreamInfo();
            sender.Duration = TimeSpan.FromSeconds(streamInfo.Duration);
            sender.CanSeek = true;
            sender.BufferTime = TimeSpan.Zero;

            e.Request.SetActualStartPosition(this._currentTime);

            deferral.Complete();
        }

        private async void OnMediaSourceSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs e)
        {
            var deferral = e.Request.GetDeferral();

            var instantBuffer = this.GetBuffer();
            var buffer = await this._waveStream.ReadAsync(instantBuffer, instantBuffer.Capacity, InputStreamOptions.None);

            MediaStreamSample sample;
            if (buffer.Length > 0)
            {
                sample = MediaStreamSample.CreateFromBuffer(buffer, this._currentTime);
                sample.Processed += this.OnSampleProcessed;

                var duration = this._waveStream.GetDurationFromBufferSize(buffer.Length);
                sample.Duration = TimeSpan.FromSeconds(duration);

                this._currentTime += sample.Duration;
                sender.BufferTime = sample.Duration;
            }
            else
            {
                sample = null;

                this._currentTime = TimeSpan.Zero;
                this._waveStream.Seek(0);
            }

            e.Request.Sample = sample;
            deferral.Complete();
        }

        private void OnSampleProcessed(MediaStreamSample sender, object args)
        {
            this._buffersQueue.Enqueue(sender.Buffer);
            sender.Processed -= this.OnSampleProcessed;
        }

        private IBuffer GetBuffer()
        {
            IBuffer buffer;
            bool dequeued = this._buffersQueue.TryDequeue(out buffer);
            if (!dequeued)
                buffer = new Buffer(SAMPLE_BUFFER_SIZE);
            return buffer;
        }

        private void OnMediaSourceClosed(MediaStreamSource sender, MediaStreamSourceClosedEventArgs e)
        {
            this._currentTime = TimeSpan.Zero;
            this._waveStream.Finish();
            this.RaiseStreamComplete();
        }

        private void RaiseStreamComplete()
        {
            EventHandler handler = this.StreamComplete;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (this._waveStream != null)
                {
                    this._waveStream.Dispose();
                    this._waveStream = null;
                }

                this._buffersQueue = null;
            }
        }

        ~FlacMediaSourceAdapter()
        {
            this.Dispose(false);
        }
    }
}