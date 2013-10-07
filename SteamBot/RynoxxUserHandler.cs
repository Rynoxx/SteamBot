using System;
using System.Data;
using SteamKit2;
using SteamTrade;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamBot
{
    /// <summary>
    /// A user handler class that implements basic text-based commands entered in
    /// chat or trade chat.
    /// </summary>
    public class RynoxxUserHandler : UserHandler
    {
        private const string MathCmd = "math";
        private const string HelpCmd = "help";
        private const string SetNameCmd = "setname";
        private const string GoogleCmd = "search";
        private const bool UseGoogleAPI = true;
        private const int GoogleSearchIntervall = 15;
        private Stopwatch LastGoogleSearch = new Stopwatch();

        public RynoxxUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            Bot.GetInventory();
            Bot.GetOtherInventory(OtherSID);
        }

        #region Overrides of UserHandler

        /// <summary>
        /// Called when the bot is fully logged in.
        /// </summary>
        public override void OnLoginCompleted()
        {
        }

        /// <summary>
        /// Called when a the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public override bool OnFriendAdd()
        {
            return true;
        }

        public override void OnFriendRemove()
        {
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        /// <summary>
        /// Called whenever a message is sent to the bot.
        /// This is limited to regular and emote messages.
        /// </summary>
        public override void OnMessage(string message, EChatEntryType type)
        {
            var cmd = message.Split(' ');

            if (cmd[0].ToLower() == MathCmd)
            {
                MathCommand(cmd, message, type);
                return;
            }

            if (cmd[0].ToLower() == HelpCmd)
            {
                HelpCommand(cmd, message, type);
                return;
            }

            if (cmd[0].ToLower() == SetNameCmd)
            {
                SetNameCommand(cmd, message, type);
                return;
            }

            #region GoogleCmd
            if (cmd[0].ToLower() == GoogleCmd)
            {
                if (UseGoogleAPI)
                {
                    if (LastGoogleSearch.IsRunning)
                    {
                        var ms = LastGoogleSearch.ElapsedMilliseconds;

                        if (ms < GoogleSearchIntervall * 1000)
                        {
                            var remaining = GoogleSearchIntervall - (ms / 1000);

                            Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.SteamFriends.GetFriendPersonaName(OtherSID) + ", you have to wait another " + remaining + " seconds before searching again.");

                            return;
                        }
                    }

                    GoogleCommand(cmd, message, type);
                    LastGoogleSearch.Restart();
                }
                else
                {
                    GoogleCommand(cmd, message, type);
                }
            return;
            }
            #endregion
        }

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public override bool OnTradeRequest()
        {
            return false;
        }

        public override void OnTradeError(string error)
        {
            Log.Error(error);
        }

        public override void OnTradeTimeout()
        {
            Log.Warn("Trade timed out.");
        }

        public override void OnTradeInit()
        {
            Trade.SendMessage("Hi.");
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // whatever.   
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // whatever.
        }

        public override void OnTradeMessage(string message)
        {
            ProcessTradeMessage(message);
        }

        public override void OnTradeReady(bool ready)
        {
            if (!IsAdmin)
            {
                Trade.SendMessage("You are not my master.");
                Trade.SetReady(false);
                return;
            }

            Trade.SetReady(true);
        }

        public override void OnTradeAccept()
        {
            if (IsAdmin)
            {
                bool ok = Trade.AcceptTrade();

                if (ok)
                {
                    Log.Success("Trade was Successful!");
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                }
            }
        }

        #endregion

        public void HelpCommand(string[] cmd, string message, EChatEntryType type)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, type, "No.");
        }

        public string ProperString(string Str)
        {
            return HttpUtility.HtmlDecode(Str).Replace("<b>...</b>", "").Replace("<b>", "").Replace("</b>", "").Replace("Â", "");
        }

        #region GoogleCmd
        public void GoogleCommand(string[] cmd, string search, EChatEntryType type)
        {
            string searchString = "";

            foreach (string str in cmd)
            {
                if (str.ToLower() == GoogleCmd)
                {
                    continue;
                }

                searchString = searchString + str;
            }

            if (!UseGoogleAPI)
            {
                string GoogleSearch = "https://google.com/#q={0}";
                Bot.SteamFriends.SendChatMessage(OtherSID, type, string.Format(GoogleSearch, searchString));
            }
            else
            {
                try
                {
                    ISearchResult searchClass = new GoogleSearch(searchString);
                    var list = searchClass.Search();
                    var searchType = list[0];
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "Your search for \"" + searchString + "\" returned the following:");
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, ProperString(searchType.url));
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, ProperString(searchType.title));
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, ProperString(searchType.content));
                }
                catch (Exception exc)
                {
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, "Sorry " + Bot.SteamFriends.GetFriendPersonaName(OtherSID) + ", I couldn't handle your search (\"" + searchString + "\").");
                    Bot.SteamFriends.SendChatMessage(OtherSID, type, exc.Message);
                }
            }
        }
        #endregion
        
        public void SetNameCommand(string[] cmd, string message, EChatEntryType type)
        {
            if (IsAdmin)
            {
                string name = "";
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "You are not my master! >:|");
            }
        }

        public void MathCommand(string[] cmd, string message, EChatEntryType type)
        {
            string reply = "";

            foreach (string str in cmd)
            {
                if (str.ToLower() == MathCmd)
                {
                    continue;
                }

                reply = reply + str;
            }

            try
            {
                var result = Calc(reply);
                Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.SteamFriends.GetFriendPersonaName(OtherSID) + ", " + reply + " = " + result);
            }
            catch (Exception exc)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Sorry " + Bot.SteamFriends.GetFriendPersonaName(OtherSID) + ", but I couldn't calculate \"" + reply + "\".");
                Bot.SteamFriends.SendChatMessage(OtherSID, type, exc.Message);
            }
        }

        public static object Calc(string toCalc)
        {
            Type scriptType = Type.GetTypeFromCLSID(Guid.Parse("0E59F1D5-1FBE-11D0-8FF2-00A0D10038BC"));

            dynamic obj = Activator.CreateInstance(scriptType, false);
            obj.Language = "javascript";

            var res = obj.Eval(toCalc);

            return res;
        }

        private void ProcessTradeMessage(string message)
        {
        }
    }


    #region SearchAPI
    public struct SearchType
    {
        public string url;
        public string title;
        public string content;
        public FindingEngine engine;
        public enum FindingEngine { Google, Bing, GoogleAndBing };
    }

    public interface ISearchResult
    {
        SearchType.FindingEngine Engine { get; set; }
        string SearchExpression { get; set; }
        List<SearchType> Search();
    }

    public class BingSearch : ISearchResult
    {
        public BingSearch(string searchExpression)
        {
            this.Engine = SearchType.FindingEngine.Bing;
            this.SearchExpression = searchExpression;
        }
        public SearchType.FindingEngine Engine { get; set; }
        public string SearchExpression { get; set; }

        public List<SearchType> Search()
        {
            // our appid from bing - 3F4313687C37F1....23A79E181D0A25
            const string urlTemplate = @"http://api.search.live.net/json.aspx?AppId=3F4313687C37F1...23A79E181D0A25&Market=en-US&Sources=Web&Adult=Strict&Query={0}&Web.Count=50";
            const string offsetTemplate = "&Web.Offset={1}";
            var resultsList = new List<SearchType>();
            int[] offsets = { 0, 50, 100, 150 };
            Uri searchUrl;
            foreach (var offset in offsets)
            {
                if (offset == 0)
                    searchUrl = new Uri(string.Format(urlTemplate, SearchExpression));
                else
                    searchUrl = new Uri(string.Format(urlTemplate + offsetTemplate,
                                        SearchExpression, offset));

                var page = new WebClient().DownloadString(searchUrl);
                var o = (JObject)JsonConvert.DeserializeObject(page);

                var resultsQuery =
                  from result in o["SearchResponse"]["Web"]["Results"].Children()
                  select new SearchType
                  {
                      url = result.Value<string>("Url").ToString(),
                      title = result.Value<string>("Title").ToString(),
                      content = result.Value<string>("Description").ToString(),
                      engine = this.Engine
                  };

                resultsList.AddRange(resultsQuery);
            }
            return resultsList;
        }
    }

    public class GoogleSearch : ISearchResult
    {
        public GoogleSearch(string searchExpression)
        {
            this.Engine = SearchType.FindingEngine.Google;
            this.SearchExpression = searchExpression;
        }

        public SearchType.FindingEngine Engine { get; set; }
        public string SearchExpression { get; set; }

        public List<SearchType> Search()
        {
            const string urlTemplate = @"http://ajax.googleapis.com/ajax/services/search/web?v=1.0&rsz=1&safe=active&q={0}&start={1}&userip={2}";
            var resultsList = new List<SearchType>();

            Random r = new Random();
            string RandomIP = r.Next(1, 255) + "." + r.Next(1, 255) + "." + r.Next(1, 255) + "." + r.Next(1, 255);
            var searchUrl = new Uri(string.Format(urlTemplate, SearchExpression, 0, RandomIP));
            var page = new WebClient().DownloadString(searchUrl);
            var o = (JObject)JsonConvert.DeserializeObject(page);

            var resultsQuery =
                from result in o["responseData"]["results"].Children()
                select new SearchType
                {
                    url = result.Value<string>("url").ToString(),
                    title = result.Value<string>("title").ToString(),
                    content = result.Value<string>("content").ToString(),
                    engine = this.Engine
                };

            resultsList.AddRange(resultsQuery);
            return resultsList;
        }
    }
    #endregion
}