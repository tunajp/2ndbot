using OpenMetaverse;
using System.Text;

namespace SecondBot.Client {
    public class InventoryListCommand : Command {

        private Inventory Inventory;
        private InventoryManager Manager;

        public InventoryListCommand(MyClient mclient) {
            this.mclient = mclient;
        }
        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
            Manager = this.mclient.Inventory;
            Inventory = Manager.Store;

            StringBuilder result = new StringBuilder();

            InventoryFolder rootFolder = Inventory.RootFolder;
            PrintFolder(rootFolder, result, 0);

            //return result.ToString();
            Console.WriteLine(result.ToString());
        }
        private void PrintFolder(InventoryFolder f, StringBuilder result, int indent)
        {
            List<InventoryBase> contents = Manager.FolderContents(f.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 3000);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    result.AppendFormat("{0}{1} ({2})\n", new string(' ', indent * 2), i.Name, i.UUID);
                    if (i is InventoryFolder folder) {
                        PrintFolder(folder, result, indent + 1);
                    }
                }
            }
        }
    }
}