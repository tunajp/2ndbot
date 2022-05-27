using OpenMetaverse;

namespace SecondBot.Client {
    public class AnimationCommand : Command {
        private Dictionary<UUID, string> m_BuiltInAnimations = new Dictionary<UUID, string>(Animations.ToDictionary());

        public AnimationCommand(MyClient mclient) {
            this.mclient = mclient;
        }

        public override void Execute(UUID fromUUID, string fromName, string message, int type)
        {
            throw new NotImplementedException();
        }

        public void list(UUID fromUUID, string fromName, string message, int type) {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            foreach (string key in m_BuiltInAnimations.Values)
            {
                result.AppendLine(key);
            }
            string mes = result.ToString();
            this.mclient.Say(fromUUID, mes, 0, type);

        }

        public void play(string anim) {
            this.stopAnim();
            foreach (var kvp in m_BuiltInAnimations)
            {
                if (kvp.Value.Equals(anim.ToUpper()))
                {
                    this.mclient.Self.AnimationStart(kvp.Key, true);
                    break;
                }
            }
        }

        private void stopAnim() {
            List<UUID> currentAnims = new List<UUID>();
            this.mclient.Self.SignaledAnimations.ForEach(delegate(KeyValuePair<UUID, int> kvp) {
                currentAnims.Add(kvp.Key);
            });
            foreach(UUID anim in currentAnims) {
                this.mclient.Self.AnimationStop(anim, true);
            }

        }
    }
}