using GMap.NET;
using GMap.NET.MapProviders;
using GO_Bot.Internals;
using GO_Bot.Internals.Bot;
using GO_Bot.Models;
using NLog;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger = NLog.Logger;

namespace GO_Bot.Views {

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private static Logger logger = LogManager.GetCurrentClassLogger();
		private static ListBoxWriter listBoxWriter;
		private static BackgroundTask backgroundTask;

		internal static MainWindow Instance { get; private set; }
		internal MainWindowModel Model { get; private set; }

		public MainWindow() {
			InitializeComponent();
			Instance = this;
			Model = MainWindowModel.Create();
			DataContext = Model;
			backgroundTask = new BackgroundTask(() => {
				bool b = Task.Run(async () => {
					GoSettings goSettings = new GoSettings();

					logger.Info("Setting up...");
					goSettings.AuthType = AuthType.Ptc;
					await txtUsername.SafeAccessAsync((t) => goSettings.PtcUsername = t.Text);
					await txtPassword.SafeAccessAsync((t) => goSettings.PtcPassword = t.Password);
					await dudLatitude.SafeAccessAsync((d) => goSettings.DefaultLatitude = d.Value ?? 0);
					await dudLongitude.SafeAccessAsync((d) => goSettings.DefaultLongitude = d.Value ?? 0);

					try {
						logger.Info("Beginning to farm...");
						PokemonGo.RocketAPI.Logger.SetLogger(new NLogLogger());
						await new GoLogic(goSettings).Execute();
					} catch (Exception e) {
						if (e is PtcOfflineException) {
							logger.Info("Could not log in (login servers may be down or invalid credentials)");
						} else {
							logger.Error(e);
						}
					}
					
					return true;
				}).Result;
			});
			SetupUI();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			GMaps.Instance.Mode = AccessMode.ServerOnly;
			gmap.MapProvider = BingMapProvider.Instance;
			gmap.Position = new PointLatLng(Settings.GeneralSettings.Latitude, Settings.GeneralSettings.Longitude);
			gmap.Zoom = 15;
			logger.Info("Welcome to GO Bot!");
		}

		private void Window_Closing(object sender, CancelEventArgs e) {
			GeneralSettings settings = Settings.GeneralSettings;

			settings.WindowLeft = Left;
			settings.WindowTop = Top;
			settings.WindowWidth = Width;
			settings.WindowHeight = Height;
			settings.PtcUsername = txtUsername.Text;

			try {
				settings.PtcPassword = txtPassword.Password.Protect();
			} catch { }

			settings.Latitude = dudLatitude.Value ?? 0;
			settings.Longitude = dudLongitude.Value ?? 0;

			Settings.Save();
			Environment.Exit(0);
		}

		private async void btnStartStop_Click(object sender, RoutedEventArgs e) {
			try {
				Mouse.OverrideCursor = Cursors.Wait;

				bool start = btnStartStop.Content.ToString().ToLower().Contains("start");

				if (start) {
					await backgroundTask.Start();
					Model.Status = "Running";
				} else {
					await backgroundTask.Stop();
					Model.Status = null;
				}

				btnStartStop.Content = (start) ? "Stop" : "Start";
			} catch (Exception ex) {
				logger.Error(ex);
			} finally {
				Mouse.OverrideCursor = null;
			}
		}

		private void SetupUI() {
			GeneralSettings settings = Settings.GeneralSettings;
			listBoxWriter = new ListBoxWriter(lbLog);

			Title = "GO Bot v0.0.2 [BETA]";
			Console.SetOut(listBoxWriter);
			Console.SetError(listBoxWriter);
			
#if !DEBUG
			if (settings.WindowLeft > 0 && settings.WindowTop > 0) {
				WindowStartupLocation = WindowStartupLocation.Manual;
				Left = settings.WindowLeft;
				Top = settings.WindowTop;
				Width = settings.WindowWidth;
				Height = settings.WindowHeight;
			}
#endif

			txtUsername.Text = settings.PtcUsername;

			try {
				txtPassword.Password = settings.PtcPassword.Unprotect();
			} catch { }

			dudLatitude.Value = settings.Latitude;
			dudLongitude.Value = settings.Longitude;
		}
		
	}

}
