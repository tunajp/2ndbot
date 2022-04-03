namespace SecondBot.Client {
    static class Constants
    {
        public const string COMMANDS = "マニュアル,座 uuid,休,立ち,おいで,止,前へ,後ろへ,右へ,左へ,テレポ sim/x/y/z,終了,グループ groupname,帰/戻,チャットモード 指名or全レス,チャットAPI chatplus or mebo,どこ,タッチ uuid,ランダム発言 ON/OFF,アニメリスト,アニメ アニメ名,踊";

        public const double RANDOM_CHAT_TIMER = 180;
        public const string RANDOM_CHAT_SEED_MESSAGE = "ランダム";

        public const string SETTING_XML_FILENAME = ".settings.xml";

        public const string NOTECARD_TEXT_FILENAME = "notecard.txt";
        public const int NOTECARD_CREATE_TIMEOUT = 1000 * 10;
    }
}
