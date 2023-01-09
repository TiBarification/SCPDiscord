﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SCPDiscord.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SCPDiscord
{
	internal static class Language
	{
		private static SCPDiscord plugin;
		public static bool ready;

		private static JObject primary;
		private static JObject backup;
		private static JObject overrides;

		private static string languagesPath = Config.GetLanguageDir();

		// All default languages included in the .dll
		private static readonly Dictionary<string, string> defaultLanguages = new Dictionary<string, string>
		{
			{ "overrides",          Encoding.UTF8.GetString(Resources.overrides)        },
			{ "english",            Encoding.UTF8.GetString(Resources.english)          },
			//{ "russian",            Encoding.UTF8.GetString(Resources.russian)          },
			//{ "french",             Encoding.UTF8.GetString(Resources.french)           },
			//{ "polish",             Encoding.UTF8.GetString(Resources.polish)           },
			//{ "italian",            Encoding.UTF8.GetString(Resources.italian)          },
			//{ "finnish",            Encoding.UTF8.GetString(Resources.finnish)          },
			//{ "english-emote",      Encoding.UTF8.GetString(Resources.english_emote)    },
			//{ "russian-emote",      Encoding.UTF8.GetString(Resources.russian_emote)    },
			//{ "french-emote",       Encoding.UTF8.GetString(Resources.french_emote)     },
			//{ "polish-emote",       Encoding.UTF8.GetString(Resources.polish_emote)     },
			//{ "italian-emote",      Encoding.UTF8.GetString(Resources.italian_emote)    },
			//{ "finnish-emote",      Encoding.UTF8.GetString(Resources.finnish_emote)    }
		};

		public static void Reload()
		{
			ready = false;
			plugin = SCPDiscord.plugin;
			languagesPath = Config.GetLanguageDir();

			if (!Directory.Exists(languagesPath))
			{
				Directory.CreateDirectory(languagesPath);
			}

			// Save default language files
			SaveDefaultLanguages();

			// Read primary language file
			plugin.Info("Loading primary language file...");
			try
			{
				LoadLanguageFile(Config.GetString("settings.language"), "primary", out primary);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DirectoryNotFoundException _:
						plugin.Error("Language directory not found.");
						break;
					case UnauthorizedAccessException _:
						plugin.Error("Primary language file access denied.");
						break;
					case FileNotFoundException _:
						plugin.Error("'" + languagesPath + Config.GetString("settings.language") + ".yml' was not found.");
						break;
					case JsonReaderException _:
					case YamlException _:
						plugin.Error("'" + languagesPath + Config.GetString("settings.language") + ".yml' formatting error.");
						break;
				}
				plugin.Error("Error reading primary language file '" + languagesPath + Config.GetString("settings.language") + ".yml'. Attempting to initialize backup system...");
				plugin.Debug(e.ToString());
			}

			// Read backup language file if not the same as the primary
			if (Config.GetString("settings.language") != "english")
			{
				plugin.Info("Loading backup language file...");
				try
				{
					LoadLanguageFile("english", "backup", out backup);
				}
				catch (Exception e)
				{
					switch (e)
					{
						case DirectoryNotFoundException _:
							plugin.Error("Language directory not found.");
							break;
						case UnauthorizedAccessException _:
							plugin.Error("Backup language file access denied.");
							break;
						case FileNotFoundException _:
							plugin.Error("'" + languagesPath + Config.GetString("settings.language") + ".yml' was not found.");
							break;
						case JsonReaderException _:
						case YamlException _:
							plugin.Error("'" + languagesPath + Config.GetString("settings.language") + ".yml' formatting error.");
							break;
					}
					plugin.Error("Error reading backup language file '" + languagesPath + "english.yml'.");
					plugin.Debug(e.ToString());
				}
			}
			if (primary == null && backup == null)
			{
				plugin.Error("NO LANGUAGE FILE LOADED! DEACTIVATING SCPDISCORD.");
				throw new Exception();
			}

			try
			{
				LoadLanguageFile("overrides", "overrides", out overrides);
			}
			catch (Exception e)
			{
				switch (e)
				{
					case DirectoryNotFoundException _:
						plugin.Warn("Language directory not found.");
						break;
					case UnauthorizedAccessException _:
						plugin.Warn("Overrides language file access denied.");
						break;
					case FileNotFoundException _:
						plugin.Warn("'" + languagesPath + "overrides.yml' was not found.");
						break;
					case JsonReaderException _:
					case YamlException _:
						plugin.Warn("'" + languagesPath + "overrides.yml' formatting error.");
						break;
				}
				plugin.Warn("Error reading overrides language file '" + languagesPath + "overrides.yml'.");
				plugin.Debug(e.ToString());
			}

			if (Config.GetBool("settings.configvalidation"))
			{
				ValidateLanguageStrings();
			}

			ready = true;
		}

		/// <summary>
		/// Saves all default language files included in the .dll
		/// </summary>
		private static void SaveDefaultLanguages()
		{
			foreach (KeyValuePair<string, string> language in defaultLanguages)
			{
				if (!File.Exists(languagesPath + language.Key + ".yml") || (Config.GetBool("settings.regeneratelanguagefiles") && language.Key != "overrides"))
				{
					plugin.Info("Creating language file " + languagesPath + language.Key + ".yml...");
					try
					{
						File.WriteAllText((languagesPath + language.Key + ".yml"), language.Value);
					}
					catch (DirectoryNotFoundException)
					{
						plugin.Warn("Could not create language file: Language directory does not exist, attempting to create it... ");
						Directory.CreateDirectory(languagesPath);
						plugin.Info("Creating language file " + languagesPath + language.Key + ".yml...");
						File.WriteAllText((languagesPath + language.Key + ".yml"), language.Value);
					}
				}
			}
		}

		/// <summary>
		/// This function makes me want to die too, don't worry.
		/// Parses a yaml file into a yaml object, parses the yaml object into a json string, parses the json string into a json object
		/// </summary>
		private static void LoadLanguageFile(string language, string type, out JObject dataObject)
		{
			// Reads file contents into FileStream
			FileStream stream = File.OpenRead(languagesPath + language + ".yml");

			// Converts the FileStream into a YAML Dictionary object
			IDeserializer deserializer = new DeserializerBuilder().Build();
			object yamlObject = deserializer.Deserialize(new StreamReader(stream));

			// Converts the YAML Dictionary into JSON String
			ISerializer serializer = new SerializerBuilder()
				.JsonCompatible()
				.Build();
			string jsonString = serializer.Serialize(yamlObject);
			dataObject = JObject.Parse(jsonString);

			plugin.Info("Successfully loaded " + type + " language file '" + language + ".yml'.");
		}

		public static void ValidateLanguageStrings()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\n||||||||||||| SCPDiscord language validator ||||||||||||||\n");
			bool valid = true;
			foreach (string node in Config.languageNodes)
			{
				try
				{
					primary.SelectToken(node + ".message").Value<string>();
				}
				catch (Exception)
				{
					sb.Append("Your SCPDiscord language file \"" + Config.GetString("settings.language") + ".yml\" does not contain the node \"" + node + ".message\".\nEither add it to your language file or delete the file to generate a new one.\n");
					valid = false;
				}
			}

			if (valid)
			{
				sb.Append("No language errors.\n");
			}

			sb.Append("||||||||||||| End of language validation ||||||||||||||");
			plugin.Info(sb.ToString());
		}

		/// <summary>
		/// Gets a string from the primary or backup language file
		/// </summary>
		/// <param name="path">The path to the node</param>
		/// <returns></returns>
		public static string GetString(string path)
		{
			if (primary == null)
			{
				plugin.Warn("Tried to read language string before loading languages.");
				return null;
			}

			try
			{
				try
				{
					return overrides?.SelectToken(path).Value<string>();
				}
				catch (Exception) { /* ignore */ }
				return primary?.SelectToken(path).Value<string>();
			}
			catch (Exception primaryException)
			{
				// This exception means the node does not exist in the language file, the plugin attempts to find it in the backup file
				if (primaryException is NullReferenceException || primaryException is ArgumentNullException || primaryException is InvalidCastException || primaryException is JsonException)
				{
					plugin.Warn("Error reading string '" + path + "' from primary language file, switching to backup...");
					try
					{
						return backup.SelectToken(path).Value<string>();
					}
					// The node also does not exist in the backup file
					catch (NullReferenceException e)
					{
						plugin.Error("Error: Language language string '" + path + "' does not exist. Message can not be sent." + e);
						return null;
					}
					catch (ArgumentNullException e)
					{
						plugin.Error("Error: Language language string '" + path + "' does not exist. Message can not be sent." + e);
						return null;
					}
					catch (InvalidCastException e)
					{
						plugin.Error(e.ToString());
						throw;
					}
					catch (JsonException e)
					{
						plugin.Error(e.ToString());
						throw;
					}
				}
				else
				{
					plugin.Error(primaryException.ToString());
					throw;
				}
			}
		}

		/// <summary>
		/// Gets a regex dictionary from the primary or backup language file.
		/// </summary>
		/// <param name="path">The path to the node.</param>
		/// <returns></returns>
		public static Dictionary<string, string> GetRegexDictionary(string path)
		{
			if (primary == null)
			{
				plugin.Warn("Tried to read regex dictionary '" + path + "' before loading languages.");
				return new Dictionary<string, string>();
			}

			try
			{
				try
				{
					JArray overrideArray = overrides?.SelectToken(path).Value<JArray>();
					return overrideArray?.ToDictionary(k => ((JObject)k).Properties().First().Name, v => v.Values().First().Value<string>());
				}
				catch (Exception) { /* ignore */ }
				JArray primaryArray = primary?.SelectToken(path).Value<JArray>();
				return primaryArray?.ToDictionary(k => ((JObject)k).Properties().First().Name, v => v.Values().First().Value<string>());
			}
			catch (NullReferenceException)
			{
				try
				{
					JArray backupArray = backup?.SelectToken(path).Value<JArray>();
					return backupArray?.ToDictionary(k => ((JObject)k).Properties().First().Name, v => v.Values().First().Value<string>());
				}
				catch (NullReferenceException)
				{
					// Doesn't exist
				}
				catch (ArgumentNullException)
				{
					// Regex array is empty
					return new Dictionary<string, string>();
				}
				catch (InvalidCastException e)
				{
					plugin.Error(e.ToString());
					throw;
				}
				catch (JsonException e)
				{
					plugin.Error(e.ToString());
					throw;
				}
			}
			catch (ArgumentNullException)
			{
				// Regex array is empty
				return new Dictionary<string, string>();
			}
			catch (InvalidCastException e)
			{
				plugin.Error(e.ToString());
				throw;
			}
			catch (JsonException e)
			{
				plugin.Error(e.ToString());
				throw;
			}

			plugin.Warn("Error: Language regex dictionary '" + path + "' does not exist in language file.");
			return new Dictionary<string, string>();
		}
	}
}