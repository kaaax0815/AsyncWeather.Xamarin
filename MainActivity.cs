using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Syncfusion.Android.ProgressBar;

namespace AsyncWeather.Xamarin
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mzc1OTk1QDMxMzgyZTM0MmUzMEpGUi96NGFrK2xrU0o2emJ1cHpmYm5mZkNqVEpQUEQ0MW1sbHNjUnJaZWs9");
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;
            SfLinearProgressBar sfLinearProgressBar = new SfLinearProgressBar(this)
            {
                LayoutParameters = new RelativeLayout.LayoutParams(
                    this.Resources.DisplayMetrics.WidthPixels,
                    this.Resources.DisplayMetrics.HeightPixels / 18),
                IsIndeterminate = true
            };
            RelativeLayout relativeLayout = FindViewById<RelativeLayout>(Resource.Id.mainlayout);
            relativeLayout.AddView(sfLinearProgressBar);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private async void SetAsyncTextOriginal() // Lags
        {
            Toast.MakeText(ApplicationContext, "Starting", ToastLength.Long).Show();
            await Task.Delay(2000);
            TextView text = FindViewById<TextView>(Resource.Id.text);
            Toast.MakeText(ApplicationContext, "Getting", ToastLength.Long).Show();
            await Task.Delay(2000);
            string content = await Get();
            Toast.MakeText(ApplicationContext, "Setting", ToastLength.Long).Show();
            await Task.Delay(2000);
            text.Text = content; // Lags at this point
        }

        private void SetAsyncText() // Lags too
        {
            RunOnUiThread(async () => FindViewById<TextView>(Resource.Id.text).Text = await Get());
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            //SetAsyncText(); or SetAsyncTextOriginal();
            SetAsyncText();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> Get()
        {
            string url = "https://api.openweathermap.org/data/2.5/onecall?lat=50&lon=10&appid=89f453dd00317568c5655dddece7f2a7";

            // The actual Get method
            using var result1 = await _httpClient.GetAsync($"{url}");
            string content1 = await result1.Content.ReadAsStringAsync();

            // The actual Get method
            using var result2 = await _httpClient.GetAsync($"{url}");
            string content2 = await result2.Content.ReadAsStringAsync();

            // The actual Get method
            using var result3 = await _httpClient.GetAsync($"{url}");
            string content3 = await result3.Content.ReadAsStringAsync();

            // The actual Get method
            using var result4 = await _httpClient.GetAsync($"{url}");
            string content4 = await result4.Content.ReadAsStringAsync();

            // The actual Get method
            using var result5 = await _httpClient.GetAsync($"{url}");
            string content5 = await result5.Content.ReadAsStringAsync();
            // The actual Get method
            using var result6 = await _httpClient.GetAsync($"{url}");
            string content6 = await result6.Content.ReadAsStringAsync();
            // The actual Get method
            using var result7 = await _httpClient.GetAsync($"{url}");
            string content7 = await result7.Content.ReadAsStringAsync();
            // The actual Get method
            using var result8 = await _httpClient.GetAsync($"{url}");
            string content8 = await result8.Content.ReadAsStringAsync();
            // The actual Get method
            using var result9 = await _httpClient.GetAsync($"{url}");
            string content9 = await result9.Content.ReadAsStringAsync();

            return content1 + content2 + content3 + content4 + content5 + content6 + content7 + content8 + content9;
        }
    }
}
