﻿using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Audiotica.Core.Windows.Helpers;
using Audiotica.Windows.Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Audiotica.Windows
{
    sealed partial class App
    {
        public App()
        {
            WindowsAppInitializer.InitializeAsync();
            InitializeComponent();
        }

        public new static App Current => Application.Current as App;

        public Shell Shell { get; private set; }

        public override void OnInitialize()
        {
            // Wrap the frame in the shell (hamburger menu)
            Shell = new Shell(RootFrame);
            Window.Current.Content = Shell;

            // Set the bounds for the view to the core window
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            if (DeviceHelper.IsType(DeviceFamily.Mobile))
            {
                ApplicationView.GetForCurrentView().VisibleBoundsChanged += OnVisibleBoundsChanged;
                OnVisibleBoundsChanged(null, null);
            }
        }

        public override void OnLaunched(ILaunchActivatedEventArgs e)
        {
            NavigationService.Navigate(NavigationService.DefaultPage);
        }

        protected override bool OnUnhandledException(Exception ex)
        {
            try
            {
                // trying to get more details on exceptions
                var tel = new TelemetryClient(TelemetryConfiguration.Active);
                tel.TrackException(ex);
            }
            catch { }
            CurtainPrompt.ShowError("Crash prevented",
                () =>
                {
                    MessageBox.Show(
                        "The problem has been reported.  If you continue to experience this bug, email support.  Details: " +
                        ex.Message,
                        "Crash prevented");
                });
            return true;
        }

        private void OnVisibleBoundsChanged(ApplicationView sender, object args)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var h = Window.Current.Bounds.Height;
            var w = Window.Current.Bounds.Width;

            var top = Math.Ceiling((bounds.Top + h) - h);
            var left = Math.Ceiling((bounds.Left + w) - w);
            var right = Math.Ceiling(w - bounds.Right);
            var bottom = Math.Ceiling(h - bounds.Bottom);
            RootFrame.Margin = new Thickness(left, 0, right, bottom);
            Shell.HamburgerPadding = new Thickness(left, 0, 0, 0);
            Shell.NavBarMargin = new Thickness(0, 0, 0, bottom);
            Shell.Padding = new Thickness(left, top, 0, 0);
        }
    }
}