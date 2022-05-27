using OpenMetaverse;
using OpenMetaverse.Assets;

namespace SecondBot.Client {
    public class CreateNotecardCommand : Command {

        public CreateNotecardCommand(MyClient mclient) {
            this.mclient = mclient;
        }

        public override void Execute(UUID fromUUID, string fromName, string message, int type)
        {
            if (!File.Exists(Constants.NOTECARD_TEXT_FILENAME)) {
                    string mes = "Notecardのファイルが見つかりません。";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
            }
            string fileData;
            try {
                fileData = File.ReadAllText(Constants.NOTECARD_TEXT_FILENAME);
            } catch (Exception ex) {
                    string mes = "Notecardのファイル読み込みに失敗しました。" + ex.Message;
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
            }
            AssetNotecard notecard = new AssetNotecard
            {
                BodyText = fileData
            };
            notecard.Encode();

            AutoResetEvent notecardEvent = new AutoResetEvent(false);
            bool finalUploadSuccess = false;
            string notecard_message = "";
            UUID notecardItemID = UUID.Zero, notecardAssetID = UUID.Zero;

            this.mclient.Inventory.RequestCreateItem(this.mclient.Inventory.FindFolderForType(AssetType.Notecard),
                "2ndbotManual", "created by 2ndbot " + DateTime.Now, AssetType.Notecard,
                UUID.Random(), InventoryType.Notecard, PermissionMask.All,
                delegate(bool createSuccess, InventoryItem item) {

                    if (createSuccess) {
                        AutoResetEvent emptyNoteEvent = new AutoResetEvent(false);
                        AssetNotecard empty = new AssetNotecard{
                            BodyText = "\n"
                        };
                        empty.Encode();

                        bool success = false;

                        this.mclient.Inventory.RequestUploadNotecardAsset(empty.AssetData, item.UUID,
                            delegate(bool uploadSuccess, string status, UUID itemID, UUID assetID)
                            {
                                notecardItemID =itemID;
                                notecardAssetID = assetID;
                                success = uploadSuccess;
                                notecard_message = status ?? "Unknown error uploading notecard asset";
                                emptyNoteEvent.Set();
                            }
                        );
                        emptyNoteEvent.WaitOne(Constants.NOTECARD_CREATE_TIMEOUT, false);

                        if (success) {

                            this.mclient.Inventory.RequestUploadNotecardAsset(notecard.AssetData, item.UUID,
                                delegate(bool uploadSuccess, string status, UUID itemID, UUID assetID)
                                {
                                    notecardItemID = itemID;
                                    notecardAssetID = assetID;
                                    finalUploadSuccess = uploadSuccess;
                                    notecard_message = status ?? "Unknown error uploading notecard asset";
                                    notecardEvent.Set();
                                });
                        } else {
                            notecardEvent.Set();
                        }
                    } else {
                        notecard_message = "Notecard item creation failed";
                        notecardEvent.Set();
                    }
                }
            );

            notecardEvent.WaitOne(Constants.NOTECARD_CREATE_TIMEOUT, false);

            if (finalUploadSuccess) {
                string mes = notecardItemID.ToString();
                this.mclient.Say(fromUUID, mes, 0, type);

                this.giveItem(notecardItemID, fromUUID, fromName);
                this.deleteItem(notecardItemID);
            } else {
                string mes = notecard_message;
                this.mclient.Say(fromUUID, mes, 0, type);
                return;
            }
        }

        private void giveItem(UUID uuid, UUID fromUUID, string fromName) {
            InventoryManager Manager = this.mclient.Inventory;
            Manager.GiveItem(uuid, "manual", AssetType.Notecard, fromUUID, true);
        }

        private void deleteItem(UUID uuid) {
            // TODO:消えない・・・
            this.mclient.Inventory.MoveFolder(uuid, this.mclient.Inventory.FindFolderForType(FolderType.Trash));
        }
    }
}