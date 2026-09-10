using System;

namespace SCPDiscordPlugin.Utilities;

public class DiscordCommandSender : CommandSender
{
  public override string SenderId => $"{DiscordUserID}@discord";

  public override string Nickname => DiscordUsername;

  public string DiscordUsername { get; set; }
  public ulong DiscordUserID { get; set; }

  public override ulong Permissions => ulong.MaxValue;

  public override byte KickPower => byte.MaxValue;

  public override bool FullPermissions => true;

  public DiscordCommandSender(ulong discordUserID, string discordUsername)
  {
    DiscordUserID = discordUserID;
    DiscordUsername = discordUsername;
  }

  public override bool Available()
  {
    return true;
  }

  public override void Print(string text)
  {
	  ServerConsole.AddLog($"[ScpDiscord|{SenderId}] {text}");
  }

  public override void Print(string text, ConsoleColor c)
  {
	  ServerConsole.AddLog($"[ScpDiscord|{SenderId}] {text}", c);
  }

  public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay) { /* ignored */ }
}
