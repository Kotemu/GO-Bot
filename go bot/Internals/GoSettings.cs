using PokemonGo.RocketAPI;
using System;
using PokemonGo.RocketAPI.Enums;

namespace GO_Bot.Internals {

	internal class GoSettings : ISettings {

		public AuthType AuthType { get; set; } = AuthType.Ptc;
		public double DefaultLatitude { get; set; }
		public double DefaultLongitude { get; set; }
		public string GoogleRefreshToken { get; set; } = String.Empty;
		public string PtcPassword { get; set; } = String.Empty;
		public string PtcUsername { get; set; } = String.Empty;

	}

}
