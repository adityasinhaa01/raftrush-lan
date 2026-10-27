using System;

namespace PuffPirates.Save
{
	[Serializable]
	public sealed class SessionData
	{
		public string token;

		public string expiration;

		public string username;
	}
}
