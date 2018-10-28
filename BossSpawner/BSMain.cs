using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace BossSpawner
{
	[ApiVersion(2,1)]
    public class BSMain : TerrariaPlugin
    {
		#region Plugin Info
		public override string Author => "Zaicon";
		public override string Description => "Automatically spawns bosses at night!";
		public override string Name => "BossSpawner";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
		#endregion

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
			ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);

			GeneralHooks.ReloadEvent += OnReload;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);

				GeneralHooks.ReloadEvent -= OnReload;
			}
			base.Dispose(disposing);
		}
		#endregion

		public BSMain(Main game) : base(game) { }

		private Config Config;
		private DateTime lastUpdate;
		private Dictionary<int,int> npcsThatWereSpawned;
		private Random Random;
		private bool AwaitingSpawn;

		#region Hooks
		private void OnGameInitialize(EventArgs args)
		{
			Config = Config.ReadConfig();
			npcsThatWereSpawned = new Dictionary<int, int>();
			Random = new Random();
			AwaitingSpawn = false;

			lastUpdate = DateTime.Now;
		}

		private void OnReload(ReloadEventArgs args)
		{
			Config = Config.ReadConfig();
		}

		private void OnGameUpdate(EventArgs args)
		{
			if (!Config.Enabled || Main.dayTime)
				return;

			if ((DateTime.Now - lastUpdate).TotalSeconds > 1)
			{
				lastUpdate = DateTime.Now;

				//check if any bosses are still alive
				//if so, let people continue to kill bosses
				if (Main.npc.ToList().Exists(e => e != null && e.active && npcsThatWereSpawned.ContainsKey(e.netID)))
					return;

				if (AwaitingSpawn)
					return;

				Task.Factory.StartNew(() => {
					AwaitingSpawn = true;
					TShock.Utils.Broadcast("All bosses have been killed! The next wave of bosses will start in 30 seconds...", Color.ForestGreen);
					Thread.Sleep(10000);
					TShock.Utils.Broadcast("Bosses will spawn in 20 seconds at /warp arena!", Color.ForestGreen);
					Thread.Sleep(10000);
					TShock.Utils.Broadcast("Bosses will spawn in 10 seconds at /warp arena!", Color.ForestGreen);
					Thread.Sleep(10000);

					//make sure it's not day time!!!
					if (Main.dayTime)
						return;

					//check for arena
					var region = TShock.Regions.GetRegionByName(Config.ArenaRegion);
					if (region == null)
					{
						TShock.Log.ConsoleError("Cannot find boss arena! Disabling boss spawning...");
						Config.Enabled = false;
						return;
					}

					//Check for players in arena
					{
						if (!TShock.Players.Any(e => e.CurrentRegion?.Name == region.Name))
						{
							TShock.Utils.Broadcast("No players are in the arena, bosses are not spawning.", Color.ForestGreen);
							AwaitingSpawn = false;
							return;
						}
					}

					Dictionary<int, int> npcsToSpawn;

					if (Config.npcsToSpawn.Count == 1)
					{
						npcsToSpawn = Config.npcsToSpawn[0];
					}
					else
					{
						npcsToSpawn = Config.npcsToSpawn[Random.Next(Config.npcsToSpawn.Count)]; //random

						//Does this even work lol
						while (npcsToSpawn == npcsThatWereSpawned)
						{
							npcsToSpawn = Config.npcsToSpawn[Random.Next(Config.npcsToSpawn.Count)]; //random
						}
					}

					//spawn bosses!!
					npcsThatWereSpawned = npcsToSpawn;

					TSPlayer.All.SendMessage($"Spawning bosses at /warp arena: {string.Join(", ", npcsToSpawn.Select(e => $"{e.Value}x {Lang.GetNPCNameValue(e.Key)}"))}", Color.ForestGreen);
					foreach (var kvp in npcsToSpawn)
					{
						TSPlayer.Server.SpawnNPC(kvp.Key,
							Lang.GetNPCNameValue(kvp.Key),
							kvp.Value,
							region.Area.X,
							region.Area.Y,
							region.Area.Width,
							region.Area.Height);
					}
					AwaitingSpawn = false;
				});
			}
		}
		#endregion
	}
}
