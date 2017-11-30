### iOS Specifics Customization

#### Notification Sound

You can set the sound to be played when notification arrive by setting **sound** key on th payload. A sound file with the value set should be in your should be on *Library/Sounds*.

Valid extensions are: aiff, wav, or caf file

 ```json
{
   "aps" : {
        "alert" :  {
            "title" : "hello",
            "body" : "world"
        },
        "sound" : "test.aiff"
    }
}
```
#### Notification on Foreground

You can set UNNotificationPresentationOptions to get an alert, badge, sound when notification is received in foreground by setting static property **PushNotificationManager.CurrentNotificationPresentationOption**. By default is set to UNNotificationPresentationOptions.None.

```csharp
     public enum UNNotificationPresentationOptions
	 {
	 	 Alert,	//Display the notification as an alert, using the notification text.
		 Badge,	//Display the notification badge value in the application's badge.
		 None,	//No options are set.
		 Sound  //Play the notification sound.
	 }
```

Usage sample on iOS Project:

```csharp
   //To set for alert
   PushNotificationManager.CurrentNotificationPresentationOption = UNNotificationPresentationOptions.Alert;

   //You can also combine them
   PushNotificationManager.CurrentNotificationPresentationOption = UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Badge;
```

A good place to do this would be on the **OnReceived** method of a custom push notification handler if it changes depending on the notification, if not you can just set it once on the AppDelegate **FinishLaunching**.

<= Back to [Table of Contents](../README.md)

