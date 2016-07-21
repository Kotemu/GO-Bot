using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Runtime.Serialization.Formatters;

namespace GO_Bot.Internals {

	internal static class Settings {

		private static string GeneralSettingsFileName = ApplicationEnvironment.SettingsDirectory() + @"\GeneralSettings.json";
		public static GeneralSettings GeneralSettings { get; private set; }

		public static void Load() {
			GeneralSettings = new GeneralSettings();
			JsonSerializerSettings settings = new JsonSerializerSettings() {
				Formatting = Formatting.Indented,
				TypeNameHandling = TypeNameHandling.Auto,
				TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
			};
			settings.Converters.Add(new StringEnumConverter() { CamelCaseText = true });

			if (File.Exists(GeneralSettingsFileName)) {
				GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(File.ReadAllText(GeneralSettingsFileName), settings);
			}

			Save(); // Ensure settings files get created and are up to date
		}

		public static void Save() {
			File.WriteAllText(GeneralSettingsFileName, JsonConvert.SerializeObject(GeneralSettings, Formatting.Indented));
		}

	}

	// TODO make this the MainWindow data context
	[JsonObject(MemberSerialization = MemberSerialization.OptOut)]
	internal class GeneralSettings {

		public double WindowLeft;
		public double WindowTop;
		public double WindowWidth;
		public double WindowHeight;
		public string PtcUsername;
		public string PtcPassword;
		public double Latitude = 39.986855;
		public double Longitude = -75.196442;

	}

}
