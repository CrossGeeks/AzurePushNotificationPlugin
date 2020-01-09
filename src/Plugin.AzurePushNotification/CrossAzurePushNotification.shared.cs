using System;

namespace Plugin.AzurePushNotification
{
  /// <summary>
  /// Cross platform AzurePushNotification implemenations
  /// </summary>
  public class CrossAzurePushNotification
  {
    static Lazy<IAzurePushNotification> Implementation = new Lazy<IAzurePushNotification>(() => CreateAzurePushNotification(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Current settings to use
    /// </summary>
    public static IAzurePushNotification Current
    {
      get
      {
        var ret = Implementation.Value;
        if (ret == null)
        {
          throw NotImplementedInReferenceAssembly();
        }
        return ret;
      }
    }

    static IAzurePushNotification CreateAzurePushNotification()
    {
#if NETSTANDARD2_0
            return null;
#else
            return new AzurePushNotificationManager();
#endif
    }

    internal static Exception NotImplementedInReferenceAssembly()
    {
      return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
    }
  }
}
