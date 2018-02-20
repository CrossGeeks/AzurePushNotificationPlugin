## Starting with Android

### Android Configuration

Edit AndroidManifest.xml and insert the following receiver elements **inside** the **application** section:

```xml
<receiver 
    android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver" 
    android:exported="false" />
<receiver 
    android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver" 
    android:exported="true" 
    android:permission="com.google.android.c2dm.permission.SEND">
    <intent-filter>
        <action android:name="com.google.android.c2dm.intent.RECEIVE" />
        <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
        <category android:name="${applicationId}" />
    </intent-filter>
</receiver>
```
Also add this permission:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

Add google-services.json to Android project. Make sure build action is GoogleServicesJson

![ADD JSON](https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/images/android-googleservices-json.png?raw=true)

Must compile against 26+ as plugin is using API 26 specific things. Here is a great breakdown: http://redth.codes/such-android-api-levels-much-confuse-wow/ (Android project must be compiled using 8.0+ target framework)

### Android Initialization

You should initialize the plugin on an Android Application class if you don't have one on your project, should create an application class. Then call **AzurePushNotificationManager.Initialize** method on OnCreate.

There are 3 overrides to **AzurePushNotificationManager.Initialize**:

- **AzurePushNotificationManager.Initialize(Context context,string notificationHubConnectionString,string notificationHubPathName, bool resetToken,bool autoRegistration)** : Default method to initialize plugin without supporting any user notification categories. Uses a DefaultPushHandler to provide the ui for the notification.

- **AzurePushNotificationManager.Initialize(Context context,string notificationHubConnectionString,string notificationHubPathName, NotificationUserCategory[] categories, bool resetToken,bool autoRegistration)**  : Initializes plugin using user notification categories. Uses a DefaultPushHandler to provide the ui for the notification supporting buttons based on the action_click send on the notification

- **AzurePushNotificationManager.Initialize(Context context,string notificationHubConnectionString,string notificationHubPathName,IPushNotificationHandler pushHandler, bool resetToken,bool autoRegistration)** : Initializes the plugin using a custom push notification handler to provide custom ui and behaviour notifications receipt and opening.

**Important: While debugging set resetToken parameter to true.**

Example of initialization:

```csharp

    [Application]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transer) :base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            //If debug you should reset the token each time.
            #if DEBUG
              AzurePushNotificationManager.Initialize(this,"Notification Hub Connection String","Notification Hub Path Name",true);
            #else
              AzurePushNotificationManager.Initialize(this,"Notification Hub Connection String","Notification Hub Path Name",false);
            #endif

              //Handle notification when app is closed here
              CrossAzurePushNotification.Current.OnNotificationReceived += (s,p) =>
              {


              };

			//Set the default notification channel for your app when running Android Oreo
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                //Change for your default notification channel id here
                AzurePushNotificationManager.DefaultNotificationChannelId = "DefaultChannel";

                //Change for your default notification channel name here
                AzurePushNotificationManager.DefaultNotificationChannelName = "General";
            }
         }
    }

```

By default the plugin launches the main launcher activity when you tap at a notification, but you can change this behaviour by setting the type of the activity you want to be launch on **AzurePushNotificationManager.NotificationActivityType**

If you set **AzurePushNotificationManager.NotificationActivityType** then put the following call on the **OnCreate** of activity of the type set. If not set then put it on your main launcher activity **OnCreate** method (On the Activity you got MainLauncher= true set)

```csharp
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

			//Other initialization stuff

            AzurePushNotificationManager.ProcessIntent(this,Intent);
        }

 ```

**Note: When using Xamarin Forms do it just after LoadApplication call.**

By default the plugin launches the activity where **ProcessIntent** method is called when you tap at a notification, but you can change this behaviour by setting the type of the activity you want to be launch on *PushNotificationManager.NotificationActivityType**

You can change this behaviour by setting **AzurePushNotificationManager.NotificationActivityFlags**. 
 
If you set **AzurePushNotificationManager.NotificationActivityFlags** to ActivityFlags.SingleTop  or using default plugin behaviour then make this call on **OnNewIntent** method of the same activity on the previous step.
       
 ```csharp
	    protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            AzurePushNotificationManager.ProcessIntent(this,intent);
        }
 ```

 More information on **AzurePushNotificationManager.NotificationActivityType** and **AzurePushNotificationManager.NotificationActivityFlags** and other android customizations here:

 [Android Customization](../docs/AndroidCustomization.md)

## Starting with iOS 

### iOS Configuration

On Info.plist enable remote notification background mode

![Remote notifications](https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/images/iOS-enable-remote-notifications.png?raw=true)

### iOS Initialization

There are 3 overrides to **AzurePushNotificationManager.Initialize**:

- **AzurePushNotificationManager.Initialize(string notificationHubConnectionString,string notificationHubPathName, NSDictionary options,bool autoRegistration,bool autoRegistration)** : Default method to initialize plugin without supporting any user notification categories. Auto registers for push notifications if second parameter is true.

- **AzurePushNotificationManager.Initialize(string notificationHubConnectionString,string notificationHubPathName,NSDictionary options, NotificationUserCategory[] categories,bool autoRegistration)**  : Initializes plugin using user notification categories to support iOS notification actions.

- **AzurePushNotificationManager.Initialize(string notificationHubConnectionString,string notificationHubPathName,NSDictionary options,IPushNotificationHandler pushHandler,bool autoRegistration)** : Initializes the plugin using a custom push notification handler to provide native feedback of notifications event on the native platform.


Call  **AzurePushNotificationManager.Initialize** on AppDelegate FinishedLaunching
```csharp

AzurePushNotificationManager.Initialize("Notification Hub Connection String","Notification Hub Path Name",options,true);

```
 **Note: When using Xamarin Forms do it just after LoadApplication call.**

Also should override these methods and make the following calls:
```csharp
        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
             AzurePushNotificationManager.DidRegisterRemoteNotifications(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            AzurePushNotificationManager.RemoteNotificationRegistrationFailed(error);

        }
        // To receive notifications in foregroung on iOS 9 and below.
        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            
            AzurePushNotificationManager.DidReceiveMessage(userInfo);
        }

      
```


## Using Push Notification APIs
It is drop dead simple to gain access to the PushNotification APIs in any project. All you need to do is get a reference to the current instance of IPushNotification via `CrossPushNotification.Current`:

Register tags

```csharp
   /// <summary>
   /// Registers tags in Azure Notification hub
   /// </summary>
    await CrossAzurePushNotification.Current.RegisterAsync(new string[]{"crossgeeks","general"});
```

Note: The method above cleans all previous registered tags when called. So it kind of replaces the tags each time you call it.

Unregister tags

```csharp
   /// <summary>
   /// Unregister all tags in Azure Notification hub
   /// </summary>
    await CrossAzurePushNotification.Current.UnregisterAsync();
```

### On Demand Registration

When plugin initializes by default auto request permission the device for push notifications. If needed you can do on demand permisssion registration by turning off auto registration when initializing the plugin.

Use the following method for on demand permission registration:

```csharp
   CrossAzurePushNotification.Current.RegisterForPushNotifications();
```


### Events

Once token is registered/refreshed you will get it on **OnTokenRefresh** event.


```csharp
   /// <summary>
   /// Event triggered when token is refreshed
   /// </summary>
    event AzurePushNotificationTokenEventHandler OnTokenRefresh;
```

Note: Don't call **RegisterAsync** in the event above because it is called automatically each time the token changes

```csharp        
  /// <summary>
  /// Event triggered when a notification is received
  /// </summary>
  event AzurePushNotificationResponseEventHandler OnNotificationReceived;
```


```csharp        
  /// <summary>
  /// Event triggered when a notification is opened
  /// </summary>
  event AzurePushNotificationResponseEventHandler OnNotificationOpened;
```

```csharp 
   /// <summary>
   /// Event triggered when a notification is deleted (Android Only)
   /// </summary>
   event AzurePushNotificationDataEventHandler OnNotificationDeleted;
```

```csharp        
  /// <summary>
  /// Event triggered when there's an error
  /// </summary>
  event AzurePushNotificationErrorEventHandler OnNotificationError;
```

Token event usage sample:
```csharp

  CrossAzurePushNotification.Current.OnTokenRefresh += (s,p) =>
  {
        System.Diagnostics.Debug.WriteLine($"TOKEN : {p.Token}");
  };

```

Push message received event usage sample:
```csharp

  CrossAzurePushNotification.Current.OnNotificationReceived += (s,p) =>
  {
 
        System.Diagnostics.Debug.WriteLine("Received");
    
  };

```

Push message opened event usage sample:
```csharp
  
  CrossAzurePushNotification.Current.OnNotificationOpened += (s,p) =>
  {
                System.Diagnostics.Debug.WriteLine("Opened");
                foreach(var data in p.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"{data.Key} : {data.Value}");
                }

                if(!string.IsNullOrEmpty(p.Identifier))
                {
                    System.Diagnostics.Debug.WriteLine($"ActionId: {p.Identifier}");
                }
             
 };
```
Push message deleted event usage sample: (Android Only)
```csharp

  CrossAzurePushNotification.Current.OnNotificationDeleted+= (s,p) =>
  {
 
        System.Diagnostics.Debug.WriteLine("Deleted");
    
  };

```

Plugin by default provides some notification customization features for each platform. Check out the [Android Customization](AndroidCustomization.md) and [iOS Customization](iOSCustomization.md) sections.

<= Back to [Table of Contents](../README.md)
