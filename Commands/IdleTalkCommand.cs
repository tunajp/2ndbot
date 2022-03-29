using OpenMetaverse;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SecondBot.Client {
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

            if (this.chatApi == ChatApi.chatplus) this.chatplus(fromUUID, fromName, message, type);
            if (this.chatApi == ChatApi.mebo) this.mebo(fromUUID, fromName, message, type);
        }

        async void chatplus(UUID fromUUID, string fromName, string message ,int type) {
            string URL = "https://www.chaplus.jp/v1/chat?apikey=" + this.chatplus_apikey;
            var json = "{ \"utterance\" : \""+ message + "\", \"username\" : \"" + fromName + "\", \"agentstate\":{ \"agentName\": \"" + this.chatplus_agentname + "\", \"tone\": \"normal\", \"age\": \"13歳\"} }";
            try {
                using (var client = new HttpClient()) {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(URL, content);
                    if(response.IsSuccessStatusCode) {
                        var _response = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine(_response);
                        Newtonsoft.Json.Linq.JObject jsonObj = Newtonsoft.Json.Linq.JObject.Parse(_response);
                        // FIX ME:一旦むりやりなパースで
                        var a = jsonObj["bestResponse"];
                        if (a != null) {
                            foreach(var i in a) {
                                string str = i.ToString();
                                if (str != null) {
                                    if (str.Contains("utterance")) {
                                        string s = str.Substring(14, str.Length -15);
                                        Console.WriteLine("req:" + message);
                                        Console.WriteLine("res:" + s);
                                        if (type == 0) this.mclient.Self.Chat(s, 0, ChatType.Normal);
                                        else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, s);
                                        break;
                                    }
                                }
                            }
                        }
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
            }
        }

        async void mebo(UUID fromUUID, string fromName, string message ,int type) {

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
            var json = "{ \"api_key\" : \"" + this.mebo_apikey + "\", \"agent_id\": \"" + this.mebo_agent_id + "\", \"utterance\" : \""+ message + "\", \"uid\" : \"" + fromName + "\"}";
            try {
                using (var client = new HttpClient()) {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(URL, content);
                    if(response.IsSuccessStatusCode) {
                        var _response = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine(_response);
                        Newtonsoft.Json.Linq.JObject jsonObj = Newtonsoft.Json.Linq.JObject.Parse(_response);
                        // FIX ME:一旦むりやりなパースで
                        var a = jsonObj["bestResponse"];
                        if (a != null) {
                            foreach(var i in a) {
                                string str = i.ToString();
                                if (str != null) {
                                    if (str.Contains("utterance")) {
                                        string s = str.Substring(14, str.Length -15);
                                        Console.WriteLine("req:" + message);
                                        Console.WriteLine("res:" + s);
                                        if (type == 0) this.mclient.Self.Chat(s, 0, ChatType.Normal);
                                        else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, s);
                                        break;
                                    }
                                }
                            }
                        }
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
            }

        }

    }
}