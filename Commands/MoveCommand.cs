using OpenMetaverse;

namespace SecondBot.Client {
    public class MoveCommand : Command {

        public MoveCommand(MyClient mclient) {
            this.mclient = mclient;
        }

        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
        }

        public void Forward() {
            this.mclient.Self.Movement.SendManualUpdate(AgentManager.ControlFlags.AGENT_CONTROL_AT_POS, this.mclient.Self.Movement.Camera.Position,
                this.mclient.Self.Movement.Camera.AtAxis, this.mclient.Self.Movement.Camera.LeftAxis, this.mclient.Self.Movement.Camera.UpAxis,
                this.mclient.Self.Movement.BodyRotation, this.mclient.Self.Movement.HeadRotation, this.mclient.Self.Movement.Camera.Far, AgentFlags.None,
                AgentState.None, true);
        }

        public void Back() {
            this.mclient.Self.Movement.SendManualUpdate(AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG, this.mclient.Self.Movement.Camera.Position,
                this.mclient.Self.Movement.Camera.AtAxis, this.mclient.Self.Movement.Camera.LeftAxis, this.mclient.Self.Movement.Camera.UpAxis,
                this.mclient.Self.Movement.BodyRotation, this.mclient.Self.Movement.HeadRotation, this.mclient.Self.Movement.Camera.Far, AgentFlags.None,
                AgentState.None, true);
        }

        public void Right() {
            this.mclient.Self.Movement.SendManualUpdate(AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG, this.mclient.Self.Movement.Camera.Position,
                this.mclient.Self.Movement.Camera.AtAxis, this.mclient.Self.Movement.Camera.LeftAxis, this.mclient.Self.Movement.Camera.UpAxis,
                this.mclient.Self.Movement.BodyRotation, this.mclient.Self.Movement.HeadRotation, this.mclient.Self.Movement.Camera.Far, AgentFlags.None,
                AgentState.None, true);
        }

        public void Left() {
            this.mclient.Self.Movement.SendManualUpdate(AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS, this.mclient.Self.Movement.Camera.Position,
                this.mclient.Self.Movement.Camera.AtAxis, this.mclient.Self.Movement.Camera.LeftAxis, this.mclient.Self.Movement.Camera.UpAxis,
                this.mclient.Self.Movement.BodyRotation, this.mclient.Self.Movement.HeadRotation, this.mclient.Self.Movement.Camera.Far, AgentFlags.None,
                AgentState.None, true);
        }

        public void TurnTo(double x, double y, double z) {
            Vector3 newDirection;
            newDirection.X = (float)x;
            newDirection.Y = (float)y;
            newDirection.Z = (float)z;
            this.mclient.Self.Movement.TurnToward(newDirection);
            this.mclient.Self.Movement.SendUpdate(false);
        }

    }
}