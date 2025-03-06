using System.Collections.Generic;
using DogsModeration.OtherStuff;
using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using static DogsModeration.OtherStuff.Utils;
namespace DogsModeration.Commands
{
    public class Unban : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "unban";

        public string Help => "throw new NotImplementedException()";

        public string Syntax => "throw new NotImplementedException()";

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


            string reason = null;

            if (args[0].Length != 17 || !ulong.TryParse(args[0], out ulong playerid))
            {
                UnturnedChat.Say(caller, "Player not found!");
                return;
            }

            if (args.Length > 1)
            {
                // [1] should be reason
                reason = args[1];
            }

            ulong modId = caller is UnturnedPlayer player2 ? (ulong)player2.CSteamID : 80085;
            _ = Run(async () =>
            {
                bool flag = await Main.instance.databaseManager.UnbanPlayer(playerid);
                if (!flag)
                {
                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        UnturnedChat.Say(caller, $"{playerid} Is not banned!");
                    });
                    return;
                }
                UnturnedChat.Say(caller, $"unbanned {playerid}");

                string name = await Main.instance.databaseManager.GetName(playerid);

                if (Main.instance.Configuration.Instance.BroadcastUnbans)
                {

                    UnturnedChat.Say(Main.instance.Translate("UNBANNED", name));
                }

                SendWebhook("UNBAN", name, playerid.ToString(), modId.ToString(), "", reason);
            });


        }

    }
}
