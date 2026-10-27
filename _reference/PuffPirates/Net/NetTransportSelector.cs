using System;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishySteamworks;

namespace PuffPirates.Net
{
	public static class NetTransportSelector
	{
		public const int SteamIndex = 0;

		public const int LocalIndex = 1;

		public const ushort LocalPort = 7770;

		private static bool _clientTransportSet;

		public static bool UseLocalTransport(bool steamAvailable)
		{
			return !steamAvailable;
		}

		private static Multipass Mp(NetworkManager nm)
		{
			_clientTransportSet = true;
			return nm.TransportManager.GetTransport<Multipass>();
		}

		public static bool StartHost(NetworkManager nm, bool local, int maxPlayers)
		{
			int maximumClients = Math.Max(1, maxPlayers - 1);
			Multipass multipass = Mp(nm);
			if (local)
			{
				Tugboat transport = nm.TransportManager.GetTransport<Tugboat>();
				transport.SetServerBindAddress("127.0.0.1", IPAddressType.IPv4);
				transport.SetPort(7770);
				transport.SetMaximumClients(maximumClients);
				transport.SetClientAddress("127.0.0.1");
				multipass.SetClientTransport(transport);
				if (!multipass.StartConnection(server: true, 1))
				{
					return false;
				}
			}
			else
			{
				global::FishySteamworks.FishySteamworks transport2 = nm.TransportManager.GetTransport<global::FishySteamworks.FishySteamworks>();
				transport2.SetMaximumClients(maximumClients);
				multipass.SetClientTransport(transport2);
				if (!multipass.StartConnection(server: true, 0))
				{
					return false;
				}
			}
			return nm.ClientManager.StartConnection();
		}

		public static bool StartClientSteam(NetworkManager nm, ulong hostSteamId)
		{
			global::FishySteamworks.FishySteamworks transport = nm.TransportManager.GetTransport<global::FishySteamworks.FishySteamworks>();
			transport.SetClientAddress(hostSteamId.ToString());
			Mp(nm).SetClientTransport(transport);
			return nm.ClientManager.StartConnection();
		}

		public static bool StartClientLocal(NetworkManager nm)
		{
			Tugboat transport = nm.TransportManager.GetTransport<Tugboat>();
			transport.SetClientAddress("127.0.0.1");
			transport.SetPort(7770);
			Mp(nm).SetClientTransport(transport);
			return nm.ClientManager.StartConnection();
		}

		public static void StopAll(NetworkManager nm)
		{
			if (!(nm == null))
			{
				if (_clientTransportSet)
				{
					nm.ClientManager.StopConnection();
				}
				nm.ServerManager.StopConnection(sendDisconnectMessage: true);
			}
		}
	}
}
