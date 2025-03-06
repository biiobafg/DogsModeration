using System;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using static DogsModeration.OtherStuff.Utils;
namespace DogsModeration.OtherStuff
{
    public static class ModerationStuff
    {
        public static void BanPlayer(ulong playerId, string reason, DateTime? banend, Rocket.API.IRocketPlayer caller)
        {
            ulong ModeratorID;
            if (caller is UnturnedPlayer player)
            {
                ModeratorID = (ulong)player.CSteamID;
            }
            else
            {
                ModeratorID = 80085; // lol boobs - for console
            }

            if (string.IsNullOrEmpty(reason))
            {
                reason = "T_T";
            }
            _ = Run(async () =>
            {
                bool flag = await Main.instance.databaseManager.AddBan(playerId, reason, banend, ModeratorID);
                if (flag)
                {
                    UnturnedChat.Say(caller, $"Banned {playerId}");

                    string Name = await Main.instance.databaseManager.GetName(playerId);
                    if (Main.instance.Configuration.Instance.BroadcastBans)
                    {

                        string ToSend = banend.HasValue
                            ? Main.instance.Translate("BANBROADCAST", Name, reason, banend)
                            : Main.instance.Translate("BANBROADCASTPERM", Name, reason);
                        UnturnedChat.Say(ToSend);
                    }
                    SendWebhook("BAN", Name, playerId.ToString(), ModeratorID.ToString(), banend.HasValue ? Format(banend.Value - DateTime.UtcNow) : "Permenant", reason);
                }
            });

            if (banend.HasValue)
            {
                Main.instance.Kick(new Steamworks.CSteamID(playerId), "BAN", reason, Format(banend.Value - DateTime.UtcNow));
            }
            else
            {
                Main.instance.Kick(new Steamworks.CSteamID(playerId), "PERM", reason);
            }
        }
    }
}
