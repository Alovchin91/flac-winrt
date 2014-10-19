using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;

namespace FLAC_WinRT.Example.App.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly RelayCommand _updateSongsListCommand;
        private readonly RelayCommand _playSelectedSongCommand;
        private readonly ObservableCollection<StorageFile> _songsCollection;
        private StorageFile _selectedSong;
        private bool _isBackgroundPlayerStarted;

        public MainViewModel()
        {
            this._songsCollection = new ObservableCollection<StorageFile>();
            this._updateSongsListCommand = new RelayCommand(this.UpdateSongsList);
            this._playSelectedSongCommand = new RelayCommand(PlaySelectedSong, () => SelectedSong != null);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand UpdateSongsListCommand
        {
            get { return this._updateSongsListCommand; }
        }

        public ICommand PlaySelectedSongCommand
        {
            get { return this._playSelectedSongCommand; }
        }

        public ObservableCollection<StorageFile> SongsCollection
        {
            get { return this._songsCollection; }
        }

        public StorageFile SelectedSong
        {
            get { return _selectedSong; }
            set
            {
                _selectedSong = value;
                _playSelectedSongCommand.RaiseCanExecuteChanged();
            }
        }

        private async void UpdateSongsList()
        {
            var musicFiles = await KnownFolders.MusicLibrary.GetFilesAsync();
            var flacFiles = await Task.Factory.StartNew(() =>
                musicFiles.Where(f => f.FileType.IndexOf("flac", StringComparison.OrdinalIgnoreCase) >= 0).ToList());

            foreach (var flacFile in flacFiles)
            {
                SongsCollection.Add(flacFile);
            }
        }

        private async void PlaySelectedSong()
        {
            if (SelectedSong == null)
                return;

            if (!_isBackgroundPlayerStarted)
            {
                var autoResetEvent = new AutoResetEvent(false);
                BackgroundMediaPlayer.MessageReceivedFromBackground += (o1, e1) =>
                {
                    if (e1.Data.ContainsKey("BackgroundPlayerStarted"))
                    {
                        autoResetEvent.Set();
                    }
                };

                _isBackgroundPlayerStarted = await Task.Factory.StartNew(() => autoResetEvent.WaitOne(2000));
            }

            if (_isBackgroundPlayerStarted)
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet { { "AddTrack", SelectedSong.Path } });
            }
        }
    }
}
