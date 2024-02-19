using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MeCardParser;
using static MeCardParser.MeCardRawWiFi;

namespace NetworkSetup
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                // The received URI is eventArgs.Uri.AbsoluteUri
                string uristr = "(not set)";
                try
                {
                    ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                    // Example: networksetup:context:hotspot;action:start;ssid:myhotspot;p:password;;
                    uristr = eventArgs.Uri.AbsoluteUri;
                    var mecard = MeCardParser.MeCardParser.Parse(uristr);

                    CreateRootFrame("");
                    Frame rootFrame = Window.Current.Content as Frame;

                    if (rootFrame.Content == null)
                    {
                        if (!rootFrame.Navigate(typeof(MainPage)))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }

                    var p = rootFrame.Content as MainPage;
                    await p.NavigateToMeCard(mecard);

                    // Ensure the current window is active
                    Window.Current.Activate();
                }
                catch (Exception ex)
                {
                    Log($"Exception: NavigateToUrl: url={uristr} ex={ex.Message}")
                    ; // Something happened, but we don't know what.
                }
            }
        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        public static void Log(string txt)
        {
            Console.WriteLine(txt);
            System.Diagnostics.Debug.WriteLine(txt);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }


        // From https://learn.microsoft.com/en-us/windows/uwp/launch-resume/reduce-memory-usage
        void CreateRootFrame(string arguments)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                try
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    // Set the default language
                    rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                    rootFrame.NavigationFailed += OnNavigationFailed;


                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }
                catch (Exception ex)
                {
                    Log($"Exception: CreateRootFrame: 10: ex={ex.Message}");
                }
            }

            if (rootFrame.Content == null)
            {
                try
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), arguments);
                }
                catch (Exception ex)
                {
                    Log($"Exception: CreateRootFrame: 20 ex={ex.Message}");
                }
            }
        }
    }
}
