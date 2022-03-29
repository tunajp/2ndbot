using OpenMetaverse;

namespace SecondBot.Client {
    public class MyClient : GridClient {

        public Dictionary<UUID, Group>? GroupsCache = null;
        private readonly ManualResetEvent GroupsEvent = new ManualResetEvent(false);

        public MyClient() {

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