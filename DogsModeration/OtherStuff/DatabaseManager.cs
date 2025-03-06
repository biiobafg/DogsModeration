using System;
using System.Threading.Tasks;
using Dapper;
using DogsModeration.Models;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using static DogsModeration.OtherStuff.Utils;
using static Rocket.Core.Logging.Logger;
namespace DogsModeration.OtherStuff
{
    public class DatabaseManager
    {
        private readonly string _connectionString;
        public DatabaseManager(SqlInfo dbConfig)
        {
            MySqlBaseConnectionStringBuilder sb = new MySqlConnectionStringBuilder
            {
                Server = dbConfig.Server,
                Database = dbConfig.Database,
                UserID = dbConfig.UserName,
                Password = dbConfig.Password,
                Port = dbConfig.Port,
                Pooling = true,
                MaximumPoolSize = 100,
            };
            _connectionString = sb.ToString();

            _ = Run(async () =>
            {
                await Init().ConfigureAwait(false);
            });
        }



        public MySqlConnection Connection
        {
            get
            {
                try
                {
                    return new MySqlConnection(_connectionString);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Couldnt connection to database");
                    return null;
                }
            }
        }




        // not doing punishments if you have an issue, do it yourself
        private async Task Init()
        {
            try
            {
                string playerTbl = @"CREATE TABLE IF NOT EXISTS DG_Players (
                    SteamID BIGINT UNSIGNED NOT NULL PRIMARY KEY,
                    CharacterName VARCHAR(50) NOT NULL,
                    SteamName VARCHAR(50) NOT NULL,
                    IP VARCHAR(45),
                    FirstJoin DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastSeen DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CountryCode VARCHAR(2),
                    ISP VARCHAR(100)
                    );";


                string hwidsTbl = @"CREATE TABLE IF NOT EXISTS DG_PlayerHwids (
                    SteamID BIGINT UNSIGNED NOT NULL,
                    HWID VARCHAR(255) NOT NULL,
                    FirstSeen DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastSeen DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE KEY (SteamID, HWID),
                    FOREIGN KEY (SteamID) REFERENCES DG_Players(SteamID)
                        ON DELETE CASCADE
                );";


                // i hate plugins that lack this, like what if they just change their name and the kid reporting them is too braindead stupid to just get their fucking steamid
                string historyTbl = @"CREATE TABLE IF NOT EXISTS DG_PlayerHistory (
                    SteamID BIGINT UNSIGNED NOT NULL,
                    CharacterName VARCHAR(50) NOT NULL,
                    SteamName VARCHAR(50) NOT NULL,
                    IP VARCHAR(45),
                    Joined DATETIME DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (SteamID, Joined),
                    FOREIGN KEY (SteamID) REFERENCES DG_Players(SteamID)
                        ON DELETE CASCADE
                );";


                // in an ideal world, IP would be removed from this table but i am lazy
                // i should also move all unbanned things to a different table but like who gives a shit + im lazy
                string bansTbl = @"CREATE TABLE IF NOT EXISTS DG_Bans (
                    BanID INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    SteamID BIGINT UNSIGNED NOT NULL,
                    ModeratorID BIGINT UNSIGNED,
                    Reason TEXT NOT NULL,
                    IP VARCHAR(45),
                    BanStart DATETIME DEFAULT CURRENT_TIMESTAMP,
                    BanEnd DATETIME,
                    IsUnbanned TINYINT(1) DEFAULT 0,
                    FOREIGN KEY (SteamID) REFERENCES DG_Players(SteamID)
                        ON DELETE CASCADE
                );";

                using MySqlConnection con = Connection;
                await con.OpenAsync();
                _ = await con.ExecuteAsync(playerTbl);
                _ = await con.ExecuteAsync(hwidsTbl);
                _ = await con.ExecuteAsync(historyTbl);
                _ = await con.ExecuteAsync(bansTbl);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }








    }
}
