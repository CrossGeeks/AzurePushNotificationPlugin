﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Iid;

namespace Plugin.AzurePushNotification
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class PNIIDService : FirebaseInstanceIdService
    {
        /**
        * Called if InstanceID token is updated. This may occur if the security of
        * the previous token had been compromised. Note that this is called when the InstanceID token
        * is initially generated so this is where you would retrieve the token.
        */
        public override void OnTokenRefresh()
        {
            // Get updated InstanceID token.
            var refreshedToken = FirebaseInstanceId.Instance.Token;

            AzurePushNotificationManager.SaveToken(refreshedToken);
            AzurePushNotificationManager.RegisterToken(refreshedToken);
            System.Diagnostics.Debug.WriteLine($"REFRESHED TOKEN: {refreshedToken}");
        }
    }
}