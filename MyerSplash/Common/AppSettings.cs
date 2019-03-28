using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using MyerSplash.ViewModel;
using MyerSplashShared.Utils;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace MyerSplash.Common
{
    public class AppSettings : ViewModelBase
    {
        public const int LightTheme = 0;
        public const int DarkTheme = 1;
        public const int SystemTheme = 2;

        public ApplicationDataContainer LocalSettings { get; set; }

        private MainViewModel MainVM
        {
            get
            {
                return SimpleIoc.Default.GetInstance<MainViewModel>();
            }
        }

        private Thickness _imageMargin;
        public Thickness ImageMargin
        {
            get
            {
                return _imageMargin;
            }
            set
            {
                if (value != _imageMargin)
                {
                    _imageMargin = value;
                    RaisePropertyChanged(() => ImageMargin);
                }
            }
        }

        private Thickness _imageListPadding;
        public Thickness ImageListPadding
        {
            get
            {
                return _imageListPadding;
            }
            set
            {
                if (value != _imageListPadding)
                {
                    _imageListPadding = value;
                    RaisePropertyChanged(() => ImageListPadding);
                }
            }
        }

        public bool EnableCompactMode
        {
            get
            {
                // No option for Xbox.
                if (DeviceUtil.IsXbox) return true;
                return ReadSettings(nameof(EnableCompactMode), false);
            }
            set
            {
                SaveSettings(nameof(EnableCompactMode), value);
                RaisePropertyChanged(() => EnableCompactMode);

                if (!_constructing)
                {
                    Events.LogCompatMode(value);
                }

                if (value)
                {
                    ImageMargin = new Thickness(0);
                    ImageListPadding = new Thickness(0);
                }
                else
                {
                    ImageMargin = new Thickness(8);
                    ImageListPadding = new Thickness(8, 0, 8, 0);
                }
            }
        }

        public bool EnableTile
        {
            get
            {
                return ReadSettings(nameof(EnableTile), true);
            }
            set
            {
                SaveSettings(nameof(EnableTile), value);
                RaisePropertyChanged(() => EnableTile);

                Events.LogTile(value);

                if (!value)
                {
                    LiveTileUpdater.CleanUpTile();
                }
            }
        }

        public bool EnableTodayRecommendation
        {
            get
            {
                return true;
            }
        }

        public bool EnableQuickDownload
        {
            get
            {
                return ReadSettings(nameof(EnableQuickDownload), false);
            }
            set
            {
                SaveSettings(nameof(EnableQuickDownload), value);
                RaisePropertyChanged(() => EnableQuickDownload);
            }
        }

        public bool EnableScaleAnimation
        {
            get
            {
                return ReadSettings(nameof(EnableScaleAnimation), true);
            }
            set
            {
                SaveSettings(nameof(EnableScaleAnimation), value);
                RaisePropertyChanged(() => EnableScaleAnimation);

                Events.LogScaleAnimation(value);
            }
        }

        public string SaveFolderPath
        {
            get
            {
                return ReadSettings(nameof(SaveFolderPath), "");
            }
            set
            {
                SaveSettings(nameof(SaveFolderPath), value);
                RaisePropertyChanged(() => SaveFolderPath);
            }
        }

        public int DefaultCategory
        {
            get
            {
                return ReadSettings(nameof(DefaultCategory), 0);
            }
            set
            {
                SaveSettings(nameof(DefaultCategory), value);
                RaisePropertyChanged(() => DefaultCategory);
            }
        }

        public int BackgroundWallpaperSource
        {
            get
            {
                return ReadSettings(nameof(BackgroundWallpaperSource), 0);
            }
            set
            {
                SaveSettings(nameof(BackgroundWallpaperSource), value);
                RaisePropertyChanged(() => BackgroundWallpaperSource);

                Events.LogBackgroundWallpapersSource(value);

                switch (value)
                {
                    case 0:
                        var task0 = BackgroundTaskRegister.UnregisterAsync();
                        break;
                    case 1:
                    // fall through
                    case 2:
                    // fall through
                    case 3:
                        var task1 = BackgroundTaskRegister.RegisterAsync();
                        break;
                }
            }
        }

        public int LoadQuality
        {
            get
            {
                return 0;
            }
        }

        public int SaveQuality
        {
            get
            {
                return ReadSettings(nameof(SaveQuality), 1);
            }
            set
            {
                SaveSettings(nameof(SaveQuality), value);
                RaisePropertyChanged(() => SaveQuality);
            }
        }

        private bool _isLight;
        public bool IsLight
        {
            get
            {
                return _isLight;
            }
            set
            {
                _isLight = value;
                TitleBarHelper.SetupTitleBarColor(!value);
            }
        }

        public int ThemeMode
        {
            get
            {
                return ReadSettings(nameof(ThemeMode), SystemTheme);
            }
            set
            {
                SaveSettings(nameof(ThemeMode), value);
                RaisePropertyChanged(() => ThemeMode);

                ElementTheme theme;
                switch (value)
                {
                    case LightTheme:
                        theme = ElementTheme.Light;
                        IsLight = true;
                        break;
                    case DarkTheme:
                        theme = ElementTheme.Dark;
                        IsLight = false;
                        break;
                    default:
                        theme = ElementTheme.Default;
                        break;
                }
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = theme;
                }
            }
        }

        private readonly bool _constructing = true;
        private UISettings _uiSettings;

        public AppSettings()
        {
            LocalSettings = ApplicationData.Current.LocalSettings;
            EnableCompactMode = EnableCompactMode;
            ThemeMode = ThemeMode;

            _uiSettings = new UISettings();
            _uiSettings.ColorValuesChanged += Settings_ColorValuesChanged;
            UpdateThemeAndNotify(_uiSettings);

            _constructing = false;
        }

        private async void Settings_ColorValuesChanged(UISettings sender, object args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (ThemeMode != SystemTheme) return;
                UpdateThemeAndNotify(sender);
            });
        }

        private void UpdateThemeAndNotify(UISettings settings)
        {
            IsLight = settings.GetColorValue(UIColorType.Background) == Colors.Black;
        }

        public async Task<StorageFolder> GetSavingFolderAsync()
        {
            var folder = await KnownFolders.PicturesLibrary.CreateFolderAsync("MyerSplash", CreationCollisionOption.OpenIfExists);
            return folder;
        }

        public async Task<StorageFolder> GetWallpaperFolderAsync()
        {
            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("WallpapersTemp", CreationCollisionOption.OpenIfExists);
            return folder;
        }

        private void SaveSettings(string key, object value)
        {
            LocalSettings.Values[key] = value;
        }

        private T ReadSettings<T>(string key, T defaultValue)
        {
            if (LocalSettings.Values.ContainsKey(key))
            {
                return (T)LocalSettings.Values[key];
            }
            if (defaultValue != null)
            {
                return defaultValue;
            }
            return default(T);
        }

        private static readonly Lazy<AppSettings> lazy = new Lazy<AppSettings>(() => new AppSettings());

        public static AppSettings Instance { get { return lazy.Value; } }
    }
}