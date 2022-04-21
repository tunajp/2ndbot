using System.Xml.Linq;

namespace SecondBot.Client {
    static public class SecondLifeFeedCommand {
        public static async Task<String> feed() {
            try {
                using (var client = new HttpClient()) {
                    var URL = "https://api.secondlife.com/datafeeds/secondlife.xml";
                    var response = await client.GetAsync(URL);
                    if(response.IsSuccessStatusCode) {
                        var _response = await response.Content.ReadAsStringAsync();
                        var document = XDocument.Parse(_response);
                        XElement? xml = document.Root;
                        Console.WriteLine(xml?.Element("status")?.Value);
                        Console.WriteLine(xml?.Element("signups")?.Value);
                        Console.WriteLine(xml?.Element("inworld")?.Value);
                        return "SecondLifeについて、サーバーは" + xml?.Element("status")?.Value + "、" + "アカウント作成数は" + xml?.Element("signups")?.Value + "、" + "現在ログイン中の廃人は" + xml?.Element("inworld")?.Value + "人います。";
                    } else {
                        Console.WriteLine("error");
                        return "error";
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return e.Message;
            }
            return "";
        }
    }
}