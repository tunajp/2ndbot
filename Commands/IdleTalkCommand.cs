using OpenMetaverse;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;

namespace SecondBot.Client {
    public class ChatplusRequest {
        public string? utterance {get; set;}
        public string? username {get; set;}
        public ChatplusAgentState? agentState {get; set;}
    }
    public class ChatplusAgentState {
        public string? agentName {get; set;}
        public string? tone {get; set;}
        public string? age {get; set;}
    }

    public class ChatplusResponse {
        public string? utterance {get; set;}
        public ChatplusScoreResponse? bestResponse {get; set;}
        public IList<ChatplusScoreResponse>? responses { get; set; }
        public string[]? tokenized { get; set; }
        public string[]? options { get; set; }
    }
    public class ChatplusScoreResponse {
        public string? utterance {get; set;}
        public float score {get; set;}
        public string? url {get; set;}
    }

    public class MeboRequest {
        public string? api_key {get; set;}
        public string? agent_id {get; set;}
        public string? utterance {get; set;}
        public string? uid {get; set;}
    }

    public class MeboResponse {
        public string? utterance {get; set;}
        public MeboBestResponse? bestResponse {get; set;}
    }
    public class MeboBestResponse {
        public string? utterance {get; set;}
        public string[]? options { get; set; }
        public string? topic {get; set;}
        public bool? isAutoResponse {get; set;}

    }


    public class IdleTalkCommand : Command {

        ChatApi chatApi; // 0:chatplus 1:mebo(free plan:1000/month)
        private string chatplus_apikey;
        private string chatplus_agentname;
        private string? mebo_apikey;
        private string? mebo_agent_id;

        public IdleTalkCommand(MyClient mclient) {
            this.mclient = mclient;
            this.chatApi = ChatApi.chatplus;
            this.chatplus_apikey = "";
            this.chatplus_agentname = "";
        }
        public void setKeys(ChatApi chatApi, string chatplus_apikey, string chatplus_agentname, string? mebo_apikey, string? mebo_agent_id) {
            this.chatApi = chatApi;
            this.chatplus_apikey = chatplus_apikey;
            this.chatplus_agentname = chatplus_agentname;
            this.mebo_apikey = mebo_apikey;
            this.mebo_agent_id = mebo_agent_id;
        }

        private bool IsJapanese(string text) {
            var isJapanese = Regex.IsMatch(text, @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]+");
            if (!isJapanese) {
                // 半角カタカナ
                if (Regex.IsMatch(text, @"[\uFF61-\uFF9F]")) return true;
                // 顔文字に使われるような記号
                if (Regex.IsMatch(text, @"[@+\(\)\<\>\'^]")) return true;
            }
            return isJapanese;
        }

        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
            // エラーメッセージっぽいのと日本語が含まれないのはチャットから除外
            if (message.Contains("User not online")) {
                return;
            }
            if (!this.IsJapanese(message) && message.Distinct().Count() != 1) { // wwは英語と判定させない
                string mes = @"Hello, my name is Tachikawa-kun, and I'm a BOT. I am currently unable to understand English. But I am sure that my dream of speaking in English will come true in the near future. I would appreciate it if you could wait for me until then.";
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                return;
            }

            Vector3? targetPos = MyApplication.getTargetPos(this.mclient, fromName);
            if (targetPos != null) {
                Vector3 newDirection;
                newDirection.X = (float)targetPos?.X;
                newDirection.Y = (float)targetPos?.Y;
                newDirection.Z = (float)targetPos?.Z;
                this.mclient.Self.Movement.TurnToward(newDirection);
                this.mclient.Self.Movement.SendUpdate(false);
            }

            if (this.chatApi == ChatApi.chatplus) this.chatplus(fromUUID, fromName, message, type);
            if (this.chatApi == ChatApi.mebo) this.mebo(fromUUID, fromName, message, type);
            MyApplication.lastChatDateTime = DateTime.Now;
        }

        async void chatplus(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            this.mclient.Self.AnimationStart(Animations.TYPE, false);

            // https://www.chaplus.jp/
            string URL = "https://www.chaplus.jp/v1/chat?apikey=" + this.chatplus_apikey;
            var jsonObject = new ChatplusRequest
            {
                utterance = message,
                username = fromName,
                agentState = new ChatplusAgentState
                {
                    agentName = this.chatplus_agentname,
                    tone = "normal",
                    age = "13歳"
                }
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(jsonObject, options);
            try {
                using (var client = new HttpClient()) {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(URL, content);
                    if(response.IsSuccessStatusCode) {
                        var _response = await response.Content.ReadAsStringAsync();

                        ChatplusResponse? chatplusResponse = System.Text.Json.JsonSerializer.Deserialize<ChatplusResponse>(_response);
                        if (type == 0) this.mclient.Self.Chat(chatplusResponse?.bestResponse?.utterance, 0, ChatType.Normal);
                        else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, chatplusResponse?.bestResponse?.utterance);
                    } else {
                        Console.WriteLine("error");
                        if (type == 0) this.mclient.Self.Chat("error", 0, ChatType.Normal);
                        else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, "error");
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                if (type == 0) this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, e.Message);
            } finally {
                this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
                this.mclient.Self.AnimationStop(Animations.TYPE, false);
            }
        }

        async void mebo(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            this.mclient.Self.AnimationStart(Animations.TYPE, false);

            // https://mebo.work/
            if (this.mebo_apikey == null || this.mebo_apikey.Length == 0) {
                if (type == 0) this.mclient.Self.Chat("mebo_apikey null", 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, "mebo_apikey null");
                return;
            }
            if (this.mebo_agent_id == null || this.mebo_agent_id.Length == 0) {
                if (type == 0) this.mclient.Self.Chat("mebo_agent_id null", 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, "mebo_agent_id null");
                return;
            }

            string URL = "https://api-mebo.dev/api";
            var jsonObject = new MeboRequest
            {
                api_key = this.mebo_apikey,
                agent_id = this.mebo_agent_id,
                utterance = message,
                uid = fromName
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(jsonObject, options);
            try {
                using (var client = new HttpClient()) {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(URL, content);
                    if(response.IsSuccessStatusCode) {
                        var _response = await response.Content.ReadAsStringAsync();

                        MeboResponse? meboResponse = System.Text.Json.JsonSerializer.Deserialize<MeboResponse>(_response);
                        if (type == 0) this.mclient.Self.Chat(meboResponse?.bestResponse?.utterance, 0, ChatType.Normal);
                        else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, meboResponse?.bestResponse?.utterance);
                    } else {
                        Console.WriteLine("error");
                        if (type == 0) this.mclient.Self.Chat("error", 0, ChatType.Normal);
                        else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, "error");
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                if (type == 0) this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, e.Message);
            } finally {
                this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
                this.mclient.Self.AnimationStop(Animations.TYPE, false);
            }

        }

        public async void rondomMessage() {
            this.mclient.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            this.mclient.Self.AnimationStart(Animations.TYPE, false);

            Random r = new System.Random();
            int nextAction = r.Next(0, 50); // 0-49
            // togetter 10
            // はてぶ 40
            if (nextAction < 10) {
                // togetter
                try {
                    using (var client = new HttpClient()) {
                        var URL = "https://togetter.com/rss/hot";
                        var response = await client.GetAsync(URL);
                        if(response.IsSuccessStatusCode) {
                            var _response = await response.Content.ReadAsStringAsync();
                            var document = XDocument.Parse(_response);
                            XElement? xml = document.Root;
                            XElement? channel = xml.Element("channel");
                            IEnumerable<XElement>? items = channel?.Elements("item");
                            var mes = items?.ElementAt(nextAction)?.Element("title")?.Value;
                            mes += " " + items?.ElementAt(nextAction)?.Element("link")?.Value;
                            this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                        } else {
                            this.mclient.Self.Chat("error", 0, ChatType.Normal);
                        }
                    }
                } catch (Exception e) {
                    this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
                }
            } else {
                // はてぶ
                try {
                    using (var client = new HttpClient()) {
                        var URL = "https://b.hatena.ne.jp/hotentry/general.rss";
                        XNamespace d = "http://purl.org/rss/1.0/";
                        XNamespace dc = "http://purl.org/dc/elements/1.1/";
                        var response = await client.GetAsync(URL);
                        if(response.IsSuccessStatusCode) {
                            var _response = await response.Content.ReadAsStringAsync();
                            var document = XDocument.Parse(_response);
                            XElement? xml = document.Root;
                            IEnumerable<XElement>? items = xml?.Descendants(d + "item");
                            var mes = items?.ElementAt(nextAction-10)?.Element(d + "title")?.Value;
                            mes += " " + items?.ElementAt(nextAction-10)?.Element(d + "link")?.Value;
                            this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                        } else {
                            this.mclient.Self.Chat("error", 0, ChatType.Normal);
                        }
                    }
                } catch (Exception e) {
                    this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
                }
            }

            this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
            this.mclient.Self.AnimationStop(Animations.TYPE, false);
            MyApplication.lastChatDateTime = DateTime.Now;
        }

    }
}