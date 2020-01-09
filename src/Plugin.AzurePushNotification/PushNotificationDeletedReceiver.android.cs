﻿using System.Collections.Generic;
using Android.App;
using Android.Content;

namespace Plugin.AzurePushNotification
{
    [BroadcastReceiver]
    public class PushNotificationDeletedReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            var extras = intent.Extras;

            if (extras != null && !extras.IsEmpty)
            {
                foreach (var key in extras.KeySet())
                {
                    parameters.Add(key, $"{extras.Get(key)}");
                    System.Diagnostics.Debug.WriteLine(key, $"{extras.Get(key)}");
                }
            }

            AzurePushNotificationManager.RegisterDelete(parameters);
        }
    }
}