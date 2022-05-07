using System.Xml.Linq;

namespace SecondBot.Client {

    public class Program {

        public static int myAppCount = 0;

        static void Main(string[] args) {
            Console.WriteLine("2ndBot Start");

            try {
                XElement xml = XElement.Load(Constants.SETTING_XML_FILENAME);
                IEnumerable<XElement> bots = from item in xml.Elements("bot") select item;
                int botNums = bots.Count<XElement>();
                Console.WriteLine("Bots:" + botNums);
                foreach(XElement bot in bots) {
                    String? fistname = bot.Element("firstname")?.Value;
                    String? lastname = bot.Element("lastname")?.Value;
                    String? password = bot.Element("password")?.Value;
                    String? start = bot.Element("start")?.Value;
                    String? nickname = bot.Element("nickname")?.Value;
                    String? home = bot.Element("home")?.Value;
                    String? bed = bot.Element("bed")?.Value;
                    String? chatplus_apikey = bot.Element("chatplus_apikey")?.Value;
                    String? chatplus_agentname = bot.Element("chatplus_agentname")?.Value;
                    String? mebo_apikey = bot.Element("mebo_apikey")?.Value;
                    String? mebo_agent_id = bot.Element("mebo_agent_id")?.Value;
                    String? script = bot.Element("script")?.Value;
                    String? owner = bot.Element("owner")?.Value;
                    if (fistname == null || fistname.Length == 0
                        || lastname == null || lastname.Length == 0
                        || password == null || password.Length == 0
                        || nickname == null || nickname.Length == 0
                        || chatplus_apikey == null || chatplus_apikey.Length == 0
                        || chatplus_agentname == null || chatplus_agentname.Length == 0
                        || owner == null || owner.Length == 0
                        ) {
                        Console.WriteLine("Required fields are not set(fistname,lastname,password,nickname,chatplus_apikey,chatplus_agentname,owner in .settings.xml)");
                        Environment.Exit(0);
                        return;
                    }
                    if (start == null) start = "last";

                    Console.WriteLine(fistname + " " + lastname + " " + nickname +" ...starting");
                    new MyApplication(fistname, lastname, password, start, nickname, home, bed, chatplus_apikey, chatplus_agentname, mebo_apikey, mebo_agent_id, script,owner);
                    // アプリケーション数が0になったら終了する
                    myAppCount += 1;
                }
            } catch(Exception e) {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
            }

            // 一旦終了させない
            using (ManualResetEvent manualResetEvent = new ManualResetEvent(false))
            {
                manualResetEvent.WaitOne();
            }
        }
    }
}