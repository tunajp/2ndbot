using System.Runtime.Loader;
using System.Reflection;

using NMeCab.Specialized;
using System.Linq;

using OpenMetaverse;
using OpenMetaverse.Packets;

namespace SecondBot.Client {
    enum ChatMode {
        Nominate = 0,
        All,
    }
    public enum ChatApi {
        chatplus = 0,
        mebo,
        openai,
    }
    public class MyApplication {
        MyClient mclient;
        LoginParams ClientLogin;

        List<UUID> currentAnims;
        private System.Timers.Timer updateTimer;
        private string? followTarget;

        private bool randomChat;

        private bool loiter;
        private bool dancing;
        private DateTime lastLoiterDateTime;
        private Vector3 loiterStartRegionPos;
        static public DateTime lastChatDateTime;
        private List<Primitive>? MovementTargetPrims;

        string? activeGroup;

        List<string> nicknames;
        string? home;
        string? bed;
        string chatplus_apikey;
        string chatplus_agentname;
        string? mebo_apikey;
        string? mebo_agent_id;
        string? openai_apikey;
        string? script;
        string owner;

        MeCabIpaDicTagger tagger;

        private Dictionary<string, List<string>> commandDic = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> danceDic = new Dictionary<string, List<string>>();

        ChatMode chatMode; // 0:指名モード 1:全レス
        ChatApi chatApi; // 0:chatplus 1:mebo(free plan:1000/month) 2:openai
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);

        private ManualResetEvent ItemEvent = new ManualResetEvent(false);

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        private Logger logger;

        private IdleTalkCommand idletalkcommand;
        private StandupCommand standupcommand;
        private TeleportCommand teleportcommand;
        private MoveCommand movecommand;
        private AnimationCommand animationcommand;
        private CreateNotecardCommand createnotecardcommand;
        private InventoryListCommand inventorylistcommand;

        private Microsoft.Scripting.Hosting.ScriptEngine scriptEngine;
        private Microsoft.Scripting.Hosting.ScriptScope scriptScope;

        public MyApplication(string firstname, string lastname, string pass, string start, List<string> nicknames, string? home,string? bed, string chatplus_apikey, string chatplus_agentname, string? mebo_apikey, string? mebo_agent_id, string? openai_apikey, string? script, string owner) {

            AssemblyLoadContext.Default.Unloading += MethodInvokedOnSigTerm;

            this.logger = new Logger(firstname + "_" + lastname);

            this.chatMode = ChatMode.Nominate;
            this.chatApi = ChatApi.chatplus;
            this.currentAnims = new List<UUID>();
            this.followTarget = null;
            this.randomChat = true;
            this.loiter = false;
            this.dancing = false;
            this.lastLoiterDateTime = DateTime.Now;
            MyApplication.lastChatDateTime = DateTime.Now;

            this.nicknames = nicknames;
            this.home = home;
            this.bed = bed;
            this.chatplus_apikey = chatplus_apikey;
            this.chatplus_agentname = chatplus_agentname;
            this.mebo_apikey = mebo_apikey;
            this.mebo_agent_id = mebo_agent_id;
            this.openai_apikey = openai_apikey;
            this.script = script;
            this.owner = owner;

            this.tagger = MeCabIpaDicTagger.Create();

            commandDic.Add("helpCommand", new List<string>(){"コマンド", "ヘルプ", "一覧", "commands"});
            commandDic.Add("manualCommand", new List<string>(){"マニュアル", "manual"});
            commandDic.Add("galmojiConvCommand", new List<string>(){"ギャル文字変換", "galmojiconv"});
            commandDic.Add("galmojiCommand", new List<string>(){"ギャル文字", "galmoji"});
            commandDic.Add("sitCommand", new List<string>(){"座", "sit"});
            commandDic.Add("touchCommand", new List<string>(){"タッチ", "touch"});
            commandDic.Add("restCommand", new List<string>(){"休", "rest"});
            commandDic.Add("standCommand", new List<string>(){"立ち", "立って", "stand"});
            commandDic.Add("followCommand", new List<string>(){"おいで", "来", "follow me"});
            commandDic.Add("stopCommand", new List<string>(){"止", "stop"});
            commandDic.Add("teleportCommand", new List<string>(){"テレポ", "teleport"});
            commandDic.Add("exitCommand", new List<string>(){"終了", "exit"});
            commandDic.Add("groupCommand", new List<string>(){"グループ", "group"});
            commandDic.Add("gotoHomeCommand", new List<string>(){"帰", "戻", "goto home", "retrun home"});
            //commandDic.Add("setHomeCommand", new List<string>(){"set home"});
            commandDic.Add("chatModeCommand", new List<string>(){"チャットモード", "chatmode", "chat mode"});
            commandDic.Add("chatApiCommand", new List<string>(){"チャットAPI", "chatapi", "chat api"});
            commandDic.Add("whereCommand", new List<string>(){"どこ", "where"});
            commandDic.Add("channelChatCommand", new List<string>(){"チャンネルチャット", "channnel chat"});
            commandDic.Add("buttonClickCommand", new List<string>(){"ボタンクリック", "button click"});
            commandDic.Add("forwardCommand", new List<string>(){"前へ", "to forward"});
            commandDic.Add("backCommand", new List<string>(){"後ろへ", "to back"});
            commandDic.Add("rightCommand", new List<string>(){"右へ", "to right"});
            commandDic.Add("leftCommand", new List<string>(){"左へ", "to left"});
            commandDic.Add("loiterCommand", new List<string>(){"うろうろ", "loiter"});
            commandDic.Add("randomCommand", new List<string>(){"ランダム発言", "random message"});
            commandDic.Add("animListCommand", new List<string>(){"アニメリスト", "anim list"});
            commandDic.Add("animCommand", new List<string>(){"アニメ", "anim"});
            commandDic.Add("danceCommand", new List<string>(){"踊", "dance"});
            commandDic.Add("secondLifeCommand", new List<string>(){"セカンドライフ", "Second Life", "SecondLife"});
            commandDic.Add("itemListCommand", new List<string>(){"アイテムリスト", "item list"});
            commandDic.Add("attachCommand", new List<string>(){"attach"});
            commandDic.Add("detachCommand", new List<string>(){"detach"});
            commandDic.Add("inventoryCommand", new List<string>(){"インベントリ表示", "inventory"});
            commandDic.Add("appearanceCommand", new List<string>(){"appearance"});
            commandDic.Add("shootCommand", new List<string>(){"shoot"});
            commandDic.Add("debugCommand", new List<string>(){"デバッグ", "debug"});

            // https://maps.secondlife.com/secondlife/Primary/151/69/22
            danceDic.Add("SPiCa", new List<string>(){"LPB1 SPiCa (bento) v5.3", "spica", "240000"});
            danceDic.Add("私の時間", new List<string>(){"LPB2 Watashi no Jikan (bento) v5.3", "mytime", "150000"});
            danceDic.Add("メランコリック", new List<string>(){"LPB3 Melancholic (bento) v5.3", "melan", "250000"});
            danceDic.Add("だってだってだって", new List<string>(){"LPB4 Datte Datte Datte (bento) v5.3", "datte", "250000"});
            danceDic.Add("PiNK CAT", new List<string>(){"LPB5 Pink Cat (bento) v5.3m", "pinkcat", "270000"});
            danceDic.Add("好き！雪！本気マジック", new List<string>(){"LPB6 Suki Yuki Honki Masic (bento) v5.3", "sukiyuki", "220000"});
            danceDic.Add("ドーナツホール", new List<string>(){"LPB7 Donut Hole (bento) v5.3c", "donut", "230000"});
            danceDic.Add("Wavefile Full", new List<string>(){"LPB8 Wavefile Full (bento) v5.3", "wave", "270000"});
            danceDic.Add("金曜日のおはよう", new List<string>(){"LPB9 Kinyou No Ohayo (bento) v5.3", "kinyou", "270000"});
            danceDic.Add("Melody Line", new List<string>(){"LPB10 Melody Line (bento) v5.3", "melody", "230000"});
            danceDic.Add("ハジメテノオト", new List<string>(){"LPB11 Hajimete no Oto (bento) v0.6b", "hajimete", "200000"});
            danceDic.Add("シューティングスター", new List<string>(){"LPB12 Shooting Star (bento) v1.2", "shooting", "240000"});
            danceDic.Add("リトライ ランデヴー", new List<string>(){"LPB13 Retry Rendezvous (bento) v5.6", "retry", "240000"});
            danceDic.Add("リトルパレード", new List<string>(){"LPB14 Little Parade (bento) v5.6", "little", "150000"});
            danceDic.Add("君がそばにいるように", new List<string>(){"LPB15 Kimiga Sobani (bento) v5.6", "kimiga", "270000"});
            //danceDic.Add("", new List<string>(){"LPB16 Platinam (bento) v5.6", "platina", ""});
            danceDic.Add("drop pop candy", new List<string>(){"LPB17 Drop Pop Candy (bento) v5.6", "drop", "240000"});
            danceDic.Add("うそつき", new List<string>(){"LPB18 Usotsuki (bento) v5.7", "uso", "228000"});
            danceDic.Add("ピチカートドロップス", new List<string>(){"LPB19 Pizzicato Drops (bento) v5.7", "pizz", "240000"});
            danceDic.Add("ヘイセイカタクリズム", new List<string>(){"LPB20 Heisei Cataclysm (bento) v5.7", "heisei", "210000"});
            danceDic.Add("未来景イノセンス", new List<string>(){"LPB21 Miraikei Innocence (bento) v5.7", "mirai", "288000"});
            danceDic.Add("ストロボナイツ", new List<string>(){"LPB22 Storobo Nights (bento) v5.7b", "storobo", "192000"});
            danceDic.Add("春に一番近い街", new List<string>(){"LPB23 Haruni Ichiban Chikai Machi (bento) v5.7", "haru", "300000"});
            danceDic.Add("サイバーサンダーサイダー", new List<string>(){"LPB24 Cyber Thunder Thunder (bento) v5.7b", "cyber", "210000"});
            danceDic.Add("モザイクロール", new List<string>(){"LPB25 Mosaic Roll (bento) v5.7b", "mosaic", "210000"});
            danceDic.Add("ハーゲンダッツ以下の殺風景", new List<string>(){"LPB26 Haagen-Dazs Ika no Sappuukei (bento) v5.7a", "haa", "252000"});
            danceDic.Add("Dear", new List<string>(){"LPB27 Dear (bento) v5.7b", "dear", "288000"});
            danceDic.Add("Hand in Hand", new List<string>(){"LPB28 Hand in Hand (bento) v5.7b", "hand", "330000"});
            danceDic.Add("言葉のいらない約束", new List<string>(){"LPB29 Kotoba no iranai Yakusoku (bento) v5.7b", "kotoba", "258000"});
            danceDic.Add("Last Night Good Night", new List<string>(){"LPB30 Last Night Good Night (bento) v1.7", "last", "402000"});
            danceDic.Add("ヤキモチの答え", new List<string>(){"LPB31 Yakimochi no Kotae (bento) v1.7", "yaki", "228000"});
            danceDic.Add("DAYBREAK FRONTLINE", new List<string>(){"LPB32 Daybreak Frontline_Left  (bento) v1.7", "day", "240000"}); //LPB32 Daybreak Frontline_Right  (bento) v1.7
            danceDic.Add("ろりこんでよかった～", new List<string>(){"LPB33 Lolicon de Yokatta~ Full (bento) v1.7", "loli", "228000"});
            danceDic.Add("ねこみみスイッチ", new List<string>(){"LPB34 Nekomimi Switch (bento) v1.7", "neko", "252000"});
            danceDic.Add("Jenga", new List<string>(){"LPB35 Jenga (bento) v1.7", "jenga", "312000"});
            danceDic.Add("月見夜ラビット", new List<string>(){"LPB36 Tsukimiyo Rabbit v1.7", "rabbit", "258000"});
            danceDic.Add("唯我独尊ONLY ONE", new List<string>(){"LPB37 Yuigadokuson (bento) v1.7", "yuiga", "240000"});
            danceDic.Add("告白ライバル宣言", new List<string>(){"LPB38 Kokuhaku Rival Sengen (bento) v1.7", "koku", "252000"});
            danceDic.Add("きょうもハレバレ", new List<string>(){"LPB39 Kyomo Harebare Left (bento) v1.7", "hare", "240000"}); // LPB39 Kyomo Harebare Right (bento) v1.7
            danceDic.Add("Angel Rhythm", new List<string>(){"LPB40 Angel Rhythm (bento) v1.7", "angel", "216000"});
            //danceDic.Add("", new List<string>(){"LPB41 PositiveThinking (bento) v1.7", "", ""});
            danceDic.Add("いーあるふぁんくらぶ", new List<string>(){"LPB42 Li Aru Fanclub Left (bento) v1.7", "fan", "270000"}); // LPB42 Li Aru Fanclub Right (bento) v1.7
            danceDic.Add("極楽浄土", new List<string>(){"LPB43 Gokuraku Jodo (bento) v1.7", "goku", "228000"});
            //danceDic.Add("Strawberry", new List<string>(){"LPB44 Strawberry (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB45 Sweet Magic (bento) v1.7", "", "228000"});
            //danceDic.Add("", new List<string>(){"LPB46 1/6 (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB47 Lamb (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB48 Uchouten Vivace (bento) v1.7b", "", ""});
            //danceDic.Add("", new List<string>(){"LPB49 Relagic fish (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB50 Masked bitcH (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB51 UmiYuri Kaiteitan (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB52 Tajuu Mirai no Quartet (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB53 Scissorhands (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB54 Love Letter (bento) v1.7", "", ""});
            //danceDic.Add("Classic", new List<string>(){"LPB55 Classic (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB56 Twinkle Days  -L (bento) v1.7", "", ""}); // LPB56 Twinkle Days  -R (bento) v1.7
            //danceDic.Add("", new List<string>(){"LPB57 Uso to Nuigurumi -L (bento) v1.7", "", ""}); // LPB57 Uso to Nuigurumi -R (bento) v1.7
            //danceDic.Add("", new List<string>(){"LPB58 Tsumi to Batsu (bento) v1.7", "", ""});
            //danceDic.Add("", new List<string>(){"LPB59 Junjou Fighter (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB60 Hi-Fi Raver (bento) v2.0", "", ""});
            //danceDic.Add("トキヲ・ファンカ", new List<string>(){"LPB61 Tokio Funka (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB62 Owari e Mukau Hajimari no Uta (bento) v2.0", ""}, "");
            //danceDic.Add("", new List<string>(){"LPB63 Fuwari Crayon (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB64 Syalala (bento) v2.0b", "", ""});
            //danceDic.Add("", new List<string>(){"LPB65 Dive to Blue (bento) -L v1.9", "", ""}); // LPB65 Dive to Blue (bento) -R v1.9
            //danceDic.Add("", new List<string>(){"LPB66 Mabel (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB67 Sadistic Love (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB68 Anti-Gravity (bento) v2.0", "", ""});
            danceDic.Add("恋愛デコレート", new List<string>(){"LPB69 Ren ai Decorate (bento) v2.0", "deco", "330000"});
            //danceDic.Add("", new List<string>(){"LPB70 Wakusei Loop (bento) v2.0", "", ""});
            //danceDic.Add("Mad Lovers", new List<string>(){"LPB71 Mad Lovers (bento) v2.0", "", ""}); // LPB71 Mad Lovers Chair ( Chair only )
            //danceDic.Add("", new List<string>(){"LPB72 Taiyoukei Disco (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB73 Robo Kiss -L (bento) v2.0", "", ""}); // LPB73 Robo Kiss -R (bento) v2.0
            //danceDic.Add("", new List<string>(){"LPB74 Shiroi Yukino Princess wa (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB75 Marine Bloomin' (bento) v1.9b", "", ""});
            //danceDic.Add("", new List<string>(){"LPB76 Musunde Hiraite Rasetsu to Mukuro (bento) v2.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB77 Gigantic O.T.N. (bento) v2.1", "", ""});
            //danceDic.Add("", new List<string>(){"LPB78 Koi no 2-4-11 (bento) v2.1", "", ""});
            //danceDic.Add("", new List<string>(){"LPB79 Twinkle World (bento) v2.1", "", ""});
            //danceDic.Add("", new List<string>(){"LPB80 Yume to Hazakura (bento) v2.1", "", ""});
            //danceDic.Add("", new List<string>(){"LPB81 Natsu ni Sarishi Kimi o Omof (bento) v2.1", "", ""});
            //danceDic.Add("", new List<string>(){"LPB82 Liar Dance -Center (bento) v2.1", "", ""}); // LPB82 Liar Dance -Right (bento) v2.1, LPB82 Liar Dance -Left (bento) v2.1
            //danceDic.Add("", new List<string>(){"LPB83 DoReMiFa Rondo -L (bento) v2.1", "", ""}); // LPB83 DoReMiFa Rondo -R (bento) v2.1
            //danceDic.Add("", new List<string>(){"LPB84 Girls (bento) v2.1", "", ""});
            //danceDic.Add("紅一葉", new List<string>(){"LPB85 Akahitoha (bento) v2.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB86 Give me a break Stop now (bento) v2.2", "", ""});
            //danceDic.Add("ポーカーフェイス", new List<string>(){"LPB87 Poker Face -R (bento) v2.2", "", ""}); // LPB87 Poker Face -L (bento) v2.2, LPB87 Poker Face -RR (bento) v2.2.LPB87 Poker Face -LL (bento) v2.2
            //danceDic.Add("", new List<string>(){"LPB88 Killer Lady (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB89 Onegai Darling (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB90 Wave (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB91 Heart a la mode (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB92 Calc Piano (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB93 Happy Halloween (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB94 Hybrid (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB95 Koizora Yohou (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB96 Tougen Renka (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB97 Ojyama Mushi  (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB98 Get Up & Move!! (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB99 MiraiDokei AM4:30 (bento) v3.0", "", ""});
            //danceDic.Add("ZIGG-ZAGG", new List<string>(){"LPB100 Zigg-Zagg (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB101 Shake It Off -L (bento) v3.0", "", ""}); // LPB101 Shake It Off -R (bento) v3.0
            //danceDic.Add("", new List<string>(){"LPB102 Pusse Cafe (bento) v3.0", "", ""}); // LPB102 Pusse Cafe (bento) v3.0
            //danceDic.Add("", new List<string>(){"LPB103 Love Letter (bento) v3.0", "", ""});
            //danceDic.Add("視力検査", new List<string>(){"LPB104 Shiryoku Kensa (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB105 39 (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB106 Beat It (bento) v3.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB107 Asu no Yozora Shoukaihan (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB108 Jinsei Reset Button (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB109 Love Me If You Can - Left (bento) v3.0", "", ""}); // LPB109 Love Me If You Can - Right (bento) v3.0
            //danceDic.Add("", new List<string>(){"LPB110 Madamamagoto (bento) v3.0", "", ""});
            //danceDic.Add("", new List<string>(){"LPB111 Suki Suki Zecchou Shou -Center (bento) v3.0", "", ""}); // LPB111 Suki Suki Zecchou Shou -Left (bento) v3.0, LPB111 Suki Suki Zecchou Shou -Right (bento) v3.0
            //danceDic.Add("", new List<string>(){"LPB112 Happy Happy Birthday (bento) v3.0", "", ""});
            //danceDic.Add("桃源恋歌", new List<string>(){"LPB113 Tougen Renka 3 -Center (bento) v5.0", "", ""}); // LPB113 Tougen Renka 3  -Right (bento) v5.0, LPB113 Tougen Renka 3 -Left (bento) v5.0
            //danceDic.Add("", new List<string>(){"LPB114 Gift (bento) v5.5e", "", ""});
            //danceDic.Add("", new List<string>(){"LPB115 DeepBlueTown he Oideyo (bento) v5.5e", "", ""});
            //danceDic.Add("", new List<string>(){"LPB116 DeepBlueSong (bento) v5.5e", "", ""});
            //danceDic.Add("", new List<string>(){"LPB117 [A]ddiction (bento) v5.5e", "", ""});
            //danceDic.Add("", new List<string>(){"LPB118 Marine Dreamin' (bento) v5.5e", "", ""});
            //danceDic.Add("", new List<string>(){"LPB119 Glide -L (bento) v5.7b", "", ""}); // LPB119 Glide -R (bento) v5.7b
            //danceDic.Add("", new List<string>(){"LPB120 Clap Hip Cherry (bento) v5.7a", "", ""});
            //danceDic.Add("", new List<string>(){"LPB121 Aimaisa Kaihi (bento) v7.2", "", ""});
            //danceDic.Add("恋はきっと急上昇☆", new List<string>(){"LPB122 Koi wa Kitto Kyuujoushou (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB123 Love Cino (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB124 Tetrodotoxin Sizer Nigou  -L (bento) v7.2", "", ""}); // LPB124 Tetrodotoxin Sizer Nigou  -R (bento) v7.2
            //danceDic.Add("", new List<string>(){"LPB125 Senbonzakura (bento) v7.2", "", ""}); // LPB125 Senbonzakura Fan (Fan only)
            //danceDic.Add("", new List<string>(){"LPB126 Gravity=Reality (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB127 Kusari no Shoujo (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB128 Dramaturgy (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB129 The Cure [Short Dance]  (bento) v7.2", "", ""});
            //danceDic.Add("脱獄", new List<string>(){"LPB130 Datsugoku (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB131 Round off the Square Earth (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB132 Let's try it (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB133 Orange  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB134 Roki  (bento) v7.2", "", ""});
            //danceDic.Add("シャルル", new List<string>(){"LPB135 Charles  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB136 Sugar hate  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB137 Kimi no kanojo  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB138 Sea Breeze  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB139 Meg meg fire endless night  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB140 Bye-Bye  (bento) v7.2", "", ""});
            //danceDic.Add("", new List<string>(){"LPB141 Love Mikujen'nu (bento) v7.2", "", ""});

            this.mclient = new MyClient();
            this.mclient.Settings.USE_LLSD_LOGIN = true; // LLSD or XML-RPC(古い形式)
            this.mclient.Settings.USE_ASSET_CACHE = true;

            this.mclient.Network.LoginProgress += Network_OnLogin;
            this.mclient.Network.SimConnected += Network_OnSimConnected;
            this.mclient.Network.Disconnected += Network_OnDisconnected;

            this.mclient.Self.ChatFromSimulator += Self_ChatFromSimulator;
            this.mclient.Self.IM += Self_IM;
            this.mclient.Self.AnimationsChanged += Self_AnimationsChanged;
            this.mclient.Self.ScriptDialog += Self_ScriptDialog;

            this.mclient.Inventory.InventoryObjectOffered += Inventory_InventoryObjectOfferd;
            this.mclient.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
            this.mclient.Friends.FriendshipOffered += Frind_FrienshipOfferd;
            this.mclient.Objects.ObjectProperties += Objects_OnObjectProperties;
            this.mclient.Groups.GroupInvitation += Group_GroupInvitation;

            // https://github.com/cinderblocks/libremetaverse/blob/master/LibreMetaverse.GUI/AvatarList.cs
            this.mclient.Grid.CoarseLocationUpdate += Grid_CoarseLocationUpdate;
            this.mclient.Network.SimChanged += Network_OnCurrentSimChanged;
            this.mclient.Objects.AvatarUpdate += Objects_OnNewAvatar;
            this.mclient.Objects.TerseObjectUpdate += Objects_OnObjectUpdated;

            string firstName = firstname;
            string lastName = lastname;
            string password = pass;

            this.ClientLogin = mclient.Network.DefaultLoginParams(firstName, lastName, password, "2ndbot", "1.0");
            this.ClientLogin.Start = start;

            this.mclient.Network.BeginLogin(this.ClientLogin);

            updateTimer = new System.Timers.Timer(1000);
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
            updateTimer.Start();

            this.idletalkcommand = new IdleTalkCommand(this.mclient);
            this.standupcommand = new StandupCommand(this.mclient);
            this.teleportcommand = new TeleportCommand(this.mclient);
            this.movecommand = new MoveCommand(this.mclient);
            this.animationcommand = new AnimationCommand(this.mclient);
            this.createnotecardcommand = new CreateNotecardCommand(this.mclient);
            this.inventorylistcommand = new InventoryListCommand(this.mclient);

            this.scriptEngine = IronPython.Hosting.Python.CreateEngine();
            this.scriptScope = scriptEngine.CreateScope();
            this.scriptScope.SetVariable("application", this);
            this.scriptScope.SetVariable("mclient", this.mclient);
            this.scriptScope.SetVariable("idletalkcommand", this.idletalkcommand);
            this.scriptScope.SetVariable("standupcommand", this.standupcommand);
            this.scriptScope.SetVariable("teleportcommand", this.teleportcommand);
            this.scriptScope.SetVariable("movecommand", this.movecommand);
            this.scriptScope.SetVariable("animationcommand", this.animationcommand);
            this.scriptScope.SetVariable("createnotecardcommand", this.createnotecardcommand);
            try {
               this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                dynamic scriptInfo = this.scriptScope.GetVariable(@"scriptInfo");
                var info = scriptInfo();
                Console.WriteLine(info);
            } catch(Exception e) {
               Console.WriteLine(e.Message);
            }
        }

        public void exitApp() {
            this.loiter = false;
            this.stopDance();
            this.mclient.Network.Logout();
        }

        void MethodInvokedOnSigTerm(AssemblyLoadContext sender) {
            Console.WriteLine("SIGHUP");
        }
        async void Network_OnLogin(object? sender, LoginProgressEventArgs e) {
            String mes = await SecondLifeFeedCommand.feed();
            if (e.Status == LoginStatus.Success) {
                Console.WriteLine("Login success");
                this.loiterStartRegionPos = this.mclient.Self.SimPosition;
                this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic Network_OnLogin = this.scriptScope.GetVariable(@"Network_OnLogin");
                    var info = Network_OnLogin();
                    Console.WriteLine(info);
                } catch(Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (e.Status == LoginStatus.Failed) {
                Console.WriteLine("Login Failed");
                Program.myAppCount -= 1;
                this.tagger.Dispose();
                if (Program.myAppCount == 0) {
                    Console.WriteLine(mes);
                    Environment.Exit(0);
                }
                return;
            }
        }
        void Network_OnSimConnected(object? sender, SimConnectedEventArgs e) {
            Console.WriteLine("Connected");
            this.mclient.Self.RequestBalance();
            //this.mclient.Appearance.SetPreviousAppearance();
            //this.mclient.Groups.ActivateTitle()
        }
        void Network_OnDisconnected(object? sender, DisconnectedEventArgs e) {
            Console.WriteLine("Disconnected");
            Program.myAppCount -= 1;
            this.tagger.Dispose();
            Console.WriteLine("残り:" + Program.myAppCount);
            if (Program.myAppCount == 0) {
                Environment.Exit(0);
            }
        }

        void Self_ChatFromSimulator(object? sender, ChatEventArgs e) {
            if (e.Message.Length > 0) this.logger.ChatLog(e.FromName + ":" + e.Message);
            //if (e.Message.Length > 0 && (this.mclient.MasterKey == e.SourceID || (this.mclient.MasterName == e.FromName && !this.mclient.AllowObjectMaster))) {
            //    this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
            //}
            Console.WriteLine("発言者:" + e.SourceID + " ," + this.nicknames[0] + ":" + this.mclient.Self.AgentID);
            if (e.Message.Length > 0 && e.SourceID != this.mclient.Self.AgentID) {
                bool isJapanese = idletalkcommand.IsJapanese(e.Message);
                if (!isJapanese && e.SourceType != ChatSourceType.Agent) {
                    Console.WriteLine("Ignore this as it is not a chat from agent.");
                    return;
                }
                if (e.FromName.Contains("LPB")) {
                    Console.WriteLine("Ignore this as it is LPB Dance System.");
                    return;
                }
                string message = e.Message;
                message = message.Trim();

                Console.WriteLine(message);

                bool continueCommand = true;
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic chatBeforHook = this.scriptScope.GetVariable(@"chatBeforHook");
                    continueCommand = chatBeforHook(e.SourceID, e.FromName, message, 0);
                } catch(Exception ex) {
                    Console.WriteLine(ex.Message);
                }
                if (!continueCommand) {
                    return;
                }

                bool found = false;
                if (isJapanese) {
                    var nodes = tagger.Parse(message);
                    var arr = nodes.Where(n => this.nicknames.Contains(n.Surface.ToLower()));
                    if (arr.Count() > 0) {
                        //Console.WriteLine("呼ばれた");
                        found = true;
                    }

                } else {
                    string[] arr =  message.Split(' ');
                    var arr1 = arr.Where(a => this.nicknames.Contains(a.ToLower()));
                    if (arr1.Count() > 0) {
                        //Console.WriteLine("呼ばれた");
                        found = true;
                    }
                }
                //foreach (var nickname in this.nicknames) {
                //    if (message.Trim().ToLower().Contains(nickname)) {
                //        found = true;
                //        break;
                //    }
                //}
                if (found) {
                    message = message.Replace("　", " ");
                    this.command(e.SourceID, e.FromName, message, 0);
                } else {
                    if (this.chatMode == ChatMode.All) {
                        Console.WriteLine(this.mclient.Self.AgentID + " " + e.SourceID);
                        try {
                            this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                            dynamic openaiPrompt = this.scriptScope.GetVariable(@"openaiPrompt");
                            string prompt = openaiPrompt(e.SourceID, e.FromName, message, 0);

                            idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id, this.openai_apikey, prompt);
                            idletalkcommand.Execute(e.SourceID, e.FromName, message, 0);
                        } catch(Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }

        void Self_IM(object? sender, InstantMessageEventArgs e) {
            string message = e.IM.Message.Replace("　", " ");
            message = message.Trim();

            switch (e.IM.Dialog) {
                case InstantMessageDialog.RequestTeleport:
                    this.loiter = false;
                    this.stopDance();
                    Console.WriteLine("Accepting teleport lure:" + e.IM.FromAgentName);
                    standupcommand.setCurrentAnims(this.currentAnims);
                    standupcommand.Execute(e.IM.FromAgentID, e.IM.FromAgentName, message, 1);

                    this.mclient.Self.TeleportLureRespond(e.IM.FromAgentID, e.IM.IMSessionID, true);
                    break;
                case InstantMessageDialog.FriendshipOffered:
                    Console.WriteLine("FriendshipOffered");
                    //this.mclient.Friends.AcceptFriendship(e.IM.FromAgentID, e.IM.IMSessionID);
                    break;
                case InstantMessageDialog.MessageFromAgent:
                    Console.WriteLine(e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    if (e.IM.Message.Trim().Length > 0 && !e.IM.Message.Contains("typing")) {
                        this.logger.IMLog(e.IM.FromAgentName, e.IM.Message);
                        this.command(e.IM.FromAgentID, e.IM.FromAgentName, message, 1);
                    }
                    break;
                case InstantMessageDialog.GroupInvitation:
                    Console.WriteLine("GroupInvitation:" + e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    break;
                default:
                    Console.WriteLine("ignored:" + e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    break;
            }
        }

        void command(UUID fromUUID, string fromName, string message ,int type) {
            bool idleTalkExecute = false;
            bool commandFound = false;

            commandFound = commandSearch(fromUUID, fromName, message, type);

            if (!commandFound) {
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic scommand = this.scriptScope.GetVariable(@"command");
                    idleTalkExecute = scommand(fromUUID, fromName, message, type);
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
            if(idleTalkExecute) {
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic openaiPrompt = this.scriptScope.GetVariable(@"openaiPrompt");
                    string prompt = openaiPrompt(fromUUID, fromName, message, type);

                    idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id, this.openai_apikey, prompt);
                    idletalkcommand.Execute(fromUUID, fromName, message, type);
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }
        bool commandSearch(UUID fromUUID, string fromName, string message ,int type) {
            foreach (var comm in commandDic) {
                foreach(var comstring in comm.Value) {
                    Console.WriteLine(comstring);
                    if (message.ToLower().Contains(comstring.ToLower())) {
                        Type t = this.GetType();
                        MethodInfo? mi = t.GetMethod(comm.Key, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mi != null) mi.Invoke(this, new object[] {fromUUID, fromName, message, type});
                        return true;
                    }
                }
            }
            return false;
        }

#region commands
        void helpCommand(UUID fromUUID, string fromName, string message ,int type) {
            string commandList = Constants.COMMANDS;
            this.mclient.Say(fromUUID, commandList, 0, type, false);
        }
        void manualCommand(UUID fromUUID, string fromName, string message ,int type) {
            createnotecardcommand.Execute(fromUUID, fromName, message, type);
        }
        void galmojiConvCommand(UUID fromUUID, string fromName, string message ,int type) {
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "引数を指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            this.mclient.Say(fromUUID, this.mclient.GetGalMoji(arr1[arr1.Length-1]), 0, type);
        }
        void galmojiCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "";
            string arg = message.ToLower();
            if (arg.Contains("on")) {
                this.mclient.galMojiMode = true;
                mes = "ギャル文字をONにしました。OFFにするとき「立川君ギャル文字 OFF」と命令してください。";
            } else if (arg.Contains("off")) {
                this.mclient.galMojiMode = false;
                mes = "ギャル文字をOFFにしました。ONにするとき「立川君ギャル文字 ON」と命令してください。";
            }
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void sitCommand(UUID fromUUID, string fromName, string message ,int type) {
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "UUIDを指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            standupcommand.setCurrentAnims(this.currentAnims);
            standupcommand.Execute(fromUUID, fromName, message, type);

            string target_uuid_string = arr1[arr1.Length-1];
            Console.WriteLine("target:" + target_uuid_string);

            UUID target = new UUID(target_uuid_string);
            this.mclient.Self.RequestSit(target, Vector3.Zero);
            this.mclient.Self.Sit();
        }
        void touchCommand(UUID fromUUID, string fromName, string message ,int type) {
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "UUIDを指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }

            string target_uuid_string = arr1[arr1.Length-1];
            UUID target;
            if (UUID.TryParse(target_uuid_string, out target)) {
                Primitive targetPrim = this.mclient.Network.CurrentSim.ObjectsPrimitives.Find(
                    prim => prim.ID == target
                );

                if (targetPrim != null) {
                    this.mclient.Self.Touch(targetPrim.LocalID);
                } else {
                    string mes = "オブジェクトが見つかりませんでした";
                    this.mclient.Say(fromUUID, mes, 0, type);
                }
            }
        }
        void restCommand(UUID fromUUID, string fromName, string message ,int type) {
            if (String.IsNullOrEmpty(this.bed)) {
                string mes = "ベッドがありません・・・";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            string target_uuid_string = this.bed;
            UUID target = new UUID(target_uuid_string);
            this.mclient.Self.RequestSit(target, Vector3.Zero);
            this.mclient.Self.Sit();
        }
        void standCommand(UUID fromUUID, string fromName, string message ,int type) {
            standupcommand.setCurrentAnims(this.currentAnims);
            standupcommand.Execute(fromUUID, fromName, message, type);
        }
        void followCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.loiter = false;
            this.stopDance();
            this.mclient.Self.AutoPilotCancel();
            standupcommand.setCurrentAnims(this.currentAnims);
            standupcommand.Execute(fromUUID, fromName, message, type);

            //followTarget = e.SourceID;
            followTarget = fromName;
            Console.WriteLine(followTarget.ToString());
        }
        void stopCommand(UUID fromUUID, string fromName, string message ,int type) {
            followTarget = null;
            this.loiter = false;
            this.stopDance();
            this.dancing = false;
            standupcommand.setCurrentAnims(this.currentAnims);
            standupcommand.Execute(fromUUID, fromName, message, type);
            this.mclient.Self.AutoPilotCancel();
        }
        void teleportCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.loiter = false;
            this.stopDance();
            this.dancing = false;
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "宛先をsim名/x/y/zで指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }

            standupcommand.setCurrentAnims(this.currentAnims);
            standupcommand.Execute(fromUUID, fromName, message, type);
            string target = arr1[arr1.Length-1];
            Console.WriteLine(target);

            teleportcommand.setTarget(target);
            teleportcommand.Execute(fromUUID, fromName, message, type);
        }
        void exitCommand(UUID fromUUID, string fromName, string message ,int type) {
            if (fromName != this.owner) {
                string mes = "Exit order revoked.";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            this.exitApp();
        }
        void groupCommand(UUID fromUUID, string fromName, string message ,int type) {
            // グループタグに関係しそうなとこ
            // https://github.com/cinderblocks/libremetaverse/blob/e26ae695fed27e43a510a85d786abbac2552d051/Programs/examples/TestClient/Commands/Groups/GroupRolesCommand.cs
            // https://github.com/cinderblocks/libremetaverse/blob/e26ae695fed27e43a510a85d786abbac2552d051/Programs/examples/TestClient/Commands/Groups/ActivateGroupCommand.cs
            //string groupName = "泪橋";

            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "グループ名を指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            string groupName = arr1[arr1.Length-1];
            Console.WriteLine(groupName);

            UUID groupUUID = this.mclient.GroupName2UUID(groupName);
            Console.WriteLine(groupUUID);
            if (UUID.Zero != groupUUID) {
                EventHandler<PacketReceivedEventArgs> pcallback = AgentDataUpdateHandler;
                this.mclient.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);

                Console.WriteLine("setting " + groupName + " as active group");
                this.mclient.Groups.ActivateGroup(groupUUID);
                GroupsEvent.WaitOne(30000, false);

                this.mclient.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
                GroupsEvent.Reset();

                if (String.IsNullOrEmpty(activeGroup))
                Console.WriteLine(this.mclient.ToString() + " failed to activate the group " + groupName);
            }
        }
        void gotoHomeCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.loiter = false;
            this.stopDance();
            this.dancing = false;
            string mes = "お家へ帰りまーす！";
            if (String.IsNullOrEmpty(this.home)) {
                mes = "お家がありません・・・";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            this.mclient.Say(fromUUID, mes, 0, type);

            standupcommand.setCurrentAnims(this.currentAnims);
            standupcommand.Execute(fromUUID, fromName, message, type);

            //this.mclient.Self.GoHome();
            string target = this.home;
            teleportcommand.setTarget(target);
            teleportcommand.Execute(fromUUID, fromName, message, type);
        }
        void setHomeCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Self.SetHome();
            string mes = "ここをホームに設定しました";
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void chatModeCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "";
            if (message.Contains("指名")) {
                this.chatMode = ChatMode.Nominate;
                mes = "チャットモードを指名モードに変更しました。";
                foreach (var nickname in this.nicknames) {
                    mes += nickname + ",";
                }
                mes += "から始まる言葉のみ反応します。";
            } else if (message.Contains("全レス")) {
                this.chatMode = ChatMode.All;
                mes = "チャットモードを全レスモードに変更しました。うるさい場合は指名モードにしてください。";
            }
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void chatApiCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "";
            if (message.Contains("chatplus")) {
                this.chatApi = ChatApi.chatplus;
                mes = "チャットAPIをchatplusに変更しました。会話のドッジボールをお楽しみください。";
            } else if (message.Contains("mebo")) {
                this.chatApi = ChatApi.mebo;
                mes = "チャットAPIをmeboに変更しました。会話のラリーをお楽しみください。";
            } else if (message.Contains("openai")) {
                this.chatApi = ChatApi.openai;
                mes = "チャットAPIをOpenAIに変更しました。イーロン・マスクをお楽しみください。";
            }
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void whereCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "Sim: " + this.mclient.Network.CurrentSim.ToString() + "' Position: " + this.mclient.Self.SimPosition.ToString();
            Vector3 globalPos = this.getCurrentGlobalPosition();
            mes += "\n" + "グローバル座標:" + globalPos.X + "," + globalPos.Y + "." + globalPos.Z;
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void channelChatCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "";
            int index = message.IndexOf("チャンネルチャット");
            int index2 = message.IndexOf(" ", index);
            if (index2 == -1) {
                mes = "チャンネル番号を指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            int index3 = message.IndexOf(" ", index2+1);
            if (index3 == -1) {
                mes = "押したいボタンを指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }

            string channel = message.Substring(index2+1, index3 - index2-1); // わからん！スペースで一回配列にしたほうがいいのか？
            string button = message.Substring(index3+1,message.Length - index3-1);
            mes = "チャンネル:" + Int32.Parse(channel).ToString() + ",押したいボタン:" + button;
            this.mclient.Say(fromUUID, mes, 0, type);

            // FIX ME: マイナスチャンネルでの発言が通らない
            this.mclient.Self.Chat(button, Int32.Parse(channel), ChatType.Normal);

            // MEGABOLTの実装
            //this.mclient.Self.ReplyToScriptDialog(ed.Channel, butindex, butlabel, ed.ObjectID);
        }
        void buttonClickCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "";
            int index = message.IndexOf("ボタンクリック");
            int index2 = message.IndexOf(" ", index);
            string arrString = message.Substring(index2+1, message.Length - index2-1);
            string[] arr =  arrString.Split(' ');

            if (arr.Length == 0) {
                mes = "チャンネル番号を指定してください";
            } else if (arr.Length == 1) {
                mes = "インデックスを指定してください";
            } else if (arr.Length == 2) {
                mes = "ボタン名を指定してください";
            } else if (arr.Length == 3) {
                mes = "オブジェクトIDを指定してください";
            }
            if (mes.Length > 0) {
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            string channel = arr[0];
            string btnIndex = arr[1];
            string btnLabel = arr[2];
            string objectId = arr[3];
            //mes = channel + "," + btnIndex + "," + btnLabel + "," + objectId;
            //this.mclient.Say(fromUUID, mes, 0, type);

            this.mclient.Self.ReplyToScriptDialog(Int32.Parse(channel), Int32.Parse(btnIndex), btnLabel, UUID.Parse(objectId));
        }
        void forwardCommand(UUID fromUUID, string fromName, string message ,int type) {
            movecommand.Forward();
        }
        void backCommand(UUID fromUUID, string fromName, string message ,int type) {
            movecommand.Back();
        }
        void rightCommand(UUID fromUUID, string fromName, string message ,int type) {
            movecommand.Right();
        }
        void leftCommand(UUID fromUUID, string fromName, string message ,int type) {
            movecommand.Left();
        }
        void loiterCommand(UUID fromUUID, string fromName, string message ,int type) {
            //this.findRandomObject();
            //this.targetPrim = this.getNextPrim();
            this.followTarget = null;
            this.loiterStartRegionPos = this.mclient.Self.SimPosition;
            this.loiter = true;
        }
        void randomCommand(UUID fromUUID, string fromName, string message ,int type) {
            string mes = "";
            string arg = message.ToLower();
            if (arg.Contains("on")) {
                this.randomChat = true;
                mes = "ランダム発言をONにしました。邪魔なときな「立川君ランダム発言 OFF」と命令してください。";
            } else if (arg.Contains("off")) {
                this.randomChat = false;
                mes = "ランダム発言をOFFにしました。寂しいときな「立川君ランダム発言 ON」と命令してください。";
            }
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void animListCommand(UUID fromUUID, string fromName, string message ,int type) {
            animationcommand.list(fromUUID, fromName, message, type);
        }
        void animCommand(UUID fromUUID, string fromName, string message ,int type) {
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "アニメーション名を指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            string animName = arr1[arr1.Length-1];
            animationcommand.play(animName);
        }
        void danceCommand(UUID fromUUID, string fromName, string message ,int type) {
            //Random r = new System.Random();
            //int next = r.Next(1, 7);
            //string danceanim = "DANCE" + next.ToString();
            //animationcommand.play(danceanim);
            this.randomDance();
        }
        async void secondLifeCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Say(fromUUID, await SecondLifeFeedCommand.feed(), 0, type);
        }
        void itemListCommand(UUID fromUUID, string fromName, string message ,int type) {
            UUID folderUUID = UUID.Zero;
            List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(this.mclient.Inventory.Store.RootFolder.UUID);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    if (i.Name == "Objects") {
                        folderUUID = i.UUID;
                        break;
                    }
                    //if (i is InventoryFolder folder) {
                    //    // TODO: Nest
                    //}
                }
            } else {
                Console.WriteLine("Objects folder not found");
                return;
            }

            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            List<InventoryItem> items = new List<InventoryItem>();
            contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "Objects");
                return;
            }
            string mes = "";
            foreach (InventoryBase item in contents)
            {
                mes += item.Name + ",\n";
            }
            this.mclient.Say(fromUUID, mes, 0, type);
        }
        void attachCommand(UUID fromUUID, string fromName, string message ,int type) {
            if (fromName != this.owner) {
                string mes = "attach order revoked." + fromName + "," + this.owner;
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "アイテム名称を指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            string itemName = arr1[arr1.Length-1];
            Console.WriteLine(itemName);

            UUID folderUUID = UUID.Zero;
            List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(this.mclient.Inventory.Store.RootFolder.UUID);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    if (i.Name == "Objects") {
                        folderUUID = i.UUID;
                        break;
                    }
                    //if (i is InventoryFolder folder) {
                    //    // TODO: Nest
                    //}
                }
            } else {
                Console.WriteLine("Objects folder not found");
                return;
            }

            //UUID folderUUID = new UUID("d5131324-0362-4556-992e-65a912d515dd"); // Objects
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            //UUID itemUUid = new UUID("0ba3cfeb-7417-7be4-0df9-1288625d5470"); // Fairy Wings I SUPPORT UKRAINE v11
            ////FIX ME: item null
            //InventoryItem item = this.mclient.Inventory.FetchItem(itemUUid, this.mclient.Self.AgentID, 1000);
            //if (item == null) {
            //    Console.WriteLine("attach item is null");
            //    return;
            //}
            ////InventoryItem item = (InventoryItem)this.mclient.Inventory.Store[itemUUid];
            /*List<InventoryBase> */contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            List<InventoryItem> items = new List<InventoryItem>();
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "Objects");
                return;
            }
            foreach (InventoryBase item in contents)
            {
                if (item is InventoryItem inventoryItem) {
                    items.Add(inventoryItem);
                    if (inventoryItem.Name.Contains(itemName)) {
                        this.mclient.Appearance.Attach(inventoryItem, AttachmentPoint.Default);
                        Console.WriteLine(inventoryItem.Name + "," + inventoryItem.UUID.ToString());
                        break;
                    }
                }
            }
        }
        void detachCommand(UUID fromUUID, string fromName, string message ,int type) {
            var arr1 = message.Split(' ');
            if (arr1.Length == 1) {
                string mes = "アイテム名称を指定してください";
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
            string itemName = arr1[arr1.Length-1];
            Console.WriteLine(itemName);

            UUID folderUUID = UUID.Zero;
            List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(this.mclient.Inventory.Store.RootFolder.UUID);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    if (i.Name == "Objects") {
                        folderUUID = i.UUID;
                        break;
                    }
                    //if (i is InventoryFolder folder) {
                    //    // TODO: Nest
                    //}
                }
            } else {
                Console.WriteLine("Objects folder not found");
                return;
            }
            //UUID folderUUID = new UUID("d5131324-0362-4556-992e-65a912d515dd"); // Objects
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            //UUID itemUUid = new UUID("0ba3cfeb-7417-7be4-0df9-1288625d5470"); // Fairy Wings I SUPPORT UKRAINE v11
            ////FIX ME: item null
            //InventoryItem item = this.mclient.Inventory.FetchItem(itemUUid, this.mclient.Self.AgentID, 1000);
            ////InventoryItem item = (InventoryItem)this.mclient.Inventory.Store[itemUUid];
            //if (item == null) {
            //    Console.WriteLine("detach item is null");
            //    return;
            //}
            /*List<InventoryBase> */contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            List<InventoryItem> items = new List<InventoryItem>();
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "Objects");
                return;
            }
            foreach (InventoryBase item in contents)
            {
                if (item is InventoryItem inventoryItem) {
                    items.Add(inventoryItem);
                    if (inventoryItem.Name.Contains(itemName)) {
                        this.mclient.Appearance.Detach(inventoryItem);
                        Console.WriteLine(inventoryItem.Name + "," + inventoryItem.UUID.ToString());
                        break;
                    }
                }
            }
        }
        void inventoryCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.inventorylistcommand.Execute(fromUUID, fromName, message, type);
        }
        void appearanceCommand(UUID fromUUID, string fromName, string message ,int type) {
            // CAUTION: 現在実装が不完全です

            //string[] args = new String[]{"Clothing/kani"}; // Clothing/kani
            //string appearance = args.Aggregate(string.Empty, (current, t) => current + (t + " "));
            //appearance = appearance.TrimEnd();
            //// FIX ME: フォルダが見つけられない
            //UUID folder = this.mclient.Inventory.FindObjectByPath(this.mclient.Inventory.Store.RootFolder.UUID, this.mclient.Self.AgentID, appearance, 20 * 1000);
            //if (folder == UUID.Zero) {
            //    Console.WriteLine("Outfit path " + appearance + " not found");
            //    return;
            //}

            // ERROR - Failed to fetch the current agent wearables, cannot safely replace outfit
            // AppearanceManager.cs if (needsCurrentWearables && !GetAgentWearables())
            // kani 0b329337-02d8-0aad-4202-ece611bf500e
            // nuko ef676e23-ea67-0abb-6e67-03e9d499e43a
            UUID folderUUID = new UUID("0b329337-02d8-0aad-4202-ece611bf500e");
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            List<InventoryBase> contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            List<InventoryItem> items = new List<InventoryItem>();
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "nuko");
                return;
            }
            foreach (InventoryBase item in contents)
            {
                if (item is InventoryItem inventoryItem)
                    items.Add(inventoryItem);
            }
            var lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            lockSlim.EnterWriteLock();
            this.mclient.Appearance.ReplaceOutfit(items, false);
            lockSlim.ExitWriteLock();
            this.mclient.Appearance.RequestSetAppearance(true);
        }
        void shootCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.mclient.Self.Movement.Mouselook = true;
            this.mclient.Self.Movement.MLButtonDown = true;
            this.mclient.Self.Movement.SendUpdate();

            this.mclient.Self.Movement.MLButtonUp = true;
            this.mclient.Self.Movement.MLButtonDown = false;
            this.mclient.Self.Movement.FinishAnim = true;
            this.mclient.Self.Movement.SendUpdate();

            this.mclient.Self.Movement.Mouselook = false;
            this.mclient.Self.Movement.MLButtonUp = false;
            this.mclient.Self.Movement.FinishAnim = false;
            this.mclient.Self.Movement.SendUpdate();
        }
        void debugCommand(UUID fromUUID, string fromName, string message ,int type) {
            this.randomDance();
        }
#endregion
        private void randomDance() {
            UUID dir = getInventoryUUID("#LittlePierceDanceSet", this.mclient.Inventory.Store.RootFolder.UUID);
            if (dir != UUID.Zero) {
                int count = this.danceDic.Count();
                Random r = new System.Random();
                int danceNum = r.Next(0, count-1);
                string key = this.danceDic.Keys.ToList()[danceNum];
                List<string> nthValue = this.danceDic[key];
                string danceName = nthValue[0];
                string danceCommand = nthValue[1];
                int danceTime = Int32.Parse(nthValue[2]);
                Console.WriteLine("DanceName:" + danceName + ",DanceCommand:" + danceCommand + ",DanceTime:" + danceTime.ToString());

                this.dancing = true;
                Console.WriteLine("attach dir->" + dir.ToString());
                attach(dir, danceName);
                Thread.Sleep(1000 * 10);
                this.mclient.Say(UUID.Zero, "聞いてください、" + key, 0, 0);
                this.mclient.Say(UUID.Zero, danceCommand, 0, 0);
                Thread.Sleep(danceTime);
                detach(dir, danceName);
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(UUID.Zero, "", "", 0);
                this.dancing = false;
            }
        }

        private void stopDance() {
            UUID dir = getInventoryUUID("#LittlePierceDanceSet", this.mclient.Inventory.Store.RootFolder.UUID);
            if (dir != UUID.Zero) {
                allDetach(dir);
            }
        }

        private Primitive? getNextPrim() {
            if (this.MovementTargetPrims == null) return null;
            int primsCount = MovementTargetPrims.Count;
            Random r = new System.Random();
            int next = r.Next(0, primsCount);
            int i=0;
            foreach (Primitive p in MovementTargetPrims)
            {
                if (i == next) {
                    //target
                    Console.WriteLine(p.Properties.Name);
                    return p;
                }
                i++;
            }
            return null;
        }

        void Self_AnimationsChanged(object? sender, AnimationsChangedEventArgs e) {
            var anims = e.Animations;
            //Console.WriteLine(anims.Count);

            // get current Animations UUID
            this.currentAnims.Clear();
            anims.ForEach(delegate(UUID anim){
                //Console.WriteLine(anim);
                this.currentAnims.Add(anim);
            });
        }
        private void updateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) {
            //foreach (Command c in Commands.Values)
            //    if (c.Active)
            //        c.Think();
            this.follow();
            if (this.randomChat) {
                TimeSpan d = DateTime.Now - MyApplication.lastChatDateTime;
                if (d.TotalSeconds > Constants.RANDOM_CHAT_TIMER) {
                    Console.WriteLine("ランダムな発言");
                    Random r = new System.Random();
                    int max = 4; // 0-3
                    if (this.chatApi == ChatApi.openai) {
                        max = 2; // 0-1
                    }
                    int nextAction = r.Next(0, max);
                    if (nextAction == 0) {
                        int ramdomGestureNum = r.Next(0, 10); // 0-9
                        try {
                            this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                            dynamic randomGesture = this.scriptScope.GetVariable(@"randomGesture");
                            randomGesture(ramdomGestureNum);
                            MyApplication.lastChatDateTime = DateTime.Now;
                        } catch(Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    } else if (nextAction == 1) {
                        idletalkcommand.rondomMessage();
                    } else {
                        idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id, this.openai_apikey, "");
                        idletalkcommand.Execute(UUID.Zero, "", Constants.RANDOM_CHAT_SEED_MESSAGE, 0);
                    }
                }
            }

            if (this.loiter == true && dancing == false) {

                TimeSpan d = DateTime.Now - this.lastLoiterDateTime;
                if (d.TotalSeconds > Constants.LOITER_TIMER) {
                    Random r = new System.Random();
                    int nextAction = r.Next(0, 10); // 0-9
                    standupcommand.setCurrentAnims(this.currentAnims);
                    standupcommand.Execute(UUID.Zero, "", "", 0);

                    if (nextAction < 5) {
                        Console.WriteLine("開始場所:" + this.loiterStartRegionPos.X + "," + this.loiterStartRegionPos.Y);
                        int targetX = r.Next(Constants.LOITER_RADIUS*-1, Constants.LOITER_RADIUS);
                        int targetY = r.Next(Constants.LOITER_RADIUS*-1, Constants.LOITER_RADIUS);

                        Console.WriteLine("ターゲット位置:" + (this.loiterStartRegionPos.X + targetX) + "," + (this.loiterStartRegionPos.Y + targetY));

                        Vector3 newDirection;
                        newDirection.X = (float)this.loiterStartRegionPos.X + targetX;
                        newDirection.Y = (float)this.loiterStartRegionPos.Y + targetY;
                        newDirection.Z = (float)this.loiterStartRegionPos.Z;
                        this.mclient.Self.Movement.TurnToward(newDirection);
                        this.mclient.Self.Movement.SendUpdate(false);

                        uint regionX, regionY;
                        foreach(var t in this.mclient.Network.Simulators) {
                            Utils.LongToUInts(t.Handle, out regionX, out regionY);
                            double xTarget = (double)this.loiterStartRegionPos.X + targetX + (double)regionX;
                            double yTarget = (double)this.loiterStartRegionPos.Y + targetY + (double)regionY;
                            double zTarget = this.loiterStartRegionPos.Z - 2f;
                            this.mclient.Self.AutoPilot(xTarget, yTarget, zTarget);
                            break;
                        }
                    } else if (nextAction < 8 ) {
                        this.findRandomObject();
                        Primitive? targetPrim = this.getNextPrim();
                        if (targetPrim != null) {
                            Console.WriteLine("Sit->" + targetPrim.Properties.Name);
                            //this.mclient.Self.Touch(targetPrim.LocalID);
                            this.mclient.Self.RequestSit(targetPrim.ID, Vector3.Zero);
                            this.mclient.Self.Sit();
                        }

                    } else {
                        string target = this.mclient.Network.CurrentSim.Name.ToString() + "/" + this.loiterStartRegionPos.X + "/" + this.loiterStartRegionPos.Y + "/" + this.loiterStartRegionPos.Z;
                        //this.mclient.Say(UUID.Zero, target, 0, 0);
                        teleportcommand.setTarget(target);
                        teleportcommand.Execute(UUID.Zero, "", "", 0);
                        this.randomDance();
                    }

                    this.lastLoiterDateTime = DateTime.Now;
                }


            }
            try {
                this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                dynamic updateTimer_Elapsed = this.scriptScope.GetVariable(@"updateTimer_Elapsed");
                var info = updateTimer_Elapsed();
                //Console.WriteLine(info);
            } catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }

        private void findRandomObject() {
            float radius = 5;
            Vector3 location = this.mclient.Self.SimPosition;
            List<Primitive> prims = this.mclient.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                }
            );

            // *** request properties of these objects ***
            bool complete = RequestObjectProperties(prims, 250);

            this.MovementTargetPrims = new List<Primitive>(prims);

        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }

            this.mclient.Objects.SelectObjects(this.mclient.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }

        void Objects_OnObjectProperties(object? sender, ObjectPropertiesEventArgs e)
        {
            lock (PrimsWaiting)
            {
                Primitive? prim;
                if (PrimsWaiting.TryGetValue(e.Properties.ObjectID, out prim))
                {
                    prim.Properties = e.Properties;
                }
                PrimsWaiting.Remove(e.Properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }

        void Network_OnCurrentSimChanged(object? sender, SimChangedEventArgs e) {
            Console.WriteLine("OnCurrentSimChanged:" + e.ToString());
        }
        void Grid_CoarseLocationUpdate(object? sender, CoarseLocationUpdateEventArgs e) {
            List<UUID> newEntries = e.NewEntries;
            foreach(UUID t in newEntries) {
                Console.WriteLine("Grid_CoarseLocationUpdate:" + e.Simulator.Name + "," + t.ToString());
            }
        }
        void Objects_OnNewAvatar(object? sender, AvatarUpdateEventArgs e)
        {
            //Console.WriteLine("OnNewAvatar:" + e.Avatar.Name);
        }

        void Objects_OnObjectUpdated(object? sender, TerseObjectUpdateEventArgs e)
        {
            if (e.Update.Avatar) {
                Avatar av;
                e.Simulator.ObjectsAvatars.TryGetValue(e.Update.LocalID, out av);
                //Console.WriteLine("OnObjectUpdated:" + av.Name);
            }
        }

        public static Vector3? getTargetPos(MyClient client,string targetName) {
            if (targetName != null) {
                lock(client.Network.Simulators) {
                    //Console.WriteLine(followTarget);
                    foreach(var t in client.Network.Simulators) {
                        Avatar targetAv = t.ObjectsAvatars.Find(avatar => avatar.Name == targetName);
                        if (targetAv != null) {
                            return targetAv.Position;
                        }
                    }
                    return null;
                }
            } else {
                return null;
            }
        }
        private void follow() {
            if (followTarget != null) {
                lock(this.mclient.Network.Simulators) {
                    //Console.WriteLine(followTarget);
                    foreach(var t in this.mclient.Network.Simulators) {
                        //Console.WriteLine(t.Name); // MEMO:Simulatorsは現在いるリージョンを含め十字に5個のリージョンが返ってくる
                        Avatar targetAv = t.ObjectsAvatars.Find(avatar => avatar.Name == followTarget);

                        //Avatar targetAv;
                        ////t.ObjectAvatarsはUUIDじゃないのか・・・
                        //if (t.ObjectsAvatars.TryGetValue(followTarget, out targetAv)) {
                        //}

                        // MEMO:追いかけてる途中でリージョンをまたいた場合と、別のリージョンから呼びかけた場合で挙動が異なる
                        // 追いかけてる途中でリージョンをまたぐと、ターゲットがすべてのSimulatarsから全く見つからなくなる
                        // おそらくアバターのアップデート情報を取得する必要があると思われる
                        // 別のリージョンから呼びかけた場合、別のSimulatorでターゲットアバターが見つかるので、
                        // FIXME: Calculate global distancesを対処すればいいと思われる
                        // 全く別の問題として、アバターが見えない区画設定になっている場所へ移動するとターゲットアバターが見つからなくなる問題があるが、解決方法は多分ない
                        //if (targetAv == null) {
                        //    Console.WriteLine("follow missing:" + targetAv + " in " + t.Name);
                        //}
                        if (targetAv != null) {
                            //Console.WriteLine("Target:" + (double)targetAv.Position.X + "," + (double)targetAv.Position.Y); // 231.68441772460938,118.41368103027344

                            float DISTANCE_BUFFER = 1.0f;
                            float distance = 0.0f;
                            if (t == this.mclient.Network.CurrentSim) {
                                distance = Vector3.Distance(targetAv.Position, mclient.Self.SimPosition);
                                //Console.WriteLine(distance.ToString());
                            } else {
                                // FIXME: Calculate global distances
                                Console.WriteLine("target->" + t.Name + "," + "current->" + this.mclient.Network.CurrentSim.Name);
                            }

                            if (distance > DISTANCE_BUFFER) {
                                uint regionX, regionY;
                                Utils.LongToUInts(t.Handle, out regionX, out regionY);

                                double xTarget = (double)targetAv.Position.X + (double)regionX;
                                double yTarget = (double)targetAv.Position.Y + (double)regionY;
                                double zTarget = targetAv.Position.Z - 2f;
                                //Console.WriteLine("Region:" + regionX + "," + regionY); // 263680,248064 多分そのリージョンの0,0座標
                                //Console.WriteLine("Target:" + (double)targetAv.Position.X + "," + (double)targetAv.Position.Y); // 231.68441772460938,118.41368103027344
                                //Console.WriteLine("AutoPilot:" + xTarget + "," + yTarget);

                                if (zTarget > 0) this.mclient.Self.AutoPilot(xTarget, yTarget, zTarget);
                            } else {
                                // We are in range of the target and moving, stop moving
                                this.mclient.Self.AutoPilotCancel();
                            }
                        }
                    }
                }
            }
        }

        private void AgentDataUpdateHandler(object? sender, PacketReceivedEventArgs e) {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
            if (p.AgentData.AgentID == this.mclient.Self.AgentID) {
                activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " ( " + Utils.BytesToString(p.AgentData.GroupTitle) + " )";
                GroupsEvent.Set();
            }
        }

        void Self_ScriptDialog(object? sender, ScriptDialogEventArgs e) {
            this.mclient.Self.Chat("チャンネル:" + e.Channel.ToString() + ",オブジェクトID:" + e.ObjectID, 0, ChatType.Normal);
            string mes = "インデックス:ボタン名: ";
            int i=0;
            foreach(var label in e.ButtonLabels) {
                mes += i.ToString() + ":" + label + ",";
                i++;
            }
            this.mclient.Self.Chat(mes, 0, ChatType.Normal);
            //this.mclient.Self.Chat("チャンネルチャット チャンネル番号 ボタン名称", 0, ChatType.Normal);
            this.mclient.Self.Chat("ボタンクリック チャンネル番号 インデックス ボタン名 オブジェクトID", 0, ChatType.Normal);
        }

        void Inventory_InventoryObjectOfferd(object? sender, InventoryObjectOfferedEventArgs e) {
            Console.WriteLine("Accepting InventoryOffer" + e.Offer.FromAgentName + " " + e.Offer.Message);
            e.Accept = true;
            this.mclient.Self.PlayGesture(new UUID("68ca4eb0-4b4d-4dce-39fb-df6f5e08fee9"));
        }

        void Inventory_OnFolderUpdated(object? sender, FolderUpdatedEventArgs e) {
            ItemEvent.Set();
            this.inventorylistcommand.UpdateFolder(e.FolderID);
        }
        void Frind_FrienshipOfferd(object? sender, FriendshipOfferedEventArgs e) {
            Console.WriteLine("Accepting Friendship:" + e.AgentName);
            this.mclient.Friends.AcceptFriendship(e.AgentID, e.SessionID);
        }

        void Group_GroupInvitation(object?sender, GroupInvitationEventArgs e) {
            Console.WriteLine("GroupInvitation:" + e.FromName + "," + e.Message);
            // TODO:グループ招待をどうするかOwnerにIMを送信して返事で決めさせる
            //e.Accept = true;
            e.Accept = false;
        }

        public Vector3 getCurrentGlobalPosition() {
            Vector3 pos = this.mclient.Self.SimPosition;
            uint regionX, regionY;
            foreach(var t in this.mclient.Network.Simulators) {
                Utils.LongToUInts(t.Handle, out regionX, out regionY);
                double xTarget = (double)this.loiterStartRegionPos.X + (double)regionX;
                double yTarget = (double)this.loiterStartRegionPos.Y + (double)regionY;
                double zTarget = this.loiterStartRegionPos.Z - 2f;
                this.mclient.Self.AutoPilot(xTarget, yTarget, zTarget);
                return new Vector3((float)xTarget, (float)yTarget, (float)zTarget);
            }
            return Vector3.Zero;
        }

        private UUID getInventoryUUID(string name, UUID panretFolderUUID/*=this.mclient.Inventory.Store.RootFolder.UUID*/) {
            UUID folderUUID = UUID.Zero;
            List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(panretFolderUUID);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    if (i.Name == name) {
                        folderUUID = i.UUID;
                        Console.WriteLine("Object found:" + name + "," + folderUUID.ToString());
                        return folderUUID;
                        //break;
                    }
                    if (i is InventoryFolder folder) {
                        folderUUID = getInventoryUUID(name, i.UUID);
                        if (folderUUID != UUID.Zero) return folderUUID;
                    }
                }
            } else {
                Console.WriteLine("Objects folder not found");
            }

            return folderUUID;
        }

        private void attach(UUID folderUUID, string itemName) {
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            List<InventoryBase> contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            List<InventoryItem> items = new List<InventoryItem>();
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "Objects");
                return;
            }
            foreach (InventoryBase item in contents)
            {
                if (item is InventoryItem inventoryItem) {
                    items.Add(inventoryItem);
                    if (inventoryItem.Name.Contains(itemName)) {
                        this.mclient.Appearance.Attach(inventoryItem, AttachmentPoint.AltLeftEar);
                        Console.WriteLine("attach->" + inventoryItem.Name + "," + inventoryItem.UUID.ToString());
                        break;
                    }
                }
            }
        }

        private void detach(UUID folderUUID, string itemName) {
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            List<InventoryBase> contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            List<InventoryItem> items = new List<InventoryItem>();
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "Objects");
                return;
            }
            foreach (InventoryBase item in contents)
            {
                if (item is InventoryItem inventoryItem) {
                    items.Add(inventoryItem);
                    if (inventoryItem.Name.Contains(itemName)) {
                        this.mclient.Appearance.Detach(inventoryItem);
                        Console.WriteLine("detach->" + inventoryItem.Name + "," + inventoryItem.UUID.ToString());
                        break;
                    }
                }
            }
        }

        private void allDetach(UUID folderUUID) {
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

            ItemEvent.WaitOne(30000, false);
            List<InventoryBase> contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
            List<InventoryItem> items = new List<InventoryItem>();
            if (contents == null) {
                Console.WriteLine("Failed to get contents of " + "Objects");
                return;
            }
            foreach (InventoryBase item in contents)
            {
                if (item is InventoryItem inventoryItem) {
                    items.Add(inventoryItem);
                    this.mclient.Appearance.Detach(inventoryItem);
                }
            }
        }
    }
}