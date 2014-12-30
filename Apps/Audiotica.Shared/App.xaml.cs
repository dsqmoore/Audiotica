﻿#region

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Store;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Audiotica.Core.Common;
using Audiotica.Core.Utilities;
using Audiotica.View;
using Audiotica.ViewModel;
using GalaSoft.MvvmLight.Threading;
using GoogleAnalytics;

#endregion

namespace Audiotica
{
    public sealed partial class App
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection _transitions;
#endif

        public static ViewModelLocator Locator
        {
            get { return _locator ?? (_locator = Current.Resources["Locator"] as ViewModelLocator); }
        }

        public static Frame RootFrame { get; private set; }

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            Resuming += OnResuming;
        }

        private bool _init;
        private static ViewModelLocator _locator;

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            RootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new Frame {Style = (Style) Resources["AppFrame"]};

                Window.Current.Content = RootFrame;
                DispatcherHelper.Initialize();
#if BETA
                await BetaChangelogHelper.OnLaunchedAsync();
#endif
            }

            // ReSharper disable once CSharpWarnings::CS4014
            BootAppServices();

            if (RootFrame != null && RootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // Removes the turnstile navigation for startup.
                if (RootFrame.ContentTransitions != null)
                {
                    _transitions = new TransitionCollection();
                    foreach (var c in RootFrame.ContentTransitions)
                    {
                        _transitions.Add(c);
                    }
                }

                RootFrame.ContentTransitions = null;
                RootFrame.Navigated += RootFrame_FirstNavigated;
#endif
                var page = typeof (HomePage);

                if (BetaChangelogHelper.IsFirstRun)
                    page = typeof (FirstRunPage);

                if (!RootFrame.Navigate(page, e.Arguments))
                {
                    CurtainToast.ShowError("Failed to create initial page");
                }
            }

            Window.Current.Activate();
        }

        private async void BootAppServices()
        {
            if (!_init)
            {
                try
                {
                    await Locator.CollectionService.LoadLibraryAsync();
                    Locator.Download.LoadDownloads();
                }
                catch (Exception ex)
                {
                    EasyTracker.GetTracker().SendException(ex.Message + " " + ex.StackTrace, true);
                    CurtainToast.ShowError("ErrorBootingToast".FromLanguageResource());
                }

                _init = true;
            }
            Locator.AudioPlayerHelper.OnAppActive();
        }

#if WINDOWS_PHONE_APP
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            RootFrame.ContentTransitions = _transitions ?? new TransitionCollection {new NavigationThemeTransition()};
            RootFrame.Navigated -= RootFrame_FirstNavigated;
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            ApplicationView.GetForCurrentView().VisibleBoundsChanged += OnVisibleBoundsChanged;
            OnVisibleBoundsChanged(null, null);

            ReviewReminder();
        }

        private async void ReviewReminder()
        {
            var launchCount = AppSettingsHelper.Read<int>("LaunchCount");
            AppSettingsHelper.Write("LaunchCount", ++launchCount);
            if (launchCount != 5) return;

            var md = new MessageDialog(
                "Your feedback helps you improve this app. If you like it, please take a minute and rate it with five stars so we can continue working on new features and updates.",
                "Rate Audiotica!");
            md.Commands.Add(new UICommand("rate"));
            md.Commands.Add(new UICommand("no, thanks"));

            var result = await md.ShowAsync();

            if (result.Label == "rate")
            {
                Launcher.LaunchUriAsync(new Uri("ms-windows-store:reviewapp?appid=" + CurrentApp.AppId));
            }
        }

        private void OnVisibleBoundsChanged(ApplicationView sender, object args)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var h = Window.Current.Bounds.Height;

            var diff = Math.Ceiling(h - bounds.Bottom);
            RootFrame.Margin = new Thickness(0, 0, 0, diff);
        }
#endif

        private void OnResuming(object sender, object o)
        {
            Locator.AudioPlayerHelper.OnAppActive();
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            Locator.AudioPlayerHelper.OnAppSuspended();

            deferral.Complete();
        }
    }
}