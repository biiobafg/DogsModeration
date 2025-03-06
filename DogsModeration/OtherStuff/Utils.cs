using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DogsModeration.Models;
using Newtonsoft.Json;
using RestSharp;
using SDG.Unturned;
using static Rocket.Core.Logging.Logger;
namespace DogsModeration.OtherStuff
{
    public static class Utils
    {

        public static Task Run(System.Action action)
        {
            return Task.Run(() =>
            {
                try
                {
                    action();

                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            });
        }

        // this was taken from MCrows moderation plugin, like #shoutout that guy
        private static readonly Dictionary<char, int> timePeriods = new()
    {
      {
        'd',
        86400
      },
      {
        'h',
        3600
      },
      {
        'm',
        60
      },
      {
        's',
        1
      }
    };
        public static TimeSpan? GetDuration(IEnumerable<string> args)
        {
            int result1 = 0;
            if (args != null && args.Count() > 0 && !int.TryParse(args.ElementAt(0), out result1))
            {
                foreach (string source in args)
                {
                    foreach (KeyValuePair<char, int> timePeriod in timePeriods)
                    {
                        if (source.Contains(timePeriod.Key))
                        {
                            if (int.TryParse(source.Trim(timePeriod.Key), out int result2))
                            {
                                result1 += result2 * timePeriod.Value;
                                break;
                            }
                            break;
                        }
                    }
                }
            }
            return result1 == 0 ? new TimeSpan?() : new TimeSpan?(TimeSpan.FromSeconds(result1));
        }

        public static string Format(TimeSpan span)
        {
            if (span == null)
            {
                return null;
            }

            StringBuilder sb = new();

            if (span.Days >= 1)
            {
                _ = sb.Append($" {span.Days}d");
            }
            if (span.Hours >= 1)
            {
                _ = sb.Append($" {span.Hours}h");
            }
            if (span.Minutes >= 1)
            {
                _ = sb.Append($" {span.Minutes}m");
            }
            if (span.Seconds >= 1)
            {
                _ = sb.Append($" {span.Seconds}s");
            }

            string built = sb.ToString();

            return string.IsNullOrEmpty(built) ? null : built;
        }

        public static void SendWebhook(string type, string name = "", string steamid = "", string punisher = "", string duration = "", string reason = "",
            string id = "", string type2 = "")
        {

            // there is probably a wayyy better way of doing this, or i could just use a webhook library like shimmys but honetly
            // who gives a shit, it fucking works
            List<Webhook> hooks = Main.instance.Configuration.Instance.Webhooks.FindAll(x => x.Type.Contains(type));
            if (hooks == null || hooks.Count == 0)
            {
                return;
            }

            foreach (Webhook hook in hooks)
            {
                string msg = hook.Message;
                // this should really be put in another method but its not that large so i really dont give a shit
                msg = msg.Replace(',', '\n')
                                .Replace("{reason}", reason)
                                 .Replace("{name}", name)
                                 .Replace("{punisher}", punisher)
                                 .Replace("{steamid}", steamid)
                                 .Replace("{duration}", duration)
                                 .Replace("{id}", id)
                                 .Replace("{type}", type2);

                _ = Run(async () =>
                {
                    if (string.IsNullOrEmpty(hook.URL) || hook.URL == "NULL")
                    {
                        return;
                    }

                    // i love how outdated the mono version is for unturned
                    // httpclient, unitywebrequest every thing else is FUCKED
                    // i love adding other libraries to just send a json to a webhook
                    RestClient client = new(hook.URL);
                    RestRequest request = new(Method.POST);


                    string ftr = Provider.serverName;
                    string icnUrl = Provider.configData.Browser.Icon;

                    var payload = new
                    {
                        content = "",
                        embeds = new[]
                                {
                                new
                                {
                                    title = hook.Title,
                                    description = msg,
                                    color = int.Parse(hook.Color.Trim('#'), NumberStyles.HexNumber),
                                    timestamp = DateTime.UtcNow.ToString("u"),
                                    footer = new
                                    {
                                        text = Provider.serverName,
                                        icon_url = Provider.configData.Browser.Icon
                                    }
                                }
                            }
                    };


                    string json = JsonConvert.SerializeObject(payload);
                    StringContent content = new(json, Encoding.UTF8, "application/json");
                    try
                    {
                        _ = request.AddHeader("Content-Type", "application/json");
                        _ = request.AddParameter("application/json", json, ParameterType.RequestBody);
                        _ = await client.ExecutePostTaskAsync(request);
                    }
                    catch
                    {
                    }
                });
            }


        }
    }
}
