using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Text;
using Xamarin.Essentials;

namespace AsyncWeather.Xamarin
{
    [Activity(Label = "@string/settings", Theme = "@style/AppTheme.NoActionBar", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class Settings : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.content_settings);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.settings_toolbar);
            SetSupportActionBar(toolbar);
            TableRow weatherlayout = FindViewById<TableRow>(Resource.Id.weatherlayout);
            TableRow locationlayout = FindViewById<TableRow>(Resource.Id.locationlayout);
            TableRow cachelayout = FindViewById<TableRow>(Resource.Id.cachelayout);
            LinearLayout devlayout = FindViewById<LinearLayout>(Resource.Id.devlayout);
            weatherlayout.Click += Weatherlayout_Click;
            locationlayout.Click += Locationlayout_Click;
            cachelayout.Click += Cachelayout_Click;
            devlayout.Click += Devlayout_Click;
        }

        private async void Devlayout_Click(object sender, EventArgs e)
        {
            try
            {
                string url = "https://landing.kaaaxcreators.de";
                await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
            {
                Toast.MakeText(ApplicationContext, "Cant open Browser", ToastLength.Long).Show();
            }
        }

        private void Cachelayout_Click(object sender, EventArgs e)
        {
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.userinput_settings, null);
            Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
            alertbuilder.SetView(view);
            alertbuilder.SetTitle(GetString(Resource.String.cacheduration));
            EditText userdata = view.FindViewById<EditText>(Resource.Id.dialogText);
            userdata.Hint = Preferences.Get("cachehr", $"3 ({GetString(Resource.String.defaultvalue)})").ToString(); // Check if Key exists if not show "3"
            alertbuilder.SetCancelable(false)
            .SetPositiveButton(GetString(Resource.String.submit), delegate
            {
                Preferences.Set("cachehr", Convert.ToInt32(userdata.Text));
            })
            .SetNegativeButton(GetString(Resource.String.cancel), delegate
            {
                alertbuilder.Dispose();
            })
            .SetNeutralButton(GetString(Resource.String.reset), delegate
             {
                 Preferences.Remove("cachehr");
             });
            Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();
            dialog.Show();
        }

        private void Locationlayout_Click(object sender, EventArgs e)
        {
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.userinput_settings, null);
            Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
            alertbuilder.SetView(view);
            alertbuilder.SetTitle(GetString(Resource.String.locationapi));
            EditText userdata = view.FindViewById<EditText>(Resource.Id.dialogText);
            userdata.Hint = FromBase64(Preferences.Get("locationiq", ToBase64(GetString(Resource.String.hidden)))); // Check if Key exists if not show "**hidden**"
            alertbuilder.SetCancelable(false)
            .SetPositiveButton(GetString(Resource.String.submit), delegate
            {
                Preferences.Set("locationiq", ToBase64(userdata.Text.ToString()));
            })
            .SetNegativeButton(GetString(Resource.String.cancel), delegate
            {
                alertbuilder.Dispose();
            })
            .SetNeutralButton(GetString(Resource.String.reset), delegate
            {
                Preferences.Remove("locationiq");
            });
            Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();
            dialog.Show();
        }

        private void Weatherlayout_Click(object sender, EventArgs e)
        {
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.userinput_settings, null);
            Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
            alertbuilder.SetView(view);
            alertbuilder.SetTitle(GetString(Resource.String.weatherapi));
            EditText userdata = view.FindViewById<EditText>(Resource.Id.dialogText);
            userdata.Hint = FromBase64(Preferences.Get("openweathermap", ToBase64(GetString(Resource.String.hidden)))); // Check if Key exists if not show "**hidden**" ( convert to base64 just to decode it again lul
            alertbuilder.SetCancelable(false)
            .SetPositiveButton(GetString(Resource.String.submit), delegate
            {
                Preferences.Set("openweathermap", ToBase64(userdata.Text.ToString()));
            })
            .SetNegativeButton(GetString(Resource.String.cancel), delegate
            {
                alertbuilder.Dispose();
            })
            .SetNeutralButton(GetString(Resource.String.reset), delegate
            {
                Preferences.Remove("openweathermap");
            });
            Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();
            dialog.Show();
        }

        // Base64 Conversion from here: https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        public string ToBase64(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public string FromBase64(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }
    }
}