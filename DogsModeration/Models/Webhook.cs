using System.Xml.Serialization;
namespace DogsModeration.Models
{
    public class Webhook
    {
        public Webhook(string url, string type, string message, bool inline, string color, string title)
        {
            URL = url;
            Title = title;
            Type = type;
            Message = message;
            Inline = inline;
            Color = color;
        }
        public Webhook()
        {

        }

        [XmlAttribute]
        public string URL { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
        [XmlAttribute]
        public bool Inline { get; set; }
        [XmlAttribute]
        public string Color { get; set; }
        [XmlAttribute]
        public string Title { get; set; }
        public string Message { get; set; }

    }
}
