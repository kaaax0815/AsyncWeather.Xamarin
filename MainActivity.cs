using Android.App;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Text;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Plugin.Geolocator;
using Square.Picasso;
using Syncfusion.Android.ProgressBar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace AsyncWeather.Xamarin
{
    /// <summary>
    /// Error Codes: 199 = Permission; 505 = Generic; 594 = Feature; 250 = Try Again
    /// HTML Codes: hXXX = XXX HTML Error
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        SfLinearProgressBar sfLinearProgressBar { get; set; }
        readonly string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        readonly string openweathermap = "ODlmNDUzZGQwMDMxNzU2OGM1NjU1ZGRkZWNlN2YyYTc=";
        readonly string syncfusionlicense = "TXpjMU9UazFRRE14TXpneVpUTTBNbVV6TUVwR1VpOTZOR0ZySzJ4clUwbzJlbUoxY0hwbVltNW1aa05xVkVwUVVFUTBNVzFzYkhOalVuSmFaV3M5";
        readonly string locationiq = "cGsuMGNlZDAwYmQ5MjZkYmQ2ZDFlYTY0OTQxNDkxZjIyOGE=";
        OneClickApi oneClickApi { get; set; }
        ReverseGeocoding reverseGeocoding { get; set; }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Encoding.UTF8.GetString(Convert.FromBase64String(syncfusionlicense)));
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            InitProgressbar();
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

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            /**if (id == Resource.Id.action_settings)
            {
                StartActivity(typeof(Settings));
                return true;
            }**/
            if (id == Resource.Id.action_refresh)
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
                return true;
            }
            /**if (id == Resource.Id.action_search)
            {
                if (search_clicked)
                {
                    FindViewById<LinearLayout>(Resource.Id.search_layout).Visibility = ViewStates.Gone;
                    search_clicked = false;
                    GetWeather();
                    search_clicked = false;
                    return true;
                }
                FindViewById<LinearLayout>(Resource.Id.search_layout).Visibility = ViewStates.Visible;
                FindViewById<EditText>(Resource.Id.search_edit).RequestFocus();
                search_clicked = true;

            }**/
            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool IsOnline()
        {
            ConnectivityManager cm = (ConnectivityManager)GetSystemService(ConnectivityService);
            return cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected;
        }
        // Get and Returns Location or Error
        public static async Task<double[]> GetLocation()
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
                // StartActivity(new Intent(Android.Provider.Settings.ActionLocationSourceSettings));
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
                double[] double_generic = { 505, 0 };
                return double_generic;
            }
        }

        public static async Task<string> GetWeather(double[] loc, string lang, string key)
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
                return "505";
            }
            // return "h501";
        }

        public static async Task<string> GetReverseLocation(double[] loc, string key)
        {
            try
            {
                string lat = loc[0].ToString();
                string lon = loc[1].ToString();
                string reverse = await Get("https://us1.locationiq.com/v1/reverse.php?key=" + key + "&lat=" + lat.Replace(",", ".") + "&lon=" + lon.Replace(",", ".") + "&format=json");
                return reverse;
            }
            catch (Exception weathere)
            {
                return "505";
            }
            // return "h501";
        }


        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> Get(string url)
        {
            using HttpResponseMessage result1 = await _httpClient.GetAsync($"{url}");
            string content = await result1.Content.ReadAsStringAsync();
            return content;
        }

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

        public void InitProgressbar()
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
                while (loc[0] == 250)
                {
                    loc = await GetLocation();
                }
                string weather = await GetWeather(loc, lang, Encoding.UTF8.GetString(Convert.FromBase64String(openweathermap)));
                string reverse = await GetReverseLocation(loc, Encoding.UTF8.GetString(Convert.FromBase64String(locationiq)));
                DeserializeWeather(weather);
                DeserializeReverse(reverse);
                SavePreferences(weather, reverse);
                SetText();
                SetChart();
                SetAlerts();
                StopProgressbar();
            }
            catch(Exception onlinee)
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
            Preferences.Set("offline_time", thisTime.ToString("t"));
        }

        public void OfflineWeather()
        {
            //StartProgresbar();
            //string weather = Preferences.Get("offline_weather", "");
            //DeserializeWeather(weather);
            //SetText();
            //StopProgressbar();
        }

        public void DeserializeWeather(string json)
        {
            oneClickApi = JsonConvert.DeserializeObject<OneClickApi>(json);
        }

        public void DeserializeReverse(string json)
        {
            reverseGeocoding = JsonConvert.DeserializeObject<ReverseGeocoding>(json);
        }

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
            alert.SetNeutralButton(GetString(Resource.String.ok), (senderAlert, args) =>
            {
                sfLinearProgressBar.Visibility = ViewStates.Gone;
                return;
            });
            Dialog dialog = alert.Create();
            dialog.Show();
        }

        public async void SetText()
        {
            await Task.Delay(1000);
            // Current Weather
            if (reverseGeocoding.address.city != null)
                FindViewById<TextView>(Resource.Id.city_txt).Text = reverseGeocoding.address.city;
            else if (reverseGeocoding.address.village != null)
                FindViewById<TextView>(Resource.Id.city_txt).Text = reverseGeocoding.address.village;
            DateTime thisDay = DateTime.Today;
            FindViewById<TextView>(Resource.Id.date_txt).Text = thisDay.ToString("D");
            string url = "https://openweathermap.org/img/wn/" + oneClickApi.current.weather[0].icon + "@4x.png";
            Picasso.Get().Load(url).Into(FindViewById<ImageView>(Resource.Id.weather_img));
            FindViewById<TextView>(Resource.Id.temp_txt).Text = oneClickApi.current.temp + "°C";
            FindViewById<TextView>(Resource.Id.feelslike_txt).Text = GetString(Resource.String.feelslike) + oneClickApi.current.feels_like + "°C";
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
                FindViewById<TextView>(Resource.Id.rain_txt).Text = " " + oneClickApi.current.rain._1h + GetString(Resource.String.rain) + "1h";
            }
            else if (oneClickApi.current.snow != null && oneClickApi.current.snow._1h > 0.01)
            {
                FindViewById<RelativeLayout>(Resource.Id.rain_layout).Visibility = ViewStates.Visible;
                FindViewById<TextView>(Resource.Id.rain_txt).Text = " " + oneClickApi.current.snow._1h + GetString(Resource.String.snow) + "1h";
            }
            // Forecast
            FindViewById<TextView>(Resource.Id.forecast1_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[1].dt).ToLocalTime().ToString("d");
            string forecast1_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[1].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast1_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast1_img));
            FindViewById<TextView>(Resource.Id.forecast1_max).Text = GetString(Resource.String.max) + oneClickApi.daily[1].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast1_min).Text = GetString(Resource.String.min) + oneClickApi.daily[1].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast1_pop).Text = oneClickApi.daily[1].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast2_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[2].dt).ToLocalTime().ToString("d");
            string forecast2_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[2].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast2_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast2_img));
            FindViewById<TextView>(Resource.Id.forecast2_max).Text = GetString(Resource.String.max) + oneClickApi.daily[2].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast2_min).Text = GetString(Resource.String.min) + oneClickApi.daily[2].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast2_pop).Text = oneClickApi.daily[2].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast3_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[3].dt).ToLocalTime().ToString("d");
            string forecast3_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[3].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast3_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast3_img));
            FindViewById<TextView>(Resource.Id.forecast3_max).Text = GetString(Resource.String.max) + oneClickApi.daily[3].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast3_min).Text = GetString(Resource.String.min) + oneClickApi.daily[3].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast3_pop).Text = oneClickApi.daily[3].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast4_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[4].dt).ToLocalTime().ToString("d");
            string forecast4_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[4].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast4_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast4_img));
            FindViewById<TextView>(Resource.Id.forecast4_max).Text = GetString(Resource.String.max) + oneClickApi.daily[4].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast4_min).Text = GetString(Resource.String.min) + oneClickApi.daily[4].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast4_pop).Text = oneClickApi.daily[4].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast5_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[5].dt).ToLocalTime().ToString("d");
            string forecast5_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[5].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast5_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast5_img));
            FindViewById<TextView>(Resource.Id.forecast5_max).Text = GetString(Resource.String.max) + oneClickApi.daily[5].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast5_min).Text = GetString(Resource.String.min) + oneClickApi.daily[5].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast5_pop).Text = oneClickApi.daily[5].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast6_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[6].dt).ToLocalTime().ToString("d");
            string forecast6_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[6].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast6_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast6_img));
            FindViewById<TextView>(Resource.Id.forecast6_max).Text = GetString(Resource.String.max) + oneClickApi.daily[6].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast6_min).Text = GetString(Resource.String.min) + oneClickApi.daily[6].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast6_pop).Text = oneClickApi.daily[6].pop.ToString("P0");
            FindViewById<TextView>(Resource.Id.forecast7_date).Text = dtDateTime.AddSeconds(oneClickApi.daily[7].dt).ToLocalTime().ToString("d");
            string forecast7_url = "https://openweathermap.org/img/wn/" + oneClickApi.daily[7].weather[0].icon + "@4x.png";
            Picasso.Get().Load(forecast7_url).Resize(150, 150).CenterCrop().Into(FindViewById<ImageView>(Resource.Id.forecast7_img));
            FindViewById<TextView>(Resource.Id.forecast7_max).Text = GetString(Resource.String.max) + oneClickApi.daily[7].temp.max.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast7_min).Text = GetString(Resource.String.min) + oneClickApi.daily[7].temp.min.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.forecast7_pop).Text = oneClickApi.daily[7].pop.ToString("P0");
            // Forecast Info
            FindViewById<TextView>(Resource.Id.temp_mor).Text = GetString(Resource.String.temp) + oneClickApi.daily[1].temp.morn.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_mor).Text = GetString(Resource.String.feelslike) + oneClickApi.daily[1].feels_like.morn.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.temp_day).Text = GetString(Resource.String.temp) + oneClickApi.daily[1].temp.day.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_day).Text = GetString(Resource.String.feelslike) + oneClickApi.daily[1].feels_like.day.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.temp_eve).Text = GetString(Resource.String.temp) + oneClickApi.daily[1].temp.eve.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_eve).Text = GetString(Resource.String.feelslike) + oneClickApi.daily[1].feels_like.eve.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.temp_night).Text = GetString(Resource.String.temp) + oneClickApi.daily[1].temp.night.ToString() + "°C";
            FindViewById<TextView>(Resource.Id.feels_night).Text = GetString(Resource.String.feelslike) + oneClickApi.daily[1].feels_like.night.ToString() + "°C";
        }
        public void SetChart()
        {

        }
        public void SetAlerts()
        {
            TextView alerts = FindViewById<TextView>(Resource.Id.alerts);
            if (oneClickApi.alerts != null)
            {

                if (oneClickApi.alerts.Count > 1)
                {
                    if (oneClickApi.alerts.Count > 2)
                    {
                        alerts.Text = oneClickApi.alerts[0].sender_name + ": " + oneClickApi.alerts[0].description + "\n\n" + oneClickApi.alerts[1].sender_name + ": " + oneClickApi.alerts[1].description + "\n\n" + oneClickApi.alerts[2].sender_name + ": " + oneClickApi.alerts[2].description;
                    }
                    else
                    {
                        alerts.Text = oneClickApi.alerts[0].sender_name + ": " + oneClickApi.alerts[0].description + "\n\n" + oneClickApi.alerts[1].sender_name + ": " + oneClickApi.alerts[1].description;
                    }
                }
                else
                {
                    alerts.Text = oneClickApi.alerts[0].sender_name + ": " + oneClickApi.alerts[0].description;
                }
            }
            else
            {
                alerts.Text = GetString(Resource.String.noalerts);
            }
        }
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
        public string lat { get; set; }
        public string lon { get; set; }
        public string display_name { get; set; }
        public string @class { get; set; }
        public string type { get; set; }
        public double importance { get; set; }
        public string icon { get; set; }
#pragma warning restore IDE1006 // Benennungsstile
    }
}
