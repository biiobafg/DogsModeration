using System.Collections.Generic;
using DogsModeration.Models;
using Rocket.API;

namespace DogsModeration
{
    public class Config : IRocketPluginConfiguration
    {
        public bool BlockJoinCode { get; set; }
        public bool BroadcastBans { get; set; }
        public bool BroadcastUnbans { get; set; }
        public SqlInfo Database { get; set; }
        public List<Webhook> Webhooks { get; set; }
        public void LoadDefaults()
        {
            BlockJoinCode = false;
            BroadcastBans = true;
            BroadcastUnbans = true;
            Database = new SqlInfo();
            Webhooks = new List<Webhook>()
            {
                new("NULL", "BAN", "{name}({steamid}), Reason: {reason}, Duration: {duration}, Punisher: {punisher}", false, "1e1f22", "Player Banned"),
                new("NULL", "KICK", "{name}({steamid}), Reason: {reason}, Punisher: {punisher}", false, "1e1f22", "Player Kicked"),
                new("NULL", "MUTE", "{name}({steamid}), Reason: {reason}, Duration: {duration}, Punisher: {punisher}", false, "1e1f22", "Player Muted"),
                new("NULL", "BLOCK", "{name}({steamid}), ID: {id}, Reason: {reason}, Type: {type}", false, "1e1f22", "Join Blocked"),
                new("NULL", "AUTO", "{name}({steamid}), Farmed Account", false, "1e1f22", "Player AutoBanned"),
                new("NULL", "UNBAN", "{name}({steamid}), Unbanner: {punisher}", false, "1e1f22", "Player Unbanned")
            };
        }
    }
}
