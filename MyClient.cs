using OpenMetaverse;

namespace SecondBot.Client {
    public class MyClient : GridClient {

        public Dictionary<UUID, Group>? GroupsCache = null;
        private readonly ManualResetEvent GroupsEvent = new ManualResetEvent(false);

        private GalMoji.Encoder enc;
        public bool galMojiMode;
        public MyClient() {
            this.enc = new GalMoji.Encoder();
            this.galMojiMode = false;
        }

        public void Say(UUID fromUUID, string message, int channel, int type, bool filter = true) {
            if (filter && this.galMojiMode) message = this.GetGalMoji(message);
            if (type == 0) Self.Chat(message, 0, ChatType.Normal);
            else if (type == 1) Self.InstantMessage(fromUUID, message);
        }
        public string GetGalMoji(string message) {
            return this.enc.Encode(message, true);
        }

        public void ReloadGroupsCache() {
            Groups.CurrentGroups += Groups_CurrentGroups;
            Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        void Groups_CurrentGroups(object? sender, CurrentGroupsEventArgs e) {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();
        }
        public UUID GroupName2UUID(String groupName) {
            UUID tryUUID;
            if (UUID.TryParse(groupName,out tryUUID))
                    return tryUUID;
            if (null == GroupsCache) {
                    ReloadGroupsCache();
                if (null == GroupsCache)
                    return UUID.Zero;
            }
            lock(GroupsCache) {
                if (GroupsCache.Count > 0) {
                    foreach (Group currentGroup in GroupsCache.Values)
                        if (String.Equals(currentGroup.Name, groupName, StringComparison.CurrentCultureIgnoreCase))
                            return currentGroup.ID;
                }
            }
            return UUID.Zero;
        }
    }
}