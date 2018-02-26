using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TShockAPI;

namespace BossSpawner
{
	public class Config
	{
		private static string FilePath = Path.Combine(TShock.SavePath, "bossConfig.json");

		public bool Enabled;
		public string ArenaRegion;
		public List<Dictionary<int, int>> npcsToSpawn;

		public static Config ReadConfig()
		{
			//if file doesn't exist
			if (!File.Exists(FilePath))
			{
				//create new config
				Config newConfig = DefaultConfig();

				//save it to file
				File.WriteAllText(FilePath, JsonConvert.SerializeObject(newConfig));

				TShock.Log.ConsoleInfo("bossConfig.json not found. Creating a new one...");

				return newConfig;
			}

			try
			{
				Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(FilePath));

				if (config.npcsToSpawn.Count == 0)
				{
					TShock.Log.ConsoleInfo("There were no bosses defined in bossConfig.json! Disabling boss spawning...");
					config.Enabled = false;
				}
				if (string.IsNullOrWhiteSpace(config.ArenaRegion))
				{
					TShock.Log.ConsoleInfo("Invalid region name for arena in bossConfig.json! Disabling boss spawning...");
					config.Enabled = false;
				}

				return config;
			}
			catch
			{
				TShock.Log.ConsoleError("bossConfig.json could not be loaded! Using default config...");
				return DefaultConfig();
			}
		}

		public static Config DefaultConfig()
		{
			return new Config()
			{
				Enabled = true,
				npcsToSpawn = new List<Dictionary<int, int>>()
				{
					new Dictionary<int, int>()
					{
						{ 50, 5 }
					},
					new Dictionary<int, int>()
					{
						{ 125, 2 },
						{ 126, 2 }
					},
					new Dictionary<int, int>()
					{
						{ 134, 2 }
					},
					new Dictionary<int, int>()
					{
						{ 127, 3 }
					},
					new Dictionary<int, int>()
					{
						{ 262, 1 }
					},
					new Dictionary<int, int>()
					{
						{ 245, 5 }
					},
					new Dictionary<int, int>()
					{
						{ 325, 4 },
						{ 327, 2 }
					},
					new Dictionary<int, int>()
					{
						{ 344, 4 },
						{ 346, 2 }
					}
				}
			};
		}
	}
}
