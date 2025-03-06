using System;
using System.Collections.Generic;
using DogsModeration.OtherStuff;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace DogsModeration.Commands
{
    public class Kick : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "kick";

        public string Help => "throw new System.NotImplementedException()";

        public string Syntax => "throw new System.NotImplementedException()";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] args)
        {
            if (args.Length < 1)
            {
                UnturnedChat.Say(caller, "Invalid args use /kick {playerid / name} {reason}");
                return;
            }
            try
            {

                // [0] = id / name
                // [1] = reason
                string reason = "";
                UnturnedPlayer player = UnturnedPlayer.FromName(args[0]);
                player ??= UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(ulong.Parse(args[0])));
                if (player == null)
                {
                    UnturnedChat.Say(caller, "Player not found!");
                }
                if (args.Length > 1)
                {
                    reason = args[1];
                }

                player.Kick(reason);
                UnturnedChat.Say(caller, Main.instance.Translate("KICKED", player.CharacterName));

                _ = Utils.Run(() =>
                {
                    Utils.SendWebhook("KICK", player.CharacterName, player.Id, caller is UnturnedPlayer plr ? plr.Id : "80085", "", reason);
                });

            }
            catch (Exception)
            {
                UnturnedChat.Say(caller, "Player not found!");
            }
        }
    }
}
