using OpenMetaverse;

namespace SecondBot.Client {
    public class StandupCommand : Command {

        private List<UUID>? currentAnims;
        public StandupCommand(MyClient mclient) {
            this.mclient = mclient;
        }
        public void setCurrentAnims(List<UUID> currentAnims) {
            this.currentAnims = new List<UUID>(currentAnims);
        }
        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
            if (this.currentAnims != null) {
                foreach(UUID anim in this.currentAnims) {
                    this.mclient.Self.AnimationStop(anim, true);
                }
            }
            this.mclient.Self.Stand();
            this.mclient.Self.Movement.SendUpdate();
            //this.mclient.Self.AnimationStart(new UUID("2408fe9e-df1d-1d7d-f4ff-1384fa7b350f"), true);
            //this.mclient.Self.AnimationStart(Animations.SIT_TO_STAND, true);
            //this.mclient.Self.AutoPilot(231, 115, 23);

            //this.mclient.Self.Movement.StandUp = true;
            //this.mclient.Self.Movement.SendUpdate();
            //this.mclient.Self.AnimationStop(new UUID("2dd8c10b-2b4f-7526-b2f1-67923ce74c06"), true);
        }

    }
}