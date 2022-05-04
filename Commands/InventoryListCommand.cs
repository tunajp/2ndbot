using OpenMetaverse;
using System.Text;

namespace SecondBot.Client {
    public class InventoryListCommand : Command {

        private Inventory Inventory;
        private InventoryManager Manager;

        public InventoryListCommand(MyClient mclient) {
            this.mclient = mclient;
        }
//        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
//            Manager = this.mclient.Inventory;
//            Inventory = Manager.Store;
//
//            StringBuilder result = new StringBuilder();
//
//            InventoryFolder rootFolder = Inventory.RootFolder;
//            PrintFolder(rootFolder, result, 0);
//
//            //return result.ToString();
//            Console.WriteLine(result.ToString());
//        }
//        private void PrintFolder(InventoryFolder f, StringBuilder result, int indent)
//        {
//            List<InventoryBase> contents = Manager.FolderContents(f.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 3000);
//            if (contents != null) {
//                foreach (InventoryBase i in contents) {
//                    result.AppendFormat("{0}{1} ({2})\n", new string(' ', indent * 2), i.Name, i.UUID);
//                    if (i is InventoryFolder folder) {
//                        PrintFolder(folder, result, indent + 1);
//                    }
//                }
//            }
//        }
        public override void Execute(UUID fromUUID, string fromName, string message ,int type) {
            StringBuilder result = new StringBuilder();
            this.DisplayInventories(this.mclient.Inventory.Store.RootFolder, result);
            Console.WriteLine(result.ToString());
        }
        private void DisplayInventories(InventoryBase invBase, StringBuilder result, int indent=0) {
            //List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(invBase.UUID);
            List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(invBase.UUID);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    result.AppendFormat("{0}{1} ({2})\n", new string(' ', indent * 2), i.Name, i.UUID);
                    if (i is InventoryFolder folder) {
                        getItem(i.UUID);
                        DisplayInventories(folder, result, indent + 1);
                    }
                }
            }
        }
        private void getItem(UUID folderUUID) {
            //InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[new UUID(e.Node.Name)];
            InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
            this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);
        }

        public void UpdateFolder(UUID folderID) {
            List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(folderID);
            if (contents != null) {
                foreach (InventoryBase i in contents) {
                    Console.WriteLine(i.Name);
                    //result.AppendFormat("{0}{1} ({2})\n", new string(' ', indent * 2), i.Name, i.UUID);
                    //if (i is InventoryFolder folder) {
                    //    getItem(i.Name);
                    //    DisplayInventories(folder, result, indent + 1);
                    //}
                }
            }

        }
    }
}