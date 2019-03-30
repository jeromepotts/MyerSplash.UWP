﻿using GalaSoft.MvvmLight.Ioc;
using JP.Utils.Data;
using JP.Utils.Helper;
using MyerSplash.Common;
using MyerSplash.Model;
using MyerSplash.ViewModel;
using MyerSplashCustomControl;
using MyerSplashShared.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;

namespace MyerSplash.View.Page
{
    public sealed partial class MainPage : BindablePage
    {
        private const float TITLE_GRID_HEIGHT = 70;

        private MainViewModel MainVM
        {
            get
            {
                return SimpleIoc.Default.GetInstance<MainViewModel>();
            }
        }

        private Compositor _compositor;
        private Visual _refreshBtnVisual;

        private Visual _titleBarPlaceholderVisual;
        private Visual _titleContentVisual;

        private double _lastVerticalOffset;
        private bool _isHideTitleGrid;

        private ImageItem _clickedImg;
        private FrameworkElement _clickedContainer;

        private Dictionary<int, double> _scrollingPositions = new Dictionary<int, double>();

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(MainViewModel),
                new PropertyMetadata(false, OnLoadingPropertyChanged));

        public static void OnLoadingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var page = d as MainPage;
            if (!(bool)e.NewValue)
            {
                page.HideLoading();
            }
            else page.ShowLoading();
        }

        public MainPage()
        {
            this.InitializeComponent();
            InitComposition();
            InitBinding();

            // Ugly, I should come up with better solutions.
            MainVM.AboutToUpdateSelectedIndex += MainVM_AboutToUpdateSelectedIndex;
            MainVM.DataUpdated += MainVM_DataUpdated;

            this.SizeChanged += MainPage_SizeChanged;
            
            if (DeviceHelper.IsXbox)
            {
                TitleGridContent.Padding = new Thickness(0);
            }
        }

        private bool _showMoreFlyout = false;

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _showMoreFlyout = e.NewSize.Width < 800;

            DownloadEntryBtn.Visibility = _showMoreFlyout ? Visibility.Collapsed : Visibility.Visible;
            SearchBtn.Visibility = _showMoreFlyout ? Visibility.Collapsed : Visibility.Visible;
            SettingBtn.Visibility = _showMoreFlyout ? Visibility.Collapsed : Visibility.Visible;
        }

        private void MoreBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_showMoreFlyout)
            {
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
            else
            {
                MainVM.PresentAboutCommand.Execute(null);
            }
        }

        private async void MainVM_DataUpdated(object sender, EventArgs e)
        {
            await PostScrollToCachedPosition();
        }

        protected override void SetupNavigationBackBtn()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private async void MainVM_AboutToUpdateSelectedIndex(object sender, int e)
        {
            RecordScrollingPosition(e);
            await PostScrollToCachedPosition();
        }

        private async Task PostScrollToCachedPosition()
        {
            if (_scrollingPositions.ContainsKey(MainVM.SelectedIndex))
            {
                await Task.Yield();
                ListControl.ScrollToPosition(_scrollingPositions[MainVM.SelectedIndex]);
            }
        }

        private void InitBinding()
        {
            var b = new Binding()
            {
                Source = MainVM,
                Path = new PropertyPath("IsRefreshing"),
                Mode = BindingMode.TwoWay,
            };
            this.SetBinding(IsLoadingProperty, b);
        }

        private void InitComposition()
        {
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            _refreshBtnVisual = RefreshBtn.GetVisual();
            _titleContentVisual = TitleGridContent.GetVisual();
            _titleBarPlaceholderVisual = TitleBarBackgroundPlaceholder.GetVisual();
        }

        private void RecordScrollingPosition(int oldValue)
        {
            _scrollingPositions[oldValue] = ListControl.ScrollingPosition;
        }

        #region Loading animation

        private void ShowLoading()
        {
            ListControl.Refreshing = true;
        }

        private void HideLoading()
        {
            ListControl.Refreshing = false;
        }

        #endregion Loading animation

        private void ListControl_OnClickItemStarted(ImageItem img, FrameworkElement container)
        {
            _clickedContainer = container;
            _clickedImg = img;

            ToggleDetailControlAnimation();
        }

        private void ToggleDetailControlAnimation()
        {
            DetailControl.CurrentImage = _clickedImg;
            DetailControl.Show(_clickedContainer);

            var key = (string)App.Current.Resources["GestureKey"];
            if (!LocalSettingHelper.HasValue(key))
            {
                ToggleGestureTipsControlAnimation(true, 500);
            }

            NavigationService.AddOperation(() =>
            {
                DetailControl.Hide();
                return true;
            });
        }

        #region Scrolling

        private void ToggleRefreshBtnAnimation(bool show)
        {
            if (!AppSettings.Instance.EnableCompactMode)
            {
                return;
            }

            var scaleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(1f, show ? 1f : 0);
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(500);

            _refreshBtnVisual.CenterPoint = new Vector3((float)RefreshBtn.ActualWidth / 2f, (float)RefreshBtn.ActualHeight / 2f, 0f);
            _refreshBtnVisual.StartAnimation("Scale.X", scaleAnimation);
            _refreshBtnVisual.StartAnimation("Scale.Y", scaleAnimation);
        }

        private void ToggleTitleContentAnimation(bool show)
        {
            var offsetAnimation = _compositor.CreateScalarKeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(1f, show ? 0 : -(float)TitleGridContent.ActualHeight);
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(300);

            _titleBarPlaceholderVisual.StartAnimation(
                _titleBarPlaceholderVisual.GetTranslationYPropertyName(), offsetAnimation);
            _titleContentVisual.StartAnimation(
                _titleContentVisual.GetTranslationYPropertyName(), offsetAnimation);
        }

        private void ListControl_OnScrollViewerViewChanged(ScrollViewer scrollViewer)
        {
            if (DeviceUtil.IsXbox) return;

            if ((scrollViewer.VerticalOffset - _lastVerticalOffset) > 5 && !_isHideTitleGrid)
            {
                _isHideTitleGrid = true;
                ToggleRefreshBtnAnimation(false);

                if (AppSettings.Instance.EnableCompactMode)
                {
                    ToggleTitleContentAnimation(false);
                }
            }
            else if (scrollViewer.VerticalOffset < _lastVerticalOffset && _isHideTitleGrid)
            {
                _isHideTitleGrid = false;
                ToggleRefreshBtnAnimation(true);

                if (AppSettings.Instance.EnableCompactMode)
                {
                    ToggleTitleContentAnimation(true);
                }
            }
            _lastVerticalOffset = scrollViewer.VerticalOffset;
        }
        #endregion Scrolling

        private void OnPresentedChanged(object sender, PresentedArgs e)
        {
            if (!e.Presented)
            {
                SetupTitleBar();
            }
        }

        protected override void SetupTitleBar()
        {
            Window.Current.SetTitleBar(DummyTitleBar);
        }

        private void TopNavigationControl_TitleClicked(object sender, TitleClickEventArg e)
        {
            if (e.NewIndex == e.OldIndex)
            {
                ListControl.ScrollToTop();
            }
        }

        private void GestureControl_OnClickToDismiss(object sender, EventArgs e)
        {
            ToggleGestureTipsControlAnimation(false);

            var key = (string)App.Current.Resources["GestureKey"];
            LocalSettingHelper.AddValue(key, true);
        }

        private void ToggleGestureTipsControlAnimation(bool show, int delayMillis = 0)
        {
            var visual = GestureControl.GetVisual();
            visual.Opacity = show ? 0f : 1f;

            GestureControl.Visibility = Visibility.Visible;

            var anim = _compositor.CreateScalarKeyFrameAnimation();
            anim.Duration = TimeSpan.FromMilliseconds(500);
            anim.InsertKeyFrame(1f, show ? 1f : 0f);
            anim.DelayTime = TimeSpan.FromMilliseconds(delayMillis);
            var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += (s, e) =>
            {
                if (!show)
                {
                    GestureControl.StopAnimation();
                    GestureControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    GestureControl.StartAnimation();
                }
            };
            visual.StartAnimation("Opacity", anim);
            batch.End();
        }
    }
}