namespace SecondBot.Client {
    static class Constants
    {
        public const string COMMANDS = "マニュアル(manual),ギャル文字変換 変換したい文言,ギャル文字 ON/OFF,座 uuid,休,立ち,おいで,止,前へ,後ろへ,右へ,左へ,テレポ sim/x/y/z,うろうろ,終了,グループ groupname,帰/戻,チャットモード 指名or全レス,チャットAPI mebo or openai,どこ,タッチ uuid,ランダム発言 ON/OFF,アニメリスト,アニメ アニメ名,踊,画像ください";

        public const double RANDOM_CHAT_TIMER = 180;
        public const string RANDOM_CHAT_SEED_MESSAGE = "ランダム";
        public const double LOITER_TIMER = 10;
        public const int LOITER_RADIUS = 10;

        public const string SETTING_XML_FILENAME = ".settings.xml";

        public const string NOTECARD_TEXT_FILENAME = "notecard.txt";
        public const int NOTECARD_CREATE_TIMEOUT = 1000 * 10;

        public const string LOGDIR = "log";
        public const string CHATLOGFILE = "chat.txt";
        public const string LOGFILE = "log.txt";

        public const string OPENAIIMAGEDIR = "openai_images";
    }
}
