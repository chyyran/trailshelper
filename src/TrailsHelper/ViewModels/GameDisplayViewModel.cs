﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
        GameModel _game;
        public GameModel Game => _game;
        public ReactiveCommand<Unit, GameDisplayViewModel> InstallForGameCommand { get; }
        public Interaction<InstallViewModel, bool> ShowInstallDialog { get; }
        private string _installWindowIcon { get; }
        public WindowIcon InstallWindowIcon => new(AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new(_installWindowIcon)));
        public GameDisplayViewModel(Models.GameModel model, string installIcon)
        {
            _game = model;
            _installWindowIcon = installIcon;
            var ico = new WindowIcon(AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new(_installWindowIcon)));
            _steamInstallButtonText = this.WhenAnyValue(x => x.IsInstalled)
                .Select(x => x ? "Install to Steam version" : "Game not installed")
                .ToProperty(this, x => x.InstallButtonText);
            this.ShowInstallDialog = new();
            this.InstallForGameCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var install = new InstallViewModel(this, this.SteamPath);
                var result = await ShowInstallDialog.Handle(install);
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

        private bool _isLoaded = false;
        public bool IsLoaded { get => _isLoaded; set => this.RaiseAndSetIfChanged(ref _isLoaded, value); }

        private string _path = "Game not found.";
        public string SteamPath { get => _path; set => this.RaiseAndSetIfChanged(ref _path, value); }

        readonly ObservableAsPropertyHelper<string> _steamInstallButtonText;
        public string InstallButtonText => _steamInstallButtonText.Value;

        public string Title => _game.Title;

        public string Prefix => _game.ScriptPrefix;

        public string BattleVoiceFile => _game.BattleVoiceFile;

        private async Task LoadCover()
        {
            await using var imageStream = await _game.LoadCoverBitmapAsync();
            this.CoverArt = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }

        public async Task Load()
        {
            await this.LoadCover();
            this.IsInstalled = _game.Locator.IsInstalled();
            if (this.IsInstalled) 
                this.SteamPath = _game.Locator.GetInstallDirectory()!.FullName;
            this.IsLoaded = true;
        }
    }
}
