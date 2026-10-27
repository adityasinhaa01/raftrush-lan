using GamesMrkt.Core;
using GamesMrkt.Resource;
using UnityEngine;

namespace GamesMrkt.PuffPirates
{
	public static class ExternalLinkHelper
	{
		public static void OpenDiscord()
		{
			OpenLink(Singleton<CoreSystemHandler>.Instance.GetSystem<ResourceSystem>().GetString("dc_link"));
		}

		public static void OpenBugReport()
		{
			OpenLink("mailto:" + Singleton<CoreSystemHandler>.Instance.GetSystem<ResourceSystem>().GetString("bug_report_mail"));
		}

		private static void OpenLink(string url)
		{
			Application.OpenURL(url);
		}
	}
}
