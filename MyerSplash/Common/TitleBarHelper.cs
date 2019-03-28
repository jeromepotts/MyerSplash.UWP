using JP.Utils.UI;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;

namespace MyerSplash.Common
{
    public static class TitleBarHelper
    {
        public static void SetupTitleBarColor(bool isDarkTheme)
        {
            Debug.WriteLine($"==============SetupTitleBarColor: {isDarkTheme}");

            if (isDarkTheme)
            {
                SetUpLightTitleBar();
            }
            else
            {
                SetUpDarkTitleBar();
            }
        }

        private static void SetUpDarkTitleBar()
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonHoverBackgroundColor = "#35000000".ToColor();
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonPressedBackgroundColor = "#64000000".ToColor();
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveForegroundColor = Colors.Black;
        }

        private static void SetUpLightTitleBar()
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = (App.Current.Resources["TitleBarDarkBrush"] as SolidColorBrush).Color;
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveBackgroundColor = (App.Current.Resources["TitleBarDarkBrush"] as SolidColorBrush).Color;
            titleBar.InactiveForegroundColor = Colors.White;
            titleBar.ButtonBackgroundColor = "#00000000".ToColor();
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveBackgroundColor = "#00000000".ToColor();
            titleBar.ButtonInactiveForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = "#10FFFFFF".ToColor();
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedBackgroundColor = "#06FFFFFF".ToColor();
            titleBar.ButtonPressedForegroundColor = Colors.White;
        }
    }
}