using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Dapper;
using DogsModeration.Models;
using Newtonsoft.Json.Linq;
using static Rocket.Core.Logging.Logger;
namespace DogsModeration.OtherStuff
{
    public static class Ext
    {
        public static async Task<BanCheck> GetBan(this DatabaseManager database, ulong steamId, string ip, List<string> hwids)
        {
            try
            {
                using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
                await con.OpenAsync();
                // holy fuck its beautiful
                string qry = @"
                        SELECT 
                            b.BanID,
                            b.SteamID,
                            b.IP,
                            b.Reason,
                            b.BanEnd
                        FROM DG_Bans b
                        LEFT JOIN DG_PlayerHwids h ON b.SteamID = h.SteamID
                        WHERE b.IsUnbanned = 0
                        AND (
                            b.SteamID = @SteamID
                            OR (@IP IS NOT NULL AND @IP != '0' AND b.IP = @IP)
                            OR (h.HWID IN @Hwids)
                        )
                        AND (b.BanEnd IS NULL OR b.BanEnd > NOW())
                        ORDER BY 
                            CASE 
                                WHEN b.BanEnd IS NULL THEN 0
                                ELSE 1
                            END,
                            b.BanEnd DESC
                        LIMIT 1;";

                BanCheck result = await con.QueryFirstOrDefaultAsync<BanCheck>(
                    qry,
                    new
                    {
                        SteamID = steamId,
                        IP = ip,
                        Hwids = hwids
                    }
                );

                return result;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return null;
            }
        }
        // add player then add the isp and countrycode (so i dont have to trycatch getting the isp info and ensure that it actually inserts it
        public static async Task<bool> AddPlayer(this DatabaseManager database, ulong steamid,
        string CharName,
        string steamname,
        string IP,
        List<string> hwids)
        {
            bool result = await AddPlayerInternal(database, steamid, CharName, steamname, IP);
            _ = await AddHwids(database, steamid, hwids).ConfigureAwait(false);
            _ = await UpdateISP(database, steamid, IP).ConfigureAwait(false);
            await AddHistory(database, steamid, CharName, steamname, IP).ConfigureAwait(false);
            return result;
        }
        private static async Task AddHistory(this DatabaseManager database, ulong steamid, string charname, string steamname, string ip)
        {
            using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
            await con.OpenAsync();

            string qry = @"INSERT IGNORE INTO DG_PlayerHistory (SteamID, CharacterName, SteamName, IP) VALUES 
                    (@SteamID, @CharacterName, @SteamName, @IP)";

            _ = await con.ExecuteAsync(qry, new { SteamID = steamid, CharacterName = charname, SteamName = steamname, IP = ip });
        }
        private static async Task<bool> AddPlayerInternal(this DatabaseManager database, ulong steamid,
            string CharName,
            string steamname,
            string IP)
        {
            try
            {
                using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
                await con.OpenAsync();

                string qry = @"INSERT INTO DG_Players(
                    SteamID, CharacterName, SteamName, IP) VALUES (@SteamID, @CharacterName, @SteamName, @IP)
                    ON DUPLICATE KEY UPDATE
                        CharacterName = @Charactername,
                        SteamName = @SteamName,
                        IP = @IP,
                        LastSeen = NOW()"; ;

                return await con.ExecuteAsync(qry, new { SteamID = steamid, CharacterName = CharName, SteamName = steamname, IP }) > 0;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        public static async Task<bool> UpdateISP(this DatabaseManager database, ulong steamid, string ip)
        {
            // this should be moved to another class and just have this for only being to update the players isp once we have that, but i'll just call this with configureawait(false)
            // then once its downloaded the isp info it'll update the database, Right?
            try
            {
                string json;
                using (WebClient webClient = new())
                {
                    json = await webClient.DownloadStringTaskAsync(new Uri($"http://ip-api.com/json/{ip}?fields=status,countryCode,isp"));
                }

                JObject jobject = JObject.Parse(json);
                if ((string)jobject["status"] == "success")
                {
                    string CountryCode = (string)jobject["countryCode"];
                    string ISP = (string)jobject["isp"];

                    if (string.IsNullOrEmpty(ISP) || string.IsNullOrEmpty(CountryCode))
                    {
                        return false;
                    }
                    // this should be fine since DG_Players cant have duplicates so it can override 1 thing max
                    string qry = @"UPDATE DG_Players SET ISP = @ISP, CountryCode = @CountryCode WHERE SteamID = @SteamID;";
                    using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
                    await con.OpenAsync();
                    return await con.ExecuteAsync(qry, new { ISP, CountryCode, SteamID = steamid }) > 0;
                }
            }
            catch (Exception)
            {
                // trycatch with empty catch i need a goth mommy to RUIN me
            }
            return false;
        }


        public static async Task<bool> AddHwids(this DatabaseManager database, ulong steamid, List<string> hwids)
        {
            int count = hwids.Count;
            int dec = 0;
            foreach (string hwid in hwids)
            {
                if (await AddHwid(database, steamid, hwid))
                {
                    dec++;
                }
            }
            return dec == count;
        }
        public static async Task<bool> AddHwid(this DatabaseManager database, ulong steamid, string hwid)
        {
            try
            {
                using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
                await con.OpenAsync();

                string qry = @"INSERT INTO DG_PlayerHwids (SteamID, HWID) VALUES (@SteamID, @HWID)
                    ON DUPLICATE KEY UPDATE LastSeen = NOW();";

                return await con.ExecuteAsync(qry, new { SteamID = steamid, HWID = hwid }) > 0;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }



        public static async Task<bool> UnbanPlayer(this DatabaseManager database, ulong playerid)
        {
            using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
            await con.OpenAsync();

            return await con.ExecuteAsync("UPDATE DG_Bans SET IsUnbanned = 1 WHERE SteamID = @SteamID", new { SteamID = playerid }) > 0;
        }
        public static async Task<bool> AddBan(this DatabaseManager database, ulong steamid, string reason, DateTime? end, ulong mod)
        {
            _ = await MakeSureItExists(database, steamid);
            bool result = await AddBanInternal(database, steamid, reason, end, mod);
            await AddIpToBan(database, steamid, mod, reason).ConfigureAwait(false);
            return result;
        }
        private static async Task<bool> AddBanInternal(this DatabaseManager database, ulong steamid, string reason, DateTime? end, ulong mod)
        {
            using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
            con.Open();

            string qry = "INSERT INTO DG_Bans (SteamID, ModeratorID, Reason, BanEnd) VALUES (@SteamID, @ModeratorID, @Reason, @BanEnd)";

            return await con.ExecuteAsync(qry, new { SteamID = steamid, ModeratorID = mod, Reason = reason, BanEnd = end }) > 0;
        }
        public static async Task<string> GetName(this DatabaseManager database, ulong playerId)
        {
            using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
            con.Open();
            return await con.QueryFirstOrDefaultAsync<string>("SELECT SteamName FROM DG_Players WHERE SteamID = @SteamID", new { SteamID = playerId });
        }
        private static async Task AddIpToBan(this DatabaseManager database, ulong steamid, ulong mod, string reason)
        {
            using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
            con.Open();

            string PlayersIp = await con.QuerySingleOrDefaultAsync<string>("SELECT IP FROM DG_Players WHERE SteamID = @SteamID LIMIT 1", new { SteamID = steamid });
            if (PlayersIp is null or "0")
            {
                return;
            }

            _ = await con.ExecuteAsync("UPDATE DG_Bans SET IP = @IP WHERE SteamID = @SteamID AND ModeratorID = @ModeratorID AND Reason = @Reason AND IP = NULL LIMIT 1 ORDER BY BanID DESC", new { SteamID = steamid, ModeratorID = mod, Reason = reason });
        }

        // so this exists because the steamid field is tied to the players, so if the player doesnt exist in the database it is going to fucking explode (it'll just not ban them)
        public static async Task<bool> MakeSureItExists(this DatabaseManager database, ulong steamid)
        {
            using MySql.Data.MySqlClient.MySqlConnection con = database.Connection;
            con.Open();

            string result = await con.QuerySingleOrDefaultAsync<string>("SELECT SteamName FROM DG_Players WHERE SteamID = @SteamID LIMIT 1", new { SteamID = steamid });
            if (string.IsNullOrEmpty(result))
            {
                Log("PLAYER NOT EXIST, CREATING");
                return await AddPlayerInternal(database, steamid, steamid.ToString(), steamid.ToString(), "0");
            }
            return true;
        }

    }
}
