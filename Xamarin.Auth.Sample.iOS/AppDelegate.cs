using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Threading.Tasks;
using MonoTouch.Dialog;
using Mojio.Client;

#if __UNIFIED__
using Foundation;
using UIKit;


#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Xamarin.Auth.Sample.iOS
{
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        void LoginToMojio (bool allowCancel)
        {
            var auth = new OAuth2Authenticator (
                           clientId: "f201b929-d28c-415d-9b71-8112532301cb",
                           scope: "full",
                           authorizeUrl: new Uri ("https://api.moj.io//OAuth2/authorize"),
                           redirectUrl: new Uri ("https://api.moj.io"));

            auth.AllowCancel = allowCancel;

            // If authorization succeeds or is canceled, .Completed will be fired.
            auth.Completed += (s, e) => {
                // We presented the UI, so it's up to us to dismiss it.
                dialog.DismissViewController (true, null);

                if (!e.IsAuthenticated) {
                    mojioStatus.Caption = "Not authorized";
                    dialog.ReloadData ();
                    return;
                }
                mojioStatus.Caption = "Authorized";
                dialog.ReloadData ();

                MojioClient client = new MojioClient (new Guid ("f201b929-d28c-415d-9b71-8112532301cb"), 
                                         new Guid ("2ef80a7a-780d-41c1-8a02-13a286f11a23"), 
                                         new Guid (e.Account.Properties ["access_token"]),
                                         MojioClient.Live // or MojioClient.Live
                                     );
                var task = client.GetCurrentUserAsync ();
                task.ContinueWith (t => {
                    if (t.IsFaulted)
                        mojioStatus.Caption = "Error: " + t.Exception.InnerException.Message;
                    else if (t.IsCanceled)
                        mojioStatus.Caption = "Canceled";
                    else {
                        mojioStatus.Caption = "Logged in email: " + t.Result.Email;
                    }
                    dialog.ReloadData ();
                }, uiScheduler);
            };

            UIViewController vc = auth.GetUI ();
            dialog.PresentViewController (vc, true, null);
        }

        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            mojio = new Section ("Mojio");
            mojio.Add (new StyledStringElement ("Log in", () => LoginToMojio (true)));			
            mojio.Add (new StyledStringElement ("Log in (no cancel)", () => LoginToMojio (false)));
            mojio.Add (mojioStatus = new StringElement (String.Empty));

            dialog = new DialogViewController (new RootElement ("Xamarin.Auth Sample") {
                mojio,
            });

            window = new UIWindow (UIScreen.MainScreen.Bounds);
            window.RootViewController = new UINavigationController (dialog);
            window.MakeKeyAndVisible ();
			
            return true;
        }

        private readonly TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext ();

        UIWindow window;
        DialogViewController dialog;

        Section mojio;
        StringElement mojioStatus;

        // This is the main entry point of the application.
        static void Main (string[] args)
        {
            UIApplication.Main (args, null, "AppDelegate");
        }
    }
}

