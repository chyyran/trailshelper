using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using TrailsHelper.Models;

namespace TrailsHelper.ViewModels
{
    public class GameDisplayViewModel : ViewModelBase
    {
        readonly GameModel _game;
        public GameModel Game => _game;
        public ReactiveCommand<Unit, GameDisplayViewModel> InstallForSteamGameCommand { get; }
        public ReactiveCommand<Unit, GameDisplayViewModel> BrowseThenInstallGameCommand { get; }

        public Interaction<InstallViewModel, bool> ShowInstallDialog { get; }
        public Interaction<GameDisplayViewModel, DirectoryInfo?> BrowseInstallFolderDialog { get; }

        private string _installWindowIcon { get; }
        public WindowIcon InstallWindowIcon => new(AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new(_installWindowIcon)));
        public GameDisplayViewModel(Models.GameModel model, string installIcon)
        {
            _game = model;
            _installWindowIcon = installIcon;
            var ico = new WindowIcon(AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new(_installWindowIcon)));
            _steamInstallButtonText = this.WhenAnyValue(x => x.IsInstalled, x => x.IsSteamReady)
                .Select(value =>
                {
                    var (installed, steamReady) = value;
                    if (!steamReady)
                        return "Waiting for Steam...";

                    return installed ? "Install to Steam version" : "Game not installed";
                })
                .ToProperty(this, x => x.InstallButtonText);

            this.ShowInstallDialog = new();
            this.BrowseInstallFolderDialog = new();

            this.InstallForSteamGameCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var install = new InstallViewModel(this, this.SteamPath);
                var installResult = await ShowInstallDialog.Handle(install);
                return this;
            });
            this.BrowseThenInstallGameCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var browseResult = await BrowseInstallFolderDialog.Handle(this);
                if (browseResult == null)
                {
                    return this;
                }

                var install = new InstallViewModel(this, browseResult.FullName);
                var installResult = await ShowInstallDialog.Handle(install);
                return this;
            });
        }

        private Bitmap? _cover;
        public Bitmap? CoverArt
        {
            get => _cover;
            private set => this.RaiseAndSetIfChanged(ref _cover, value);
        }

        private bool _isInstalled = false;
        public bool IsInstalled { get => _isInstalled; set => this.RaiseAndSetIfChanged(ref _isInstalled, value); }

        private bool _isSteamReady = false;
        public bool IsSteamReady { get => _isSteamReady; set => this.RaiseAndSetIfChanged(ref _isSteamReady, value); }

        private bool _isSteamDisabled = false;
        public bool IsSteamDisabled { get => _isSteamDisabled; set => this.RaiseAndSetIfChanged(ref _isSteamDisabled, value); }

        private bool _isLoaded = false;
        public bool IsLoaded { get => _isLoaded; set => this.RaiseAndSetIfChanged(ref _isLoaded, value); }

        private string _path = "Game not found.";
        public string SteamPath { get => _path; set => this.RaiseAndSetIfChanged(ref _path, value); }

        readonly ObservableAsPropertyHelper<string> _steamInstallButtonText;
        public string InstallButtonText => _steamInstallButtonText.Value;

        public string Title => _game.Title;

        public string Prefix => _game.ScriptPrefix;

        public string BattleVoiceFile => _game.BattleVoiceFile;

        public async Task LoadCover()
        {
            await using var imageStream = await _game.LoadCoverBitmapAsync();
            this.CoverArt = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
            this.IsLoaded = true;
        }

        public async Task LoadSteam()
        {
            this.IsInstalled = _game.Locator.IsInstalled();
            if (this.IsInstalled) 
                this.SteamPath = _game.Locator.GetInstallDirectory()!.FullName;
        }
    }
}
