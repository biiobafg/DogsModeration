namespace DogsModeration.Models
{
    public class SqlInfo
    {
        public SqlInfo()
        {
            Server = "LOCALHOST";
            Database = "DOGS";
            UserName = "ROOT";
            Password = "sunglassesEmoji";
            Port = 3306;
        }
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort Port { get; set; }
    }
}
