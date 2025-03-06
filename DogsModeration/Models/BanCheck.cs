using System;

namespace DogsModeration.Models
{
    public class BanCheck
    {
        public int BanID { get; set; }
        public ulong SteamID { get; set; }
        public string Reason { get; set; }
        public string IP { get; set; }
        public DateTime? BanEnd { get; set; }
    }
}
