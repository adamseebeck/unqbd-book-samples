using Foundation;

namespace MauiInventory.App;
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    public override bool OpenUrl(UIKit.UIApplication application, NSUrl url, NSDictionary options)
    {
#if IOS
  return Microsoft.Identity.Client.AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url);
#else
        return base.OpenUrl(application, url, options);
#endif
    }
}
