using Foundation;

namespace MauiInventory.App;
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    public override void OpenUrlContexts(UIKit.UIScene scene, NSSet<UIKit.UIOpenUrlContext> contexts)
    {
#if IOS
  foreach(var context in contexts)Microsoft.Identity.Client.AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(context.Url);
#else
        base.OpenUrlContexts(scene, contexts);
#endif
    }
}
