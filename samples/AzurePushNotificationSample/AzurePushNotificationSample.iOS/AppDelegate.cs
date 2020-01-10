using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using Plugin.AzurePushNotification;
using UIKit;
using UserNotifications;

namespace AzurePushNotificationSample.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());
            AzurePushNotificationManager.CurrentNotificationPresentationOption = UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Sound;

            AzurePushNotificationManager.Initialize(AzureConstants.ListenConnectionString, AzureConstants.NotificationHubName, options);
          
            return base.FinishedLaunching(app, options);

        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            AzurePushNotificationManager.DidRegisterRemoteNotifications(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            AzurePushNotificationManager.RemoteNotificationRegistrationFailed(error);

        }
        // To receive notifications in foreground on iOS 9 and below.
        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {

            AzurePushNotificationManager.DidReceiveMessage(userInfo);
        }
    }
}
