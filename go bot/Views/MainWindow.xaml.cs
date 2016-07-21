using AllEnum;
using GO_Bot.Internals;
using GO_Bot.Models;
using NLog;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
					var client = new Client(goSettings);

					await txtUsername.SafeAccessAsync((t) => goSettings.PtcUsername = t.Text);
					await txtPassword.SafeAccessAsync((t) => goSettings.PtcPassword = t.Password);
					await txtLatitude.SafeAccessAsync((t) => goSettings.DefaultLatitude = Convert.ToDouble(t.Text));
					await txtLongitude.SafeAccessAsync((t) => goSettings.DefaultLongitude = Convert.ToDouble(t.Text));
					logger.Info("Logging in...");
					await client.DoPtcLogin(goSettings.PtcUsername, goSettings.PtcPassword);
					logger.Info("Setting up...");
					await client.SetServer();

					var profile = await client.GetProfile();
					var settings = await client.GetSettings();
					var mapObjects = await client.GetMapObjects();
					var inventory = await client.GetInventory();
					var pokemons = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon).Where(p => p != null && p?.PokemonId > 0);

					logger.Info("Beginning to farm...");
					await ExecuteFarmingPokestopsAndPokemons(client);
					logger.Info("Farmed everything around your default coordinates!");

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

			Title = "GO Bot v0.0.1 [BETA]";
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

		private static async Task ExecuteFarmingPokestopsAndPokemons(Client client) {
			var mapObjects = await client.GetMapObjects();

			var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());

			foreach (var pokeStop in pokeStops) {
				var update = await client.UpdatePlayerLocation(pokeStop.Latitude, pokeStop.Longitude);
				var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
				var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

				logger.Info($"[{DateTime.Now.ToString("HH:mm:ss")}] Farmed XP: {fortSearch.ExperienceAwarded}, Gems: { fortSearch.GemsAwarded}, Eggs: {fortSearch.PokemonDataEgg} Items: {GetFriendlyItemsString(fortSearch.ItemsAwarded)}");

				await Task.Delay(15000);
				await ExecuteCatchAllNearbyPokemons(client);
			}
		}

		private static async Task ExecuteCatchAllNearbyPokemons(Client client) {
			var mapObjects = await client.GetMapObjects();

			var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);

			foreach (var pokemon in pokemons) {
				var update = await client.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude);
				var encounterPokemonRespone = await client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);

				CatchPokemonResponse caughtPokemonResponse;
				do {
					caughtPokemonResponse = await client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude, pokemon.Longitude, MiscEnums.Item.ITEM_POKE_BALL); //note: reverted from settings because this should not be part of settings but part of logic
				}
				while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed);

				logger.Info(caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess ? $"[{DateTime.Now.ToString("HH:mm:ss")}] We caught a {pokemon.PokemonId}" : $"[{DateTime.Now.ToString("HH:mm:ss")}] {pokemon.PokemonId} got away..");
				await Task.Delay(5000);
			}
		}

		private static string GetFriendlyItemsString(IEnumerable<FortSearchResponse.Types.ItemAward> items) {
			var enumerable = items as IList<FortSearchResponse.Types.ItemAward> ?? items.ToList();

			if (!enumerable.Any())
				return string.Empty;

			return
				enumerable.GroupBy(i => i.ItemId)
						  .Select(kvp => new { ItemName = kvp.Key.ToString(), Amount = kvp.Sum(x => x.ItemCount) })
						  .Select(y => $"{y.Amount} x {y.ItemName}")
						  .Aggregate((a, b) => $"{a}, {b}");
		}

	}

}
