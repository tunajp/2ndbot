using OpenMetaverse;

namespace SecondBot.Client {

    public abstract class Command {
        public MyClient mclient;

        public abstract void Execute(UUID fromUUID, string fromName, string message ,int type);
    }
}