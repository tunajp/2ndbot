using OpenMetaverse;

namespace SecondBot.Client {
    public class TeleportCommand : Command {

        private string target;
        public TeleportCommand(MyClient mclient) {
            this.mclient = mclient;
            this.target = "";
        }
        public void setTarget(string target) {
            this.target = target;
        }
        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
            string[] tokens = this.target.Split(new char[] { '/' });
            if (tokens != null) {
                string sim = tokens[0];
                float x, y, z;
                if (!float.TryParse(tokens[1], out x) ||
                    !float.TryParse(tokens[2], out y) ||
                    !float.TryParse(tokens[3], out z))
                {
                    string mes = "Usage: goto sim/x/y/z";
                    this.mclient.Say(fromUUID, mes, 0, type);

                    return;
                }
                if (this.mclient.Self.Teleport(sim, new Vector3(x, y, z))) {
                } else {
                    string mes = "Teleport failed: " + this.mclient.Self.TeleportMessage;
                    this.mclient.Say(fromUUID, mes, 0, type);
                }
            }
        }

    }
}