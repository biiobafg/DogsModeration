using System;
using System.Collections.Generic;
using System.Linq;
using DogsModeration.OtherStuff;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace DogsModeration.Commands
{
    public class Ban : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "ban";

        public string Help => "throw new System.NotImplementedException()";

        public string Syntax => "throw new System.NotImplementedException()";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] args)
        {
            if (args.Length < 1)
            {
                UnturnedChat.Say(caller, "Invalid args use /ban {playerid / name} {reason} {length}");
                return;
            }

            // [0] = id / name
            // [1] = reason
            // [2] = length


            UnturnedPlayer player = UnturnedPlayer.FromName(args[0]);
            string reason = null;
            DateTime? banend = null;


            ulong playerid;
            if (player == null)
            {
                if (args[0].Length != 17 || !ulong.TryParse(args[0], out playerid))
                {
                    UnturnedChat.Say(caller, "Player not found!");
                    return;
                }
            }
            else
            {
                playerid = (ulong)player.CSteamID;
            }

            if (args.Length > 1)
            {
                // [1] should be reason
                reason = args[1];
            }
            if (args.Length > 2)
            {
                // [2] should be like the end date
                TimeSpan? span = Utils.GetDuration(args.Skip(2));
                if (span.HasValue)
                {
                    banend = DateTime.UtcNow.Add(span.Value);
                }
            }
            if (playerid == 0)
            {
                UnturnedChat.Say(caller, "Player not found!");
                return;
            }

            ModerationStuff.BanPlayer(playerid, reason, banend, caller);

        }
    }
}
