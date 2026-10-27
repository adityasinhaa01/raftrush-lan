using System;
using System.Collections.Generic;

namespace PuffPirates.Save
{
	[Serializable]
	public sealed class HostRunsData
	{
		public List<HostRunData> runs = new List<HostRunData>();
	}
}
