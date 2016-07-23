using PokemonGo.RocketAPI;
using System;
using PokemonGo.RocketAPI.Enums;
using AllEnum;
using System.Collections.Generic;

namespace GO_Bot.Internals {

	internal class GoSettings : ISettings {

		public AuthType AuthType { get; set; } = AuthType.Ptc;

		public double DefaultAltitude {
			get {
				return 10;
			}
		}

		public double DefaultLatitude { get; set; } = 52.379189;
		public double DefaultLongitude { get; set; } = 4.899431;
		public string GoogleRefreshToken { get; set; } = String.Empty;

		public ICollection<KeyValuePair<ItemId, int>> itemRecycleFilter {
			get {
				return new List<KeyValuePair<ItemId, int>>();
			}

			set {
				throw new NotImplementedException();
			}
		}

		public string PtcPassword { get; set; } = String.Empty;
		public string PtcUsername { get; set; } = String.Empty;

	}

}
