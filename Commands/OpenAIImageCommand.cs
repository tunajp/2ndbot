using OpenMetaverse;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using OpenMetaverse.Imaging;

namespace SecondBot.Client {

    public class OpenAIImageCommand : Command {
        private string? openai_apikey;

        public OpenAIImageCommand(MyClient mclient) {
            this.mclient = mclient;
        }
        public void setKeys(string? openai_apikey) {
            this.openai_apikey = openai_apikey;
        }

        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {

            this.mclient.Self.Chat(string.Empty, 0, ChatType.StartTyping);
            this.mclient.Self.AnimationStart(Animations.TYPE, false);

            // より詳細に「〇〇が〇〇で〇〇な写真」みたいに書くといい
            // https://labs.openai.com/
            // The maximum length is 1000 characters.
            // Japanese young woman swimsuit photo
            // Principal villainess in a office, Pixar movie still, official media, 4K HD, by Bill Presing
            // Japanese woman, still from Pixar movie, official media, 4K HD, by Bill Presing
            // photo of slim girl, 20yo, close-up, high detail, studio, smoke, sharp, pink, violet light, studio, 85mm sigma art lens
            // Two Japanese girls kissing photo, 20 years old, close up, high definition, studio, studio, 85mm sigma art lens
            // kawaii cute girl hatsune miku, Ranking number 1 in pixiv
            // https://dallery.gallery/the-dalle-2-prompt-book/
            // https://blog.ch3cooh.jp/entry/2022/08/06/124100
            Regex re = new Regex(@"[^-+*a-zA-Z0-9 ]"); // 「英数字と-、+、*」以外
            string prompt =  re.Replace(message, "");
            if (prompt.Length <= 0) {
                this.mclient.Say(fromUUID, "どんな絵を書いていいかわかりません！参考書をどうぞ！", 0, type);
                this.mclient.Say(fromUUID, "https://dallery.gallery/the-dalle-2-prompt-book/", 0, type);
                return;
            }
            try {
                this.dalle2(prompt, fromUUID, fromName, message, type).Wait();
            } catch (Exception e) {
                this.mclient.Say(fromUUID, e.Message, 0, type);
            }

            this.mclient.Say(fromUUID, "(`･ー･´)ドヤ！", 0, type);

            this.mclient.Self.Chat(string.Empty, 0, ChatType.StopTyping);
            this.mclient.Self.AnimationStop(Animations.TYPE, false);
        }

        private async Task dalle2(string prompt, UUID fromUUID, string fromName, string message ,int type) {
           if (this.openai_apikey == null || this.openai_apikey.Length == 0) {
                this.mclient.Say(fromUUID, "openai_apikey null", 0, type);
                return;
            }


            this.mclient.Say(fromUUID, "立川画伯におまかせあれ！！！", 0, type);

            var openAiService = new OpenAI.Managers.OpenAIService(new OpenAI.OpenAiOptions(){
                ApiKey = this.openai_apikey
            });
            var imageResult = await openAiService.CreateImage(
                new OpenAI.ObjectModels.RequestModels.ImageCreateRequest() {
                    Prompt = prompt,
                    N =2, // 画像の枚数
                    Size = OpenAI.ObjectModels.StaticValues.ImageStatics.Size.Size1024,
                    ResponseFormat = OpenAI.ObjectModels.StaticValues.ImageStatics.ResponseFormat.Url,
                    User = fromName // 任意
                });
            if (imageResult.Successful) {
                // URL は 1 時間後に期限切れになります
                //Console.WriteLine(string.Join("\n",imageResult.Results.Select(r => r.Url)));
                var urls = imageResult.Results.Select(r => r.Url);
                this.mkdir(Constants.OPENAIIMAGEDIR);
                int count = 1;
                foreach(string url in urls) {
                    DateTime dt = DateTime.Now;
                    string file = Constants.OPENAIIMAGEDIR + Path.DirectorySeparatorChar + dt.ToString("yyyyMMddHHmmssfff") + ".png";
                    //string inventoryName = Constants.OPENAIIMAGEDIR + Path.DirectorySeparatorChar + dt.ToString("yyyyMMddHHmmssfff");
                    this.download(url, file).Wait();
                    Console.WriteLine(file);
                    //uint timeout = 100000;
                    // TODO:　https://learn.microsoft.com/ja-jp/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only
                    this.mclient.Say(fromUUID, count.ToString() + "枚目！", 0, type);
                    this.mclient.Say(fromUUID, url, 0, type);
                    count++;
                }
            } else {
                if (imageResult.Error == null) {
                    throw new Exception("Unknown Error");
                }
                Console.WriteLine($"{imageResult.Error.Code}: {imageResult.Error.Message}");
            }

        }

        private async Task download(string url, string filename) {
            try {
                using (var client = new HttpClient()) {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) {
                        using (var content = response.Content)
                        using (var stream = await content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write,FileShare.None))
                        {
                            stream.CopyTo(fileStream);
                        }
                    } else {
                        Console.WriteLine("Error");
                    }
                }
            } catch (Exception e) {
                System.Console.WriteLine(e.Message);
            }
        }

        void mkdir(string dir) {
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }


    }
}