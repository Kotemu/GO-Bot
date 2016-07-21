using System;
using System.Security.Cryptography;
using System.Text;

namespace GO_Bot.Internals {

	internal static class StringExtensions {

		public static string Protect(this string plainText, DataProtectionScope scope = DataProtectionScope.LocalMachine, byte[] entropy = null) {
			if (plainText == null) {
				throw new ArgumentNullException("plainText");
			}

			return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), entropy, scope));
		}

		public static string Unprotect(this string encryptedText, DataProtectionScope scope = DataProtectionScope.LocalMachine, byte[] entropy = null) {
			if (encryptedText == null) {
				throw new ArgumentNullException("encryptedText");
			}

			return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedText), entropy, scope));
		}

	}

}
