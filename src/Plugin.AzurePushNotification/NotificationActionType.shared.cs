﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.AzurePushNotification
{
    public enum NotificationActionType
    {
        Default,  
        AuthenticationRequired, //Only applies for iOS
        Foreground,
        Destructive  //Only applies for iOS
    }
}
