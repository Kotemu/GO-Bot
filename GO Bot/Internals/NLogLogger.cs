using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Logging;
using Logger = NLog.Logger;

namespace GO_Bot.Internals {

	internal class NLogLogger : ILogger {

		private static Logger logger = NLog.LogManager.GetLogger("API");

		public void Write(string message, LogLevel level = LogLevel.Info) {
			logger.Info(message);
		}

	}

}
