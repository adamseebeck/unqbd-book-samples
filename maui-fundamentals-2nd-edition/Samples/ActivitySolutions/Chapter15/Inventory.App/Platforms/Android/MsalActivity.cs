using Android.App;
using Android.Content;
using Android.Content.PM;

namespace MauiInventory.App;
[Activity(Exported = true, LaunchMode = LaunchMode.SingleTask, NoHistory = true)]
[IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataHost = "auth", DataScheme = MsalIdentityClient.CallbackScheme)]
public class MsalActivity : Microsoft.Identity.Client.BrowserTabActivity
{
}
