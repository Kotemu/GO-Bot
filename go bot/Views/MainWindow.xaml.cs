using GO_Bot.Internals;
using GO_Bot.Models;
using NLog;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Logic;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
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
					await txtLatitude.SafeAccessAsync((t) => goSettings.DefaultLatitude = Convert.ToDouble(t.Text));
					await txtLongitude.SafeAccessAsync((t) => goSettings.DefaultLongitude = Convert.ToDouble(t.Text));

					try {
						logger.Info("Beginning to farm...");
						PokemonGo.RocketAPI.Logger.SetLogger(new PokemonGo.RocketAPI.Logging.ConsoleLogger(PokemonGo.RocketAPI.LogLevel.Info));
						await new Logic(goSettings).Execute();
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

			settings.Latitude = Convert.ToDouble(txtLatitude.Text);
			settings.Longitude = Convert.ToDouble(txtLongitude.Text);

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

			txtLatitude.Text = settings.Latitude.ToString();
			txtLongitude.Text = settings.Longitude.ToString();
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}
		
	}

}
