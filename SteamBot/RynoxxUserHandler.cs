using System;
using System.Data;
using SteamKit2;
using SteamTrade;
using ChatterBotAPI;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
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
        private const string AddCmd = "add";
        private const string RemoveCmd = "remove";
        private const string AddCratesSubCmd = "crates";
        private const string AddWepsSubCmd = "weapons";
        private const string AddMetalSubCmd = "metal";
        private const string AddAllSubCmd = "all";
        private const string MathCmd = "math";
        private const string HelpCmd = "help";
        private const string GoogleCmd = "search";
        private const string LaptopCmd = "laptop";
        private const string ToggleGAPICMD = "togglegoogleapi";
        private const int GoogleSearchIntervall = 15;
        private const int MsPerLetter = 40;
        private bool UseGoogleAPI = true;
        private ChatterBotFactory BotFactory = new ChatterBotFactory();
        private ChatterBot CleverBot;
        private ChatterBotSession CleverBotSession;
        private Stopwatch LastGoogleSearch = new Stopwatch();

        public RynoxxUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            Bot.GetInventory();
            Bot.GetOtherInventory(OtherSID);
            CleverBot = BotFactory.Create(ChatterBotType.CLEVERBOT);
            CleverBotSession = CleverBot.CreateSession();
        }

        #region Overrides of AdminUserHandler
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
            EChatEntryType IsTyping = EChatEntryType.Typing;
            Bot.SteamFriends.SendChatMessage(OtherSID, IsTyping, message + message + message);
            var cmd = message.Split(' ');

            if (cmd[0].ToLower() == MathCmd)
            {
                MathCommand(cmd, message, type);
                return;
            }

            if (cmd[0].ToLower() == LaptopCmd)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Laptop...? LAPTOP!? Filthy laptop peasant!");
                return;
            }

            if (cmd[0].ToLower() == HelpCmd)
            {
                HelpCommand(cmd, message, type);
                return;
            }

            if (cmd[0].ToLower() == ToggleGAPICMD)
            {
                ToggleAPICommand(cmd, message, type);
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

            string reply = "";
            reply = ProperString(CleverBotSession.Think(message));
            Thread.Sleep(reply.Length * MsPerLetter);
            Bot.SteamFriends.SendChatMessage(OtherSID, type, reply);
        }

        #endregion

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

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public override bool OnTradeRequest()
        {
            return IsAdmin;
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
            Trade.SendMessage("Success. (Type " + HelpCmd + " for commands.)");
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

        #region HelpCmd
        public void HelpCommand(string[] cmd, string message, EChatEntryType type)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, type, "Hello " + Bot.SteamFriends.GetFriendPersonaName(OtherSID) + ", I'm a bot created by Rynoxx, currently there's 3 main functions I have, googling, calculating math and chatting.");
            Bot.SteamFriends.SendChatMessage(OtherSID, type, "To google something type \"" + GoogleCmd + " [Search Query]\".");
            Bot.SteamFriends.SendChatMessage(OtherSID, type, "To toggle the use of google api type \"" + ToggleGAPICMD + "\".");
            Bot.SteamFriends.SendChatMessage(OtherSID, type, "To calculate something type \"" + MathCmd + " [Equation]\".");
            Bot.SteamFriends.SendChatMessage(OtherSID, type, "To chat with me, simply type something that doesn't start with " + HelpCmd + ", " + GoogleCmd + ", " + MathCmd + " or any of the hidden commands (which are very few, so don't worry).");
        }
        #endregion

        public string ProperString(string Str)
        {
            return HttpUtility.HtmlDecode(Str).Replace("<b>...</b>", "").Replace("<b>", "").Replace("</b>", "").Replace("Â", "");
        }

        #region GoogleCmd
        public void ToggleAPICommand(string[] cmd, string search, EChatEntryType type)
        {
            UseGoogleAPI = !UseGoogleAPI;
            if (UseGoogleAPI)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "The use of Google search API is now enabled.");
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "The use of Google search API is now disabled.");
            }
        }

        public void GoogleCommand(string[] cmd, string search, EChatEntryType type)
        {
            string searchString = "";

            foreach (string str in cmd)
            {
                if (str.ToLower() == GoogleCmd) continue;
                searchString = searchString + " " + str;
            }

            searchString = searchString.Trim();

            if (!UseGoogleAPI)
            {
                string GoogleSearch = "https://google.com/?q={0}#q={0}";
                Bot.SteamFriends.SendChatMessage(OtherSID, type, string.Format(GoogleSearch, HttpUtility.UrlEncode(searchString)));
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

        #region MathCmd
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
        #endregion

        #region TradeCmds
        private void ProcessTradeMessage(string message)
        {
            if (message.Equals(HelpCmd))
            {
                PrintHelpMessage();
                return;
            }

            if (message.StartsWith(AddCmd))
            {
                HandleAddCommand(message);
                Trade.SendMessage("done adding.");
            }
            else if (message.StartsWith(RemoveCmd))
            {
                HandleRemoveCommand(message);
                Trade.SendMessage("done removing.");
            }
        }

        private void PrintHelpMessage()
        {
            Trade.SendMessage(String.Format("{0} {1} [amount] [series] - adds all crates (optionally by series number, use 0 for amount to add all)", AddCmd, AddCratesSubCmd));
            Trade.SendMessage(String.Format("{0} {1} [amount] - adds metal", AddCmd, AddMetalSubCmd));
            Trade.SendMessage(String.Format("{0} {1} [amount] - adds weapons", AddCmd, AddWepsSubCmd));
            Trade.SendMessage(String.Format("{0} {1} [amount] - adds items", AddCmd, AddAllSubCmd));
            Trade.SendMessage(String.Format(@"{0} <craft_material_type> [amount] - adds all or a given amount of items of a given crafing type.", AddCmd));
            Trade.SendMessage(String.Format(@"{0} <defindex> [amount] - adds all or a given amount of items of a given defindex.", AddCmd));

            Trade.SendMessage(@"See http://wiki.teamfortress.com/wiki/WebAPI/GetSchema for info about craft_material_type or defindex.");
        }

        private void HandleAddCommand(string command)
        {
            var data = command.Split(' ');
            string typeToAdd;

            bool subCmdOk = GetSubCommand(data, out typeToAdd);

            if (!subCmdOk)
                return;

            uint amount = GetAddAmount(data);

            // if user supplies the defindex directly use it to add.
            int defindex;
            if (int.TryParse(typeToAdd, out defindex))
            {
                Trade.AddAllItemsByDefindex(defindex, amount);
                return;
            }

            switch (typeToAdd)
            {
                case AddMetalSubCmd:
                    AddItemsByCraftType("craft_bar", amount);
                    break;
                case AddWepsSubCmd:
                    AddItemsByCraftType("weapon", amount);
                    break;
                case AddCratesSubCmd:
                    // data[3] is the optional series number
                    if (!String.IsNullOrEmpty(data[3]))
                        AddCrateBySeries(data[3], amount);
                    else
                        AddItemsByCraftType("supply_crate", amount);
                    break;
                case AddAllSubCmd:
                    AddAllItems();
                    break;
                default:
                    AddItemsByCraftType(typeToAdd, amount);
                    break;
            }
        }



        private void HandleRemoveCommand(string command)
        {
            var data = command.Split(' ');

            string subCommand;

            bool subCmdOk = GetSubCommand(data, out subCommand);

            // were dumb right now... just remove everything.
            Trade.RemoveAllItems();

            if (!subCmdOk)
                return;
        }


        private void AddItemsByCraftType(string typeToAdd, uint amount)
        {
            var items = Trade.CurrentSchema.GetItemsByCraftingMaterial(typeToAdd);

            uint added = 0;

            foreach (var item in items)
            {
                added += Trade.AddAllItemsByDefindex(item.Defindex, amount);

                // if bulk adding something that has a lot of unique
                // defindex (weapons) we may over add so limit here also
                if (amount > 0 && added >= amount)
                    return;
            }
        }

        private void AddAllItems()
        {
            var items = Trade.CurrentSchema.GetItems();

            foreach (var item in items)
            {
                Trade.AddAllItemsByDefindex(item.Defindex, 0);
            }
        }

        private void AddCrateBySeries(string series, uint amount)
        {
            int ser;
            bool parsed = int.TryParse(series, out ser);

            if (!parsed)
                return;

            var l = Trade.CurrentSchema.GetItemsByCraftingMaterial("supply_crate");


            List<Inventory.Item> invItems = new List<Inventory.Item>();

            foreach (var schemaItem in l)
            {
                ushort defindex = schemaItem.Defindex;
                invItems.AddRange(Bot.MyInventory.GetItemsByDefindex(defindex));
            }

            uint added = 0;

            foreach (var item in invItems)
            {
                int crateNum = 0;
                for (int count = 0; count < item.Attributes.Length; count++)
                {
                    // FloatValue will give you the crate's series number
                    crateNum = (int)item.Attributes[count].FloatValue;

                    if (crateNum == ser)
                    {
                        bool ok = Trade.AddItem(item.Id);

                        if (ok)
                            added++;

                        // if bulk adding something that has a lot of unique
                        // defindex (weapons) we may over add so limit here also
                        if (amount > 0 && added >= amount)
                            return;
                    }
                }
            }
        }

        bool GetSubCommand(string[] data, out string subCommand)
        {
            if (data.Length < 2)
            {
                Trade.SendMessage("No parameter for cmd");
                subCommand = null;
                return false;
            }

            if (String.IsNullOrEmpty(data[1]))
            {
                Trade.SendMessage("No parameter for cmd");
                subCommand = null;
                return false;
            }

            subCommand = data[1];

            return true;
        }

        static uint GetAddAmount(string[] data)
        {
            uint amount = 0;

            if (data.Length > 2)
            {
                // get the optional ammount parameter
                if (!String.IsNullOrEmpty(data[2]))
                {
                    uint.TryParse(data[2], out amount);
                }
            }

            return amount;
        }
        #endregion
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