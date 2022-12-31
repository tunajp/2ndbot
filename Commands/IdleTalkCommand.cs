using OpenMetaverse;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;
using NMeCab.Specialized;

namespace SecondBot.Client {


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

        ChatApi chatApi; // 0:mebo(free plan:1000/month), 1:OpenAI
        private string? mebo_apikey;
        private string? mebo_agent_id;

        private string? openai_apikey;
        private string openai_prompt;
        private Dictionary<UUID, Queue<List<string>>> openai_dic = new Dictionary<UUID, Queue<List<string>>>();

        public IdleTalkCommand(MyClient mclient) {
            this.mclient = mclient;
            this.chatApi = ChatApi.mebo;
        }
        public void setKeys(ChatApi chatApi,string? mebo_apikey, string? mebo_agent_id, string? openai_apikey, string openai_prompt) {
            this.chatApi = chatApi;
            this.mebo_apikey = mebo_apikey;
            this.mebo_agent_id = mebo_agent_id;
            this.openai_apikey = openai_apikey;
            this.openai_prompt = openai_prompt;
        }

        public bool IsJapanese(string text) {
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
            if (this.chatApi != ChatApi.openai) {
                if (!this.IsJapanese(message) && message.Distinct().Count() != 1) { // wwは英語と判定させない
                    string mes = @"Hello, my name is Tachikawa-kun, and I'm a BOT. I am currently unable to understand English. But I am sure that my dream of speaking in English will come true in the near future. I would appreciate it if you could wait for me until then.";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
            }

            Vector3? targetPos = MyApplication.getTargetPos(this.mclient, fromName);
            if (targetPos != null) {
                Vector3 newDirection;
                newDirection.X = (float)targetPos?.X!;
                newDirection.Y = (float)targetPos?.Y!;
                newDirection.Z = (float)targetPos?.Z!;
                this.mclient.Self.Movement.TurnToward(newDirection);
                this.mclient.Self.Movement.SendUpdate(false);
            }

            if (this.chatApi == ChatApi.mebo) this.mebo(fromUUID, fromName, message, type);
            if (this.chatApi == ChatApi.openai) this.openai(fromUUID, fromName, message, type);
            MyApplication.lastChatDateTime = DateTime.Now;
        }


        async void mebo(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            this.mclient.Self.AnimationStart(Animations.TYPE, false);

            // https://mebo.work/
            if (this.mebo_apikey == null || this.mebo_apikey.Length == 0) {
                this.mclient.Say(fromUUID, "mebo_apikey null", 0, type);
                return;
            }
            if (this.mebo_agent_id == null || this.mebo_agent_id.Length == 0) {
                this.mclient.Say(fromUUID, "mebo_agent_id null", 0, type);
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
                        this.mclient.Say(fromUUID, meboResponse?.bestResponse?.utterance, 0, type);
                    } else {
                        Console.WriteLine("error");
                        this.mclient.Say(fromUUID, "error", 0, type);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                this.mclient.Say(fromUUID, e.Message, 0, type);
            } finally {
                this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
                this.mclient.Self.AnimationStop(Animations.TYPE, false);
            }

        }
        async void openai(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            this.mclient.Self.AnimationStart(Animations.TYPE, false);

            if (this.openai_apikey == null || this.openai_apikey.Length == 0) {
                this.mclient.Say(fromUUID, "openai_apikey null", 0, type);

                this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
                this.mclient.Self.AnimationStop(Animations.TYPE, false);
                return;
            }

            try {
                if (message.Contains("関係を解消")) {
                    if (openai_dic.ContainsKey(fromUUID)) {
                        Queue<List<string>> q = openai_dic[fromUUID];
                        while (q.Count > 0)
                        {
                            List<string>? outObj;
                            q.TryDequeue(out outObj); // 先頭を取り出す
                        }
                    }
                    throw new Exception("queueを空にしました");
                }
                var openAiService = new OpenAI.GPT3.Managers.OpenAIService(new OpenAI.GPT3.OpenAiOptions(){
                    ApiKey = this.openai_apikey
                });

                string prompt = this.openai_prompt;
                foreach (var item in this.openai_dic) {
                    if (item.Key == fromUUID) {
                        foreach(var item2 in item.Value) {
                            // 同じ回答があった場合は、その回答に集約されてしまうため飛ばす
                            if (prompt.Contains(item2[1])) {
                                Console.WriteLine("skipped:"+item2[0]);
                                Console.WriteLine("skipped:"+item2[1]);
                                continue;
                            }

                            // ボクは、ボクは・・・みたいに同じ単語を繰り返してる場合は、さらに繰り返しが増えてしまうため飛ばす
                            MeCabIpaDicTagger tagger;
                            tagger = MeCabIpaDicTagger.Create();
                            var nodes = tagger.Parse(item2[1]);
                            List<string> string_nodes = new List<string>();
                            foreach (var node in nodes) {
                                if (node.PartsOfSpeech == "助詞") continue;
                                if (node.Surface == "、" || node.Surface == "。") continue;
                                string_nodes.Add(node.Surface);
                            }
                            //Console.WriteLine("nodes:" + String.Join(", ", string_nodes));
                            var duplicates = string_nodes.GroupBy(x => x)
                                .Where(g => g.Count() > 1)
                                .Select(y => y.Key)
                                .ToList();
                            //Console.WriteLine("duplicates:" + String.Join(", ", duplicates));
                            if (duplicates.Count > 0) {
                                Console.WriteLine("skipped:"+item2[0]);
                                Console.WriteLine("skipped:"+item2[1]);
                                continue;
                            }

                            prompt += "私:"+item2[0] + "\n";
                            prompt += "AI:"+item2[1] + "\n";
                        }
                    }
                }

                prompt += "私:" + message + "\n";
                prompt += "AI:";
                Console.WriteLine(prompt);

                var completionResult = await openAiService.Completions.Create(new OpenAI.GPT3.ObjectModels.RequestModels.CompletionCreateRequest() {
                    Prompt = prompt,
                    MaxTokens = 120,
                    Echo = false,
                    Temperature = 0.7f,
                    Stop = "\n",
                }, OpenAI.GPT3.ObjectModels.Models.Model.TextDavinciV3);
                if (completionResult.Successful) {
                    Console.WriteLine();
                    string answer = completionResult.Choices.FirstOrDefault().Text;
                    this.mclient.Say(fromUUID, answer, 0, type);

                    if (openai_dic.ContainsKey(fromUUID)) {
                        Queue<List<string>> q = openai_dic[fromUUID];
                        List<string> mes2 = new List<string>();
                        mes2.Add(message);
                        mes2.Add(answer);
                        while (q.Count > 5)
                        {
                            List<string>? outObj;
                            q.TryDequeue(out outObj); // 先頭を取り出す
                        }
                        q.Enqueue(mes2);
                    } else {
                        // Add
                        Queue<List<string>> q2 = new Queue<List<string>>();
                        List<string> mes3 = new List<string>();
                        mes3.Add(message);
                        mes3.Add(answer);
                        q2.Enqueue(mes3);

                        openai_dic.Add(fromUUID, q2);
                    }

                } else {
                    if (completionResult.Error == null) {
                        throw new Exception("Unknown Error");
                    }
                    throw new Exception($"{completionResult.Error.Code}: {completionResult.Error.Message}");
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                this.mclient.Say(fromUUID, e.Message, 0, type);
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
                            if (xml == null) return;
                            XElement? channel = xml.Element("channel");
                            IEnumerable<XElement>? items = channel?.Elements("item");
                            var mes = items?.ElementAt(nextAction)?.Element("title")?.Value;
                            if (this.mclient.galMojiMode) mes = this.mclient.GetGalMoji(mes);
                            mes += " " + items?.ElementAt(nextAction)?.Element("link")?.Value;
                            this.mclient.Say(UUID.Zero, mes, 0, 0, false);
                        } else {
                            this.mclient.Say(UUID.Zero, "error", 0, 0);
                        }
                    }
                } catch (Exception e) {
                    this.mclient.Say(UUID.Zero, e.Message, 0, 0);
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
                            if (this.mclient.galMojiMode) mes = this.mclient.GetGalMoji(mes);
                            mes += " " + items?.ElementAt(nextAction-10)?.Element(d + "link")?.Value;
                            this.mclient.Say(UUID.Zero, mes, 0, 0, false);
                        } else {
                            this.mclient.Say(UUID.Zero, "error", 0, 0);
                        }
                    }
                } catch (Exception e) {
                    this.mclient.Say(UUID.Zero, e.Message, 0, 0);
                }
            }

            this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
            this.mclient.Self.AnimationStop(Animations.TYPE, false);
            MyApplication.lastChatDateTime = DateTime.Now;
        }

    }
}