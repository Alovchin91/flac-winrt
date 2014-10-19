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

using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using FLAC_WinRT.Example.Streaming;

namespace FLAC_WinRT.Example.Playback
{
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _taskDeferral;
        private SystemMediaTransportControls _mediaTransportControls;
        private FlacMediaSourceAdapter _currentMediaSourceAdapter;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            this._mediaTransportControls = SystemMediaTransportControls.GetForCurrentView();

            this._mediaTransportControls.ButtonPressed += this.OnMediaTransportControlsButtonPressed;
            this._mediaTransportControls.IsEnabled = true;
            this._mediaTransportControls.IsPlayEnabled = true;
            this._mediaTransportControls.IsPauseEnabled = true;
            this._mediaTransportControls.DisplayUpdater.ClearAll();
            this._mediaTransportControls.DisplayUpdater.Type = MediaPlaybackType.Music;
            this._mediaTransportControls.DisplayUpdater.Update();

            taskInstance.Canceled += this.OnTaskCanceled;
            taskInstance.Task.Completed += this.OnTaskCompleted;

            BackgroundMediaPlayer.Current.AutoPlay = true;
            BackgroundMediaPlayer.Current.CurrentStateChanged += this.OnCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromForeground += this.OnMessageReceivedFromForeground;

            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet {{"BackgroundPlayerStarted", null}});

            this._taskDeferral = taskInstance.GetDeferral();
        }

        private async void OnMessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            var trackList = e.Data.Where(d => d.Value is string && d.Key.Equals("AddTrack")).Select(d => (string) d.Value).ToList();
            if (!trackList.Any())
            {
                return;
            }
            
            var firstTrack = trackList.First();
            this._currentMediaSourceAdapter = await FlacMediaSourceAdapter.CreateAsync(firstTrack);
            BackgroundMediaPlayer.Current.SetMediaSource(this._currentMediaSourceAdapter.MediaSource);
        }

        private void OnCurrentStateChanged(MediaPlayer sender, object args)
        {
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Playing:
                    this._mediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case MediaPlayerState.Paused:
                    this._mediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlayerState.Stopped:
                    this._mediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaPlayerState.Closed:
                    this._mediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
            }
        }

        private void OnMediaTransportControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    BackgroundMediaPlayer.Current.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    BackgroundMediaPlayer.Current.Pause();
                    break;
            }
        }

        private void OnTaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs e)
        {
            this._taskDeferral.Complete();
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            this._mediaTransportControls.ButtonPressed -= this.OnMediaTransportControlsButtonPressed;

            sender.Canceled -= this.OnTaskCanceled;
            sender.Task.Completed -= this.OnTaskCompleted;

            BackgroundMediaPlayer.Current.CurrentStateChanged -= this.OnCurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromForeground -= this.OnMessageReceivedFromForeground;

            BackgroundMediaPlayer.Shutdown();

            this._taskDeferral.Complete();
        }
    }
}
