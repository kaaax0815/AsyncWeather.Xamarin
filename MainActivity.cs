using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Text;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using Com.Syncfusion.Charts;
using Newtonsoft.Json;
using Plugin.Geolocator;
using Square.Picasso;
using Syncfusion.Android.ProgressBar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace AsyncWeather.Xamarin
{
    /// <summary>
    /// Error Codes: 199 = Permission; 505 = Generic; 594 = Feature; 250 = Try Again; 456 = HTTP 401 Unauthorized
    /// HTML Codes: hXXX = XXX HTML Error
    /// "From here:" doesnt mean copied, but rather adapted. I understand all of my code, and nothing is just copy paste -> done
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity : AppCompatActivity
    {
        SfLinearProgressBar sfLinearProgressBar { get; set; }
        readonly string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string openweathermap;
        readonly string syncfusionlicense = "TXpjMU9UazFRRE14TXpneVpUTTBNbVV6TUVwR1VpOTZOR0ZySzJ4clUwbzJlbUoxY0hwbVltNW1aa05xVkVwUVVFUTBNVzFzYkhOalVuSmFaV3M5";
        string locationiq;
        int cachehr; // Default Cache Duration
        readonly long hto100ns = 36000000000; // hours to 100th nanosecond (windows filetime)
        OneClickApi oneClickApi { get; set; }
        ReverseGeocoding reverseGeocoding { get; set; }
        List<ForwardGeocoding> forwardGeocoding { get; set; }
        bool search_clicked = false;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Encoding.UTF8.GetString(Convert.FromBase64String(syncfusionlicense)));
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SetPreferences();
            FindViewById<LinearLayout>(Resource.Id.forecast_1_layout).Click += Forecast1_Click;
            FindViewById<LinearLayout>(Resource.Id.forecast_2_layout).Click += Forecast2_Click;
            FindViewById<LinearLayout>(Resource.Id.forecast_3_layout).Click += Forecast3_Click;
            FindViewById<LinearLayout>(Resource.Id.forecast_4_layout).Click += Forecast4_Click;
            FindViewById<LinearLayout>(Resource.Id.forecast_5_layout).Click += Forecast5_Click;
            FindViewById<LinearLayout>(Resource.Id.forecast_6_layout).Click += Forecast6_Click;
            FindViewById<LinearLayout>(Resource.Id.forecast_7_layout).Click += Forecast7_Click;
            FindViewById<Button>(Resource.Id.nointernet_retry).Click += Retry_Click;
            FindViewById<EditText>(Resource.Id.autoCompleteTextView1).EditorAction += MainActivity_EditorAction;
            string[] COUNTRIES = { "Wülfershausen", "Kassel", "Berlin" };
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Resource.Layout.list_item, COUNTRIES);
            AutoCompleteTextView textView = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteTextView1);
            textView.Adapter = adapter;
            InitProgressbar();
            if (IsOnline())
            {
                FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Gone;
                if (Preferences.ContainsKey("offline_time"))
                {
                    long l = 0;
                    long longoffline_time = 0;
                    try
                    {
                        longoffline_time = Preferences.Get("offline_time", l);
                    }
                    catch (Java.Lang.ClassCastException)
                    {
                        Preferences.Clear();
                        ShowMessageBox("Old Configuration", "Restart to get Weather");
                        return;
                    }
                    DateTime offline_time = DateTime.FromFileTimeUtc(longoffline_time).ToLocalTime();
                    if (offline_time.ToFileTime() <= DateTime.Now.ToFileTime() - (cachehr * hto100ns)) // Check if "offline_time" is longer than "cachehr" hours ago
                    {
                        OnlineWeather();
                    }
                    else
                    {
                        OfflineWeather();
                    }
                }
                else
                {
                    OnlineWeather();
                }
            }
            else
            {
                FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Visible;
                OfflineWeather();
            }
        }

        private void MainActivity_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Search) // If keyboard search button pressed, get text and search for city
            {
                SearchWeather(FindViewById<EditText>(Resource.Id.autoCompleteTextView1).Text);
                FindViewById<LinearLayout>(Resource.Id.search_layout).Visibility = ViewStates.Gone;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void Retry_Click(object sender, EventArgs e)
        {
            if (IsOnline())
            {
                FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Gone;
                OnlineWeather();
            }
            else
            {
                FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Visible;
                OfflineWeather();
            }
        }

        private void Forecast7_Click(object sender, EventArgs e)
        {
            int i = 7;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        private void Forecast6_Click(object sender, EventArgs e)
        {
            int i = 6;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        private void Forecast5_Click(object sender, EventArgs e)
        {
            int i = 5;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        private void Forecast4_Click(object sender, EventArgs e)
        {
            int i = 4;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        private void Forecast3_Click(object sender, EventArgs e)
        {
            int i = 3;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        private void Forecast2_Click(object sender, EventArgs e)
        {
            int i = 2;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        private void Forecast1_Click(object sender, EventArgs e)
        {
            int i = 1;
            if (oneClickApi != null)
            {
                FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.morn.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.day.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.eve.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[i].temp.night.ToString() + "°C";
                FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[i].feels_like.night.ToString() + "°C";
            }
        }

        // Boilerplate
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
                StartActivity(typeof(Settings)); // Open Settings
                return true;
            }
            if (id == Resource.Id.action_refresh)
            {
                SetPreferences(); // Update Preferences
                if (IsOnline())
                {
                    FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Gone;
                    OnlineWeather();
                }
                else
                {
                    FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Visible;
                    OfflineWeather();
                }
                return true;
            }
            if (id == Resource.Id.action_search) // Show Search layout, if pressed again hide
            {
                if (search_clicked)
                {
                    FindViewById<LinearLayout>(Resource.Id.search_layout).Visibility = ViewStates.Gone;
                    search_clicked = false;
                    if (IsOnline())
                    {
                        FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Gone;
                        OnlineWeather();
                    }
                    else
                    {
                        FindViewById<LinearLayout>(Resource.Id.nonetwork_layout).Visibility = ViewStates.Visible;
                        OfflineWeather();
                    }
                    return true;
                }
                FindViewById<LinearLayout>(Resource.Id.search_layout).Visibility = ViewStates.Visible;
                FindViewById<EditText>(Resource.Id.autoCompleteTextView1).RequestFocus();
                search_clicked = true;

            }
            return base.OnOptionsItemSelected(item);
        }

        // Boilerplate
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        // From here: https://docs.microsoft.com/de-de/xamarin/essentials/connectivity?tabs=android
        public bool IsOnline()
        {
            NetworkAccess current = Connectivity.NetworkAccess;
            return current == NetworkAccess.Internet; // If Network equals to Internet return true
        }

        // Get and Returns Location or Error
        public async Task<double[]> GetLocation()
        {
            try
            {
                Plugin.Geolocator.Abstractions.IGeolocator locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 200;
                Plugin.Geolocator.Abstractions.Position loc = await locator.GetPositionAsync();

                if (loc != null)
                {
                    // Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}
                    double[] location = { loc.Latitude, loc.Longitude };
                    return location;
                }
                else
                {
                    double[] double_again = { 250, 0 };
                    return double_again;
                }
            }
            catch (FeatureNotEnabledException)
            {
                // Open Location Setting
                ShowMessageBox(GetString(Resource.String.location_error), GetString(Resource.String.featurenotenabled));
                StartActivity(new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings));
                double[] double_feature = { 594, 0 };
                return double_feature;
            }
            catch (Plugin.Geolocator.Abstractions.GeolocationException)
            {
                // No Permissions
                try
                {
                    PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    double[] double_again = { 250, 0 };
                    return double_again;
                }
                catch (PermissionException)
                {
                    // Still no Permissions
                    Toast.MakeText(Application.Context, "The App needs Permission to function", ToastLength.Long).Show();
                    double[] double_permission = { 199, 0 };
                    return double_permission;
                }
            }
            catch (Exception loce)
            {
                string response = await Post(loce.ToString());
                ShowMessageBox(GetString(Resource.String.location_error), GetString(Resource.String.location_error_dev) + "<a href=\"" + response + "\">" + response + "</a>");
                double[] double_generic = { 505, 0 };
                return double_generic;
            }
        }

        public async Task<string> GetWeather(double[] loc, string lang, string key)
        {
            try
            {
                double lat = loc[0];
                double lon = loc[1];
                string weather = await Get("https://api.openweathermap.org/data/2.5/onecall?lat=" + lat + "&lon=" + lon + "&lang=" + lang + "&appid=" + key + "&units=metric");
                return weather;
            }
            catch (Exception weathere)
            {
                string response = await Post(weathere.ToString());
                ShowMessageBox(GetString(Resource.String.requesterror), GetString(Resource.String.requesterror_dev) + "<a href=\"" + response + "\">" + response + "</a>");
                return "h500";
            }
        }

        public async Task<string> GetReverseLocation(double[] loc, string key)
        {
            try
            {
                string lat = loc[0].ToString();
                string lon = loc[1].ToString();
                string reverse = await Get("https://us1.locationiq.com/v1/reverse.php?key=" + key + "&lat=" + lat.Replace(",", ".") + "&lon=" + lon.Replace(",", ".") + "&format=json");
                return reverse;
            }
            catch (Exception reverseloce)
            {
                string response = await Post(reverseloce.ToString());
                ShowMessageBox(GetString(Resource.String.requesterror), GetString(Resource.String.requesterror_dev) + "<a href=\"" + response + "\">" + response + "</a>");
                return "h500";
            }
        }

        public async Task<string> GetForwardLocation(string search, string key)
        {
            try
            {
                string forward = await Get($"https://us1.locationiq.com/v1/search.php?key={key}&q={search}&accept-language={lang}&format=json");
                return forward;
            }
            catch (Exception forwardloce)
            {
                string response = await Post(forwardloce.ToString());
                ShowMessageBox(GetString(Resource.String.requesterror), GetString(Resource.String.requesterror_dev) + "<a href=\"" + response + "\">" + response + "</a>");
                return "h500";
            }
        }

        // From here: https://briancaos.wordpress.com/2017/11/03/using-c-httpclient-from-sync-and-async-code/
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> Get(string url)
        {
            try
            {
                using HttpResponseMessage result1 = await _httpClient.GetAsync($"{url}");
                if (result1.IsSuccessStatusCode)
                {
                    string content = await result1.Content.ReadAsStringAsync();
                    return content;
                }
                else if (result1.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "456";
                }
                return "250";
            }
            catch (Exception)
            {
                return "250";
            }
        }

        // From here: https://briancaos.wordpress.com/2017/11/03/using-c-httpclient-from-sync-and-async-code/ and https://stackoverflow.com/questions/4015324/how-to-make-an-http-post-web-request
        public static async Task<string> Post(string content)
        {
            Dictionary<string, string> values = new Dictionary<string, string>
                    {
                        { "text", content.ToString() },
                        { "private", "1" },
                        { "lang", "java" }
                    };
            // Send Error to Nopaste
            FormUrlEncodedContent form = new FormUrlEncodedContent(values);
            using HttpResponseMessage result1 = await _httpClient.PostAsync("https://nopaste.chaoz-irc.net/api/create", form);
            string response = await result1.Content.ReadAsStringAsync();
            return response;
        }

        public void InitProgressbar() // Init Syncfusion Progressbar https://help.syncfusion.com/xamarin-android/progressbar/overview
        {
            RelativeLayout relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);
            sfLinearProgressBar = new SfLinearProgressBar(this)
            {
                LayoutParameters = new RelativeLayout.LayoutParams(
                this.Resources.DisplayMetrics.WidthPixels,
                this.Resources.DisplayMetrics.HeightPixels / 18),
                IsIndeterminate = true
            };
            relativeLayout.AddView(sfLinearProgressBar);
            sfLinearProgressBar.TrackColor = Android.Graphics.Color.ParseColor(GetString(Resource.Color.darkmode));
            sfLinearProgressBar.Visibility = ViewStates.Gone;
        }

        public void StartProgresbar()
        {
            sfLinearProgressBar.Visibility = ViewStates.Visible;
        }

        public void StopProgressbar()
        {
            sfLinearProgressBar.Visibility = ViewStates.Gone;
        }

        public async void OnlineWeather()
        {
            try
            {
                StartProgresbar();
                double[] loc = await GetLocation();
                if (loc[0] > 180) { return; }
                string weather = await GetWeather(loc, lang, Encoding.UTF8.GetString(Convert.FromBase64String(openweathermap)));
                if (weather == "456") // If Bad API Quit and Reset
                {
                    Toast.MakeText(ApplicationContext, "Bad API Key. Reseting...", ToastLength.Long).Show();
                    Preferences.Remove("openweathermap");
                    SetPreferences();
                    OnlineWeather();
                    return;
                }
                string reverse = await GetReverseLocation(loc, Encoding.UTF8.GetString(Convert.FromBase64String(locationiq)));
                if (reverse == "456") // If Bad API Quit and Reset
                {
                    Toast.MakeText(ApplicationContext, "Bad API Key. Reseting...", ToastLength.Long).Show();
                    Preferences.Remove("locationiq");
                    SetPreferences();
                    OnlineWeather();
                    return;
                }
                DeserializeWeather(weather);
                DeserializeReverse(reverse);
                SavePreferences(weather, reverse);
                SetText();
                SetChart();
                SetAlerts();
                StopProgressbar();
            }
            catch (Exception onlinee)
            {
                string response = await Post(onlinee.ToString());
                ShowMessageBox("ERROR", GetString(Resource.String.requesterror_dev) + " <a href=\"" + response + "\">" + response + "</a>");
            }
        }

        public void SavePreferences(string content, string iqcontent)
        {
            DateTime thisTime = DateTime.Now;
            // weather data
            Preferences.Set("offline_weather", content);
            // reverse geocoding
            Preferences.Set("offline_loc", iqcontent);
            Preferences.Set("offline_time", thisTime.ToFileTimeUtc());
        }

        public void OfflineWeather() // Get Weather from Cache (Preferences)
        {
            long l = 0;
            FindViewById<TextView>(Resource.Id.lastupdatetxt).Text = GetString(Resource.String.lastupdate) + "\n" + DateTime.FromFileTimeUtc(Preferences.Get("offline_time", l)).ToLocalTime().ToString("g");
            StartProgresbar();
            string weather = Preferences.Get("offline_weather", "");
            string reverse = Preferences.Get("offline_loc", "");
            DeserializeWeather(weather);
            DeserializeReverse(reverse);
            SetText();
            SetChart();
            SetAlerts();
            StopProgressbar();
        }

        public async void SearchWeather(string search)
        {
            StartProgresbar();
            string location = await GetForwardLocation(search, Encoding.UTF8.GetString(Convert.FromBase64String(locationiq)));
            if (location == "456") // If Bad API Quit and Reset
            {
                Toast.MakeText(ApplicationContext, "Bad API Key. Reseting...", ToastLength.Long).Show();
                Preferences.Remove("locationiq");
                SetPreferences();
                SearchWeather(search);
                return;
            }
            DeserializeForward(location);
            double[] searchloc = { forwardGeocoding[0].lat, forwardGeocoding[0].lon };
            string weather = await GetWeather(searchloc, lang, Encoding.UTF8.GetString(Convert.FromBase64String(openweathermap)));
            if (weather == "456") // If Bad API Quit and Reset
            {
                Toast.MakeText(ApplicationContext, "Bad API Key. Reseting...", ToastLength.Long).Show();
                Preferences.Remove("openweathermap");
                SetPreferences();
                SearchWeather(search);
                return;
            }
            DeserializeWeather(weather);
            SetText("forward"); // Set Text in "forward" mode
            SetChart();
            SetAlerts();
            StopProgressbar();
        }

        public void DeserializeWeather(string json)
        {
            oneClickApi = JsonConvert.DeserializeObject<OneClickApi>(json);
        }

        public void DeserializeReverse(string json)
        {
            reverseGeocoding = JsonConvert.DeserializeObject<ReverseGeocoding>(json);
        }

        public void DeserializeForward(string json)
        {
            forwardGeocoding = JsonConvert.DeserializeObject<List<ForwardGeocoding>>(json);
        }

        // From here: https://riptutorial.com/xamarin-android/example/17207/simple-alert-dialog-example
        public void ShowMessageBox(string title, string message)
        {
            TextView textview = new TextView(this)
            {
                TextFormatted = HtmlCompat.FromHtml(message, HtmlCompat.FromHtmlModeLegacy)
            };
            textview.SetPadding(32, 8, 32, 0);
            textview.MovementMethod = LinkMovementMethod.Instance;
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetCancelable(false);
            alert.SetView(textview);
            alert.SetIcon(Resource.Drawable.main_warning);
            alert.SetPositiveButton(GetString(Resource.String.ok), (senderAlert, args) =>
            {
                sfLinearProgressBar.Visibility = ViewStates.Gone;
                return;
            });
            Dialog dialog = alert.Create();
            dialog.Show();
        }

        public async void SetText(string mode = "")
        {
            await Task.Delay(1000);
            // Current Weather
            if (mode == "forward")
            {
                FindViewById<TextView>(Resource.Id.city_txt).Text = forwardGeocoding[0].display_name;
            }
            else
            {
                if (reverseGeocoding.address.city != null)
                {
                    FindViewById<TextView>(Resource.Id.city_txt).Text = reverseGeocoding.address.city;
                }
                else if (reverseGeocoding.address.village != null)
                {
                    FindViewById<TextView>(Resource.Id.city_txt).Text = reverseGeocoding.address.village;
                }
            }
            DateTime thisDay = DateTime.Today;
            FindViewById<TextView>(Resource.Id.date_txt).Text = thisDay.ToString("D");
            string url = "https://openweathermap.org/img/wn/" + oneClickApi.current.weather[0].icon + "@4x.png";
            Picasso.Get().Load(url).Into(FindViewById<ImageView>(Resource.Id.weather_img));
            FindViewById<TextView>(Resource.Id.temp_txt).Text = oneClickApi.current.temp + "°C";
            FindViewById<TextView>(Resource.Id.feelslike_txt).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.current.feels_like + "°C";
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            FindViewById<TextView>(Resource.Id.sunrise_txt).Text = dtDateTime.AddSeconds(oneClickApi.current.sunrise).ToLocalTime().ToString("g");
            FindViewById<TextView>(Resource.Id.sunset_txt).Text = dtDateTime.AddSeconds(oneClickApi.current.sunset).ToLocalTime().ToString("g");
            FindViewById<TextView>(Resource.Id.humidity_txt).Text = oneClickApi.current.humidity + "%";
            FindViewById<TextView>(Resource.Id.pressure_txt).Text = oneClickApi.current.pressure + "hPa";
            FindViewById<TextView>(Resource.Id.speed_txt).Text = oneClickApi.current.wind_speed + "m/s";
            FindViewById<TextView>(Resource.Id.direction_txt).Text = oneClickApi.current.wind_deg + "°";
            if (oneClickApi.current.rain != null && oneClickApi.current.rain._1h > 0.01)
            {
                FindViewById<RelativeLayout>(Resource.Id.rain_layout).Visibility = ViewStates.Visible;
                FindViewById<TextView>(Resource.Id.rain_txt).Text = " " + oneClickApi.current.rain._1h + GetString(Resource.String.rain) + " " + " 1h";
            }
            else if (oneClickApi.current.snow != null && oneClickApi.current.snow._1h > 0.01)
            {
                FindViewById<RelativeLayout>(Resource.Id.rain_layout).Visibility = ViewStates.Visible;
                FindViewById<TextView>(Resource.Id.rain_txt).Text = " " + oneClickApi.current.snow._1h + GetString(Resource.String.snow) + " " + " 1h";
            }
            //Today Info
            FindViewById<TextView>(Resource.Id.today_temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[0].temp.morn.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[0].feels_like.morn.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[0].temp.day.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[0].feels_like.day.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[0].temp.eve.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[0].feels_like.eve.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[0].temp.night.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.today_feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[0].feels_like.night.ToString() + "°C";
            // Forecast
            FindViewById<TextView>(Resource.Id.forecast1_date).Text = GetString(Resource.String.tomorrow) + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d");
            string forecast1_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[1].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast1_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast1_img));
            FindViewById<TextView>(Resource.Id.forecast1_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[1].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast1_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[1].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast1_pop).Text = oneClickApi.daily[1].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast2_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("dddd") + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d");
            string forecast2_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[2].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast2_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast2_img));
            FindViewById<TextView>(Resource.Id.forecast2_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[2].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast2_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[2].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast2_pop).Text = oneClickApi.daily[2].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast3_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("dddd") + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d");
            string forecast3_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[3].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast3_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast3_img));
            FindViewById<TextView>(Resource.Id.forecast3_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[3].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast3_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[3].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast3_pop).Text = oneClickApi.daily[3].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast4_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("dddd") + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d");
            string forecast4_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[4].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast4_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast4_img));
            FindViewById<TextView>(Resource.Id.forecast4_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[4].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast4_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[4].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast4_pop).Text = oneClickApi.daily[4].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast5_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("dddd") + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d");
            string forecast5_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[5].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast5_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast5_img));
            FindViewById<TextView>(Resource.Id.forecast5_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[5].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast5_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[5].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast5_pop).Text = oneClickApi.daily[5].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast6_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("dddd") + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d");
            string forecast6_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[6].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast6_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast6_img));
            FindViewById<TextView>(Resource.Id.forecast6_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[6].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast6_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[6].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast6_pop).Text = oneClickApi.daily[6].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast7_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("dddd") + "\n" + dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d");
            string forecast7_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[7].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast7_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast7_img));
            FindViewById<TextView>(Resource.Id.forecast7_max).Text = GetString(Resource.String.max) + " " + oneClickApi.daily[7].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast7_min).Text = GetString(Resource.String.min) + " " + oneClickApi.daily[7].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast7_pop).Text = oneClickApi.daily[7].pop.ToString("P0");
            // Forecast Info
            FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[1].temp.morn.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[1].feels_like.morn.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[1].temp.day.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[1].feels_like.day.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[1].temp.eve.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[1].feels_like.eve.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + " " + oneClickApi.daily[1].temp.night.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + " " + oneClickApi.daily[1].feels_like.night.ToString() + "°C";
        }

        // Set Syncfusion Chart https://help.syncfusion.com/xamarin-android/sfchart/overview
        public async void SetChart()
        {
            await Task.Delay(1000);
            // Chart
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            SfChart chart = FindViewById<SfChart>(Resource.Id.sfChart1);
            chart.Series.Clear();
            //Initializing Primary Axis
            CategoryAxis primaryAxis = new CategoryAxis();
            chart.PrimaryAxis = primaryAxis;
            //Initializing Secondary Axis
            NumericalAxis secondaryAxis = new NumericalAxis();
            chart.SecondaryAxis = secondaryAxis;
            // Populate Temp Series
            ObservableCollection<TempChart> tempchart = new ObservableCollection<TempChart>
                {
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[0].dt).ToLocalTime().ToString("d"), oneClickApi.daily[0].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[0].dt).ToLocalTime().ToString("d"), oneClickApi.daily[0].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[0].dt).ToLocalTime().ToString("d"), oneClickApi.daily[0].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[0].dt).ToLocalTime().ToString("d"), oneClickApi.daily[0].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d"), oneClickApi.daily[1].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d"), oneClickApi.daily[1].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d"), oneClickApi.daily[1].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d"), oneClickApi.daily[1].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d"), oneClickApi.daily[2].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d"), oneClickApi.daily[2].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d"), oneClickApi.daily[2].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d"), oneClickApi.daily[2].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d"), oneClickApi.daily[3].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d"), oneClickApi.daily[3].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d"), oneClickApi.daily[3].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d"), oneClickApi.daily[3].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d"), oneClickApi.daily[4].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d"), oneClickApi.daily[4].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d"), oneClickApi.daily[4].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d"), oneClickApi.daily[4].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d"), oneClickApi.daily[5].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d"), oneClickApi.daily[5].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d"), oneClickApi.daily[5].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d"), oneClickApi.daily[5].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d"), oneClickApi.daily[6].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d"), oneClickApi.daily[6].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d"), oneClickApi.daily[6].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d"), oneClickApi.daily[6].temp.night),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d"), oneClickApi.daily[7].temp.morn),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d"), oneClickApi.daily[7].temp.day),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d"), oneClickApi.daily[7].temp.eve),
                    new TempChart(dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d"), oneClickApi.daily[7].temp.night)
                };
            AreaSeries tempseries = (new AreaSeries()
            {
                ItemsSource = tempchart,
                XBindingPath = "Date",
                YBindingPath = "Temperature"
            });
            tempseries.TooltipEnabled = true;
            tempseries.Label = GetString(Resource.String.tempchart);
            tempseries.VisibilityOnLegend = Visibility.Visible;
            tempseries.Color = Android.Graphics.Color.Red;
            chart.Series.Add(tempseries);
            ObservableCollection<RainChart> rainchart = new ObservableCollection<RainChart>
                {
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[0].dt).ToLocalTime().ToString("d"), oneClickApi.daily[0].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d"), oneClickApi.daily[1].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d"), oneClickApi.daily[2].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d"), oneClickApi.daily[3].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d"), oneClickApi.daily[4].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d"), oneClickApi.daily[5].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d"), oneClickApi.daily[6].rain),
                    new RainChart(dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d"), oneClickApi.daily[7].rain)
                };
            AreaSeries rainseries = (new AreaSeries()
            {
                ItemsSource = rainchart,
                XBindingPath = "Date",
                YBindingPath = "Rain"
            });
            rainseries.TooltipEnabled = true;
            rainseries.Label = GetString(Resource.String.rainchart);
            rainseries.VisibilityOnLegend = Visibility.Visible;
            rainseries.Color = Android.Graphics.Color.Blue;
            chart.Series.Add(rainseries);
            if (oneClickApi.daily[0].snow > 0.001 || oneClickApi.daily[1].snow > 0.001 || oneClickApi.daily[2].snow > 0.001 || oneClickApi.daily[3].snow > 0.001 || oneClickApi.daily[4].snow > 0.001 || oneClickApi.daily[5].snow > 0.001 || oneClickApi.daily[6].snow > 0.001 || oneClickApi.daily[7].snow > 0.001)
            {
                ObservableCollection<SnowChart> snowchart = new ObservableCollection<SnowChart>
                    {
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[0].dt).ToLocalTime().ToString("d"), oneClickApi.daily[0].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d"), oneClickApi.daily[1].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d"), oneClickApi.daily[2].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d"), oneClickApi.daily[3].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d"), oneClickApi.daily[4].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d"), oneClickApi.daily[5].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d"), oneClickApi.daily[6].snow),
                        new SnowChart(dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d"), oneClickApi.daily[7].snow)
                    };
                AreaSeries snowseries = (new AreaSeries()
                {
                    ItemsSource = snowchart,
                    XBindingPath = "Date",
                    YBindingPath = "Snow"
                });
                snowseries.TooltipEnabled = true;
                snowseries.Label = GetString(Resource.String.snowchart);
                snowseries.VisibilityOnLegend = Visibility.Visible;
                snowseries.Color = Android.Graphics.Color.LightGray;
                chart.Series.Add(snowseries);
            }
            chart.Legend.Visibility = Visibility.Visible;
        }
        public void SetAlerts()
        {
            TextView alerts = FindViewById<TextView>(Resource.Id.alerts);
            if (oneClickApi.alerts != null)
            {
                alerts.Text = GetString(Resource.String.placeholder);
                foreach (Alert alert in oneClickApi.alerts) // Loop through alerts and at each one to TextView
                {
                    string alertstext = alerts.Text;
                    if (alertstext == GetString(Resource.String.placeholder) || alertstext == GetString(Resource.String.noalerts))
                    {
                        alerts.Text = alert.sender_name + ": " + alert.description;
                    }
                    else
                    {
                        alerts.Text = alertstext + "\n---\n" + alert.sender_name + ": " + alert.description;
                    }
                }
            }
            else
            {
                alerts.Text = GetString(Resource.String.noalerts);
            }
        }
        public void SetPreferences() // Get Preferences, if not present use default values (base64)
        {
            try
            {
                openweathermap = Preferences.Get("openweathermap", "ODlmNDUzZGQwMDMxNzU2OGM1NjU1ZGRkZWNlN2YyYTc=");
                locationiq = Preferences.Get("locationiq", "cGsuMGNlZDAwYmQ5MjZkYmQ2ZDFlYTY0OTQxNDkxZjIyOGE=");
                cachehr = Preferences.Get("cachehr", 3);
            }
            catch (Java.Lang.ClassCastException)
            {
                Toast.MakeText(ApplicationContext, "Bad Configuration. Reseting...", ToastLength.Long).Show();
                Preferences.Clear();
                SetPreferences();
            }
        }
    }
    // Chart Data
    public class TempChart
    {
        public TempChart(string date, double temperature)
        {
            this.Date = date;
            this.Temperature = temperature;
        }
        public string Date { get; set; }
        public double Temperature { get; set; }
    }
    public class RainChart
    {
        public RainChart(string date, double rain)
        {
            this.Date = date;
            this.Rain = rain;
        }
        public string Date { get; set; }
        public double Rain { get; set; }
    }
    public class SnowChart
    {
        public SnowChart(string date, double snow)
        {
            this.Date = date;
            this.Snow = snow;
        }
        public string Date { get; set; }
        public double Snow { get; set; }
    }
    // Deserialization
    public class Weather
    {
#pragma warning disable IDE1006 // Benennungsstile
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }

    public class Current
    {
        public int dt { get; set; }
        public int sunrise { get; set; }
        public int sunset { get; set; }
        public double temp { get; set; }
        public double feels_like { get; set; }
        public int pressure { get; set; }
        public int humidity { get; set; }
        public double dew_point { get; set; }
        public double uvi { get; set; }
        public int clouds { get; set; }
        public int visibility { get; set; }
        public double wind_speed { get; set; }
        public int wind_deg { get; set; }
        public double wind_gust { get; set; }
        public List<Weather> weather { get; set; }
        public Rain rain { get; set; }
        public Snow snow { get; set; }
    }

    public class Snow
    {
        [JsonProperty("1h")]
        public double _1h { get; set; }
    }

    public class Rain
    {
        [JsonProperty("1h")]
        public double _1h { get; set; }
    }

    public class Minutely
    {
        public int dt { get; set; }
        public double precipitation { get; set; }
    }

    public class Weather2
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }

    public class Hourly
    {
        public int dt { get; set; }
        public double temp { get; set; }
        public double feels_like { get; set; }
        public int pressure { get; set; }
        public int humidity { get; set; }
        public double dew_point { get; set; }
        public double uvi { get; set; }
        public int clouds { get; set; }
        public int visibility { get; set; }
        public double wind_speed { get; set; }
        public int wind_deg { get; set; }
        public List<Weather2> weather { get; set; }
        public double pop { get; set; }
        public Snow snow { get; set; }
    }

    public class Temp
    {
        public double day { get; set; }
        public double min { get; set; }
        public double max { get; set; }
        public double night { get; set; }
        public double eve { get; set; }
        public double morn { get; set; }
    }

    public class FeelsLike
    {
        public double day { get; set; }
        public double night { get; set; }
        public double eve { get; set; }
        public double morn { get; set; }
    }

    public class Weather3
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }

    public class Daily
    {
        public int dt { get; set; }
        public int sunrise { get; set; }
        public int sunset { get; set; }
        public Temp temp { get; set; }
        public FeelsLike feels_like { get; set; }
        public int pressure { get; set; }
        public int humidity { get; set; }
        public double dew_point { get; set; }
        public double wind_speed { get; set; }
        public int wind_deg { get; set; }
        public List<Weather3> weather { get; set; }
        public int clouds { get; set; }
        public double pop { get; set; }
        public double rain { get; set; }
        public double snow { get; set; }
        public double uvi { get; set; }
    }

    public class Alert
    {
        public string sender_name { get; set; }
        public string @event { get; set; }
        public int start { get; set; }
        public int end { get; set; }
        public string description { get; set; }
    }

    public class OneClickApi
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public string timezone { get; set; }
        public int timezone_offset { get; set; }
        public Current current { get; set; }
        public List<Minutely> minutely { get; set; }
        public List<Hourly> hourly { get; set; }
        public List<Daily> daily { get; set; }
        public List<Alert> alerts { get; set; }
    }
    // Reverse Geocoding
    public class Address
    {
        public string house_number { get; set; }
        public string museum { get; set; }
        public string road { get; set; }
        public string suburb { get; set; }
        public string village { get; set; }
        public string city_district { get; set; }
        public string city { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string postcode { get; set; }
        public string country_code { get; set; }
    }

    public class ReverseGeocoding
    {
        public string place_id { get; set; }
        public string licence { get; set; }
        public string osm_type { get; set; }
        public string osm_id { get; set; }
        public string lat { get; set; }
        public string lon { get; set; }
        public string display_name { get; set; }
        public Address address { get; set; }
        public List<string> boundingbox { get; set; }
    }
    // Forward Geocoding
    public class ForwardGeocoding
    {
        public string place_id { get; set; }
        public string licence { get; set; }
        public string osm_type { get; set; }
        public string osm_id { get; set; }
        public List<string> boundingbox { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public string display_name { get; set; }
        public string @class { get; set; }
        public string type { get; set; }
        public double importance { get; set; }
        public string icon { get; set; }
#pragma warning restore IDE1006 // Benennungsstile
    }
}
