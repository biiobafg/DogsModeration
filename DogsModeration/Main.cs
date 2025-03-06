using System;
using System.Collections.Generic;
using System.Linq;
using DogsModeration.Models;
using DogsModeration.OtherStuff;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using SDG.Unturned;
using Steamworks;
using static DogsModeration.OtherStuff.Utils;
namespace DogsModeration
{


    // i started working on this at 3:50am, i have finished this at 7:45am - good or bad?
    public class Main : RocketPlugin<Config>
    {
        public static Main instance;
        public DatabaseManager databaseManager;
        protected override void Load()
        {
            instance = this;
            Provider.onCheckBanStatusWithHWID += onCheckBan;
            databaseManager = new(Configuration.Instance.Database);

            // now incase this plugin breaks i would register the vanilla ban kick unban commands again under different names however the issue with that is
            // it needs to be an IRocketCommand to register through rocket, and since im too lazy to go see how it works for the vanilla commands
            // im just not going to do it


        }

        protected override void Unload()
        {
            Provider.onCheckBanStatusWithHWID -= onCheckBan;
        }
        // isban, explain and remaining will not matter since im just going to provider.reject them if i need them kicked
        // reasons for this: Plugin {message} is prettier
        private void onCheckBan(SteamPlayerID playerID, uint ip3, ref bool isban, ref string explain, ref uint remaining)
        {
            // from every test i have done, if you attempt to null check playerid it will throw an object reference not set to an instance of an object (the null error)
            string ip = Parser.getIPFromUInt32(ip3);
            if (Configuration.Instance.BlockJoinCode)
            {
                // now unfortunately join codes dont work on the server that im using to test this but i am assuming that the ip will just be 0 if they join through a code
                if (ip3 == 0)
                {
                    Kick(playerID.steamID, "CODEDISABLED");
                    return;
                }
            }


            // something in this is triggering mono to send its stupid paragraph about it being outdated,
            // i cant do anything about that
            // im not findind what is triggering it in this
            // fuck you

            _ = Run(async () =>
            {
                List<string> hwids = playerID.GetHwids()?.Select(Convert.ToBase64String).ToList() ?? new List<string>();

                if (hwids.Count == 0)
                {
                    // need to autoban

                    ModerationStuff.BanPlayer((ulong)playerID.steamID, "X_X", null, new ConsolePlayer());

                }
                _ = await databaseManager.AddPlayer((ulong)playerID.steamID, playerID.characterName ?? "Unknown",
                        playerID.playerName ?? "Unknown", ip.ToString(), hwids);



                BanCheck ban = await databaseManager.GetBan((ulong)playerID.steamID, ip.ToString(), hwids);


                if (ban == null || (ban.BanEnd != null && ban.BanEnd < DateTime.UtcNow))
                {
                    return;
                }

                if (ban.BanEnd == null)
                {
                    Kick(playerID.steamID, "PERM", ban.Reason);
                    CheckEvade(playerID, ip, ban);
                    return;
                }

                TimeSpan span = ban.BanEnd.Value - DateTime.UtcNow;
                Kick(playerID.steamID, "BAN", ban.Reason, Format(span));
                CheckEvade(playerID, ip, ban);
            });
        }

        private void CheckEvade(SteamPlayerID playerID, string ip, BanCheck ban)
        {
            if (ban == null)
            {
                return;
            }

            if (ban.SteamID != (ulong)playerID.steamID)
            {
                // not the same so its someone on an alt trying to evade
                string type = ip == ban.IP ? "IP" : "HWID";
                SendWebhook("BLOCK", playerID.playerName, playerID.steamID.ToString(), "", "", ban.Reason, ban.BanID.ToString(), type);
            }
        }

        public override TranslationList DefaultTranslations => new()
        {
            // i hate doing translations, hardcoded messages are easier - who the fuck changes these anyways?
            { "BAN", "Banned: {0} : {1}" }, // <- put your discord invite or something stupid in this
            { "PERM", "Banned: {0}" },
            { "CODEDISABLED", "Join code is disabled on this server!" },
            { "BANBROADCAST", "{0} has been banned for {2} {1}" },
            { "BANBROADCASTPERM", "{0} has been banned for {1}" },
            { "UNBANNED", "{0} has been unbanned!" },
            // not adding kick announcement, its stupid. What kind of sissified server owner even uses kicks (just ban them #ez)
            { "KICKED", "kicked {0}"}
        };

        public void Kick(CSteamID playerId, string TranslationKey, string arg0 = null, string arg1 = null)
        {
            // incase you wonder why i use provider.kick right after, its because for some bizzarre reason, when i first started using this to kick people
            // it wouldnt kick them for other players / the server, only on their screen (maybe because its not made to be used like this) - calling kick right after fixes that
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Provider.reject(playerId, ESteamRejection.PLUGIN, Translate(TranslationKey, arg0, arg1));
                Provider.kick(playerId, Translate(TranslationKey));

            });
        }
        public void RawKick(CSteamID playerId, string reason)
        {
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Provider.reject(playerId, ESteamRejection.PLUGIN, reason);
                Provider.kick(playerId, reason);
            });
        }
    }
}
