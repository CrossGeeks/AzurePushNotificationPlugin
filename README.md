## Azure Push Notification Plugin for Xamarin iOS and Android

[![Build Status](https://dev.azure.com/CrossGeeks/Plugins/_apis/build/status/AzurePushNotification%20Plugin%20CI%20Pipeline?branchName=master)](https://dev.azure.com/CrossGeeks/Plugins/_build/latest?definitionId=10&branchName=master)

Simple cross platform plugin for handling azure notification hub push notifications.

### Setup
* Available on NuGet: http://www.nuget.org/packages/Plugin.AzurePushNotification [![NuGet](https://img.shields.io/nuget/v/Plugin.AzurePushNotification.svg?label=NuGet)](https://www.nuget.org/packages/Plugin.AzurePushNotification/)
* Install into your PCL project and Client projects.

**Platform Support**

|Platform|Version|
| ------------------- | :------------------: |
|Xamarin.iOS|iOS 8+|
|Xamarin.Android|API 15+|

### API Usage

Call **CrossAzurePushNotification.Current** from any project to gain access to APIs.

## Features

- Receive push notifications
- Tag registration
- Support for push notification category actions
- Customize push notifications
- Localization


## Documentation

Here you will find detailed documentation on setting up and using the Azure Push Notification Plugin for Xamarin

* [Getting Started](docs/GettingStarted.md)
* [Receiving Push Notifications](docs/ReceivingNotifications.md)
* [Android Customization](docs/AndroidCustomization.md)
* [iOS Customization](docs/iOSCustomization.md)
* [Notification Category Actions](docs/NotificationActions.md)
* [Notification Localization](docs/LocalizedPushNotifications.md)
* [FAQ](docs/FAQ.md)

#### Contributors

* [Rendy Del Rosario](https://github.com/rdelrosario)
* [Charlin Agramonte](https://github.com/char0394)
* [Alberto Florenzan](https://github.com/aflorenzan)
* [Angel Andres Mañon](https://github.com/AngelAndresM)
* [Tymen Steur](https://github.com/TymenSteur)
* [Mircea-Tiberiu MATEI](https://github.com/matei-tm)
* [Pier-Lionel Sgard](https://github.com/plsgard)
* [Peseur](https://github.com/Peseur)
