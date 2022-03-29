using System.Runtime.Loader;

using OpenMetaverse;
using OpenMetaverse.Packets;

namespace SecondBot.Client {
    enum ChatMode {
        Nominate = 0,
        All,
    }
    public enum ChatApi {
        chatplus = 0,
        mebo,
    }
    public class MyApplication {
        MyClient mclient;
        LoginParams ClientLogin;

        List<UUID> currentAnims;
        private System.Timers.Timer updateTimer;
        private string? followTarget;

        string? activeGroup;

        string nickname;
        string? home;
        string? bed;
        string chatplus_apikey;
        string chatplus_agentname;
        string? mebo_apikey;
        string? mebo_agent_id;

        ChatMode chatMode; // 0:指名モード 1:全レス
        ChatApi chatApi; // 0:chatplus 1:mebo(free plan:1000/month)
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);

        private IdleTalkCommand idletalkcommand;
        private StandupCommand standupcommand;
        private TeleportCommand teleportcommand;

        public MyApplication(string firstname, string lastname, string pass, string start, string nickname, string? home,string? bed, string chatplus_apikey, string chatplus_agentname, string? mebo_apikey, string? mebo_agent_id) {

            AssemblyLoadContext.Default.Unloading += MethodInvokedOnSigTerm;

            this.chatMode = ChatMode.Nominate; // デフォルトは指名モード
            this.chatApi = ChatApi.chatplus; // デフォルトはchatplus
            this.currentAnims = new List<UUID>();
            this.followTarget = null;

            this.nickname = nickname;
            this.home = home;
            this.bed = bed;
            this.chatplus_apikey = chatplus_apikey;
            this.chatplus_agentname = chatplus_agentname;
            this.mebo_apikey = mebo_apikey;
            this.mebo_agent_id = mebo_agent_id;

            this.mclient = new MyClient();
            this.mclient.Settings.USE_LLSD_LOGIN = true; // LLSD or XML-RPC(古い形式)
            this.mclient.Settings.USE_ASSET_CACHE = true;

            this.mclient.Network.LoginProgress += Network_OnLogin;
            this.mclient.Network.SimConnected += Network_OnSimConnected;
            this.mclient.Network.Disconnected += Network_OnDisconnected;

            this.mclient.Self.ChatFromSimulator += Self_ChatFromSimulator;
            this.mclient.Self.IM += Self_IM;
            this.mclient.Self.AnimationsChanged += Self_AnimationsChanged;
            this.mclient.Self.ScriptDialog += Self_ScriptDialog;

            this.mclient.Inventory.InventoryObjectOffered += Inventory_InventoryObjectOfferd;
            this.mclient.Friends.FriendshipOffered += Frind_FrienshipOfferd;

            string firstName = firstname;
            string lastName = lastname;
            string password = pass;

            this.ClientLogin = mclient.Network.DefaultLoginParams(firstName, lastName, password, "2ndbot", "1.0");
            this.ClientLogin.Start = start;

            this.mclient.Network.BeginLogin(this.ClientLogin);

            updateTimer = new System.Timers.Timer(500);
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
            updateTimer.Start();

            this.idletalkcommand = new IdleTalkCommand(this.mclient);
            this.standupcommand = new StandupCommand(this.mclient);
            this.teleportcommand = new TeleportCommand(this.mclient);
        }

        void MethodInvokedOnSigTerm(AssemblyLoadContext sender) {
            Console.WriteLine("SIGHUP");
        }
        void Network_OnLogin(object? sender, LoginProgressEventArgs e) {
            if (e.Status == LoginStatus.Success) {
                Console.WriteLine("Login success");
            }
            else if (e.Status == LoginStatus.Failed) {
                Console.WriteLine("Login Failed");
                Program.myAppCount -= 1;
                if (Program.myAppCount == 0) {
                    Environment.Exit(0);
                }
                return;
            }
        }
        void Network_OnSimConnected(object? sender, SimConnectedEventArgs e) {
            Console.WriteLine("Connected");
            this.mclient.Self.RequestBalance();
            //this.mclient.Appearance.SetPreviousAppearance();
            //this.mclient.Groups.ActivateTitle()
        }
        void Network_OnDisconnected(object? sender, DisconnectedEventArgs e) {
            Console.WriteLine("Disconnected");
            Program.myAppCount -= 1;
            Console.WriteLine("残り:" + Program.myAppCount);
            if (Program.myAppCount == 0) {
                Environment.Exit(0);
            }
        }

        void Self_ChatFromSimulator(object? sender, ChatEventArgs e) {
            //if (e.Message.Length > 0 && (this.mclient.MasterKey == e.SourceID || (this.mclient.MasterName == e.FromName && !this.mclient.AllowObjectMaster))) {
            //    this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
            //}
            Console.WriteLine("発言者:" + e.SourceID + " ," + this.nickname + ":" + this.mclient.Self.AgentID);
            if (e.Message.Length > 0 && e.SourceID != this.mclient.Self.AgentID) {
                string message = e.Message;
                message = message.Trim();

                Console.WriteLine(message);

                if (message.Trim().StartsWith(this.nickname)) {
                    message = message.Replace("　", " ");
                    this.command(e.SourceID, e.FromName, message, 0);
                } else {
                    if (this.chatMode == ChatMode.All) {
                        Console.WriteLine(this.mclient.Self.AgentID + " " + e.SourceID);
                        idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id);
                        idletalkcommand.Execute(e.SourceID, e.FromName, message, 0);
                    }
                }
            }
        }

        void Self_IM(object? sender, InstantMessageEventArgs e) {
            string message = e.IM.Message.Replace("　", " ");
            message = message.Trim();

            switch (e.IM.Dialog) {
                case InstantMessageDialog.RequestTeleport:
                    Console.WriteLine("Accepting teleport lure:" + e.IM.FromAgentName);
                    standupcommand.setCurrentAnims(this.currentAnims);
                    standupcommand.Execute(e.IM.FromAgentID, e.IM.FromAgentName, message, 1);

                    this.mclient.Self.TeleportLureRespond(e.IM.FromAgentID, e.IM.IMSessionID, true);
                    break;
                case InstantMessageDialog.FriendshipOffered:
                    Console.WriteLine("FriendshipOffered");
                    //this.mclient.Friends.AcceptFriendship(e.IM.FromAgentID, e.IM.IMSessionID);
                    break;
                case InstantMessageDialog.MessageFromAgent:
                    Console.WriteLine(e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    if (e.IM.Message.Trim().Length > 0 && !e.IM.Message.Contains("typing")) {
                        this.command(e.IM.FromAgentID, e.IM.FromAgentName, message, 1);
                    }
                    break;
                default:
                    Console.WriteLine("ignored:" + e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    break;
            }
        }

        void command(UUID fromUUID, string fromName, string message ,int type) {
            if (message.Contains("コマンド") || message.Contains("ヘルプ") || message.Contains("一覧")) {
                string commandList = "座 uuid,休,立ち,おいで,止,テレポ sim/x/y/z,終了,グループ groupname,帰/戻,チャットモード 指名or全レス,チャットAPI chatplus or mebo,どこ,タッチ uuid";
                if (type == 0) this.mclient.Self.Chat(commandList, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, commandList);

            } else if(message.Contains("座")) {
                var arr1 = message.Split(' ');
                if (arr1.Length == 1) {
                    string mes = "UUIDを指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

                string target_uuid_string = arr1[arr1.Length-1];
                Console.WriteLine("target:" + target_uuid_string);

                UUID target = new UUID(target_uuid_string);
                this.mclient.Self.RequestSit(target, Vector3.Zero);
                this.mclient.Self.Sit();
            } else if(message.Contains("タッチ")) {
                var arr1 = message.Split(' ');
                if (arr1.Length == 1) {
                    string mes = "UUIDを指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }

                string target_uuid_string = arr1[arr1.Length-1];
                UUID target;
                if (UUID.TryParse(target_uuid_string, out target)) {
                    Primitive targetPrim = this.mclient.Network.CurrentSim.ObjectsPrimitives.Find(
                        prim => prim.ID == target
                    );

                    if (targetPrim != null) {
                        this.mclient.Self.Touch(targetPrim.LocalID);
                    }
                }
            } else if (message.Contains("休")) {
                if (String.IsNullOrEmpty(this.bed)) {
                    string mes = "ベッドがありません・・・";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }
                string target_uuid_string = this.bed;
                UUID target = new UUID(target_uuid_string);
                this.mclient.Self.RequestSit(target, Vector3.Zero);
                this.mclient.Self.Sit();
            } else if (message.Contains("立ち") || message.Contains("立って")) {
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

            } else if (message.Contains("おいで") || message.Contains("来")) {
                this.mclient.Self.AutoPilotCancel();
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

                //followTarget = e.SourceID;
                followTarget = fromName;
                Console.WriteLine(followTarget.ToString());

            } else if (message.Contains("止")) {
                followTarget = null;
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);
                this.mclient.Self.AutoPilotCancel();
            } else if (message.Contains("テレポ")) {
                int index = message.IndexOf("テレポ");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "宛先をsim名/x/y/zで指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }

                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);
                string target = message.Substring(index2+1, message.Length - index2-1);
                Console.WriteLine(target);

                teleportcommand.setTarget(target);
                teleportcommand.Execute(fromUUID, fromName, message, type);

            } else if (message.Contains("終了")) {
                this.mclient.Network.Logout();
            } else if (message.Contains("グループ")) {
                // グループタグに関係しそうなとこ
                // https://github.com/cinderblocks/libremetaverse/blob/e26ae695fed27e43a510a85d786abbac2552d051/Programs/examples/TestClient/Commands/Groups/GroupRolesCommand.cs
                // https://github.com/cinderblocks/libremetaverse/blob/e26ae695fed27e43a510a85d786abbac2552d051/Programs/examples/TestClient/Commands/Groups/ActivateGroupCommand.cs
                //string groupName = "泪橋";

                int index = message.IndexOf("グループ");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "グループ名を指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }
                string groupName = message.Substring(index2+1, message.Length - index2-1);
                Console.WriteLine(groupName);

                UUID groupUUID = this.mclient.GroupName2UUID(groupName);
                Console.WriteLine(groupUUID);
                if (UUID.Zero != groupUUID) {
                    EventHandler<PacketReceivedEventArgs> pcallback = AgentDataUpdateHandler;
                    this.mclient.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);

                    Console.WriteLine("setting " + groupName + " as active group");
                    this.mclient.Groups.ActivateGroup(groupUUID);
                    GroupsEvent.WaitOne(30000, false);

                    this.mclient.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
                    GroupsEvent.Reset();

                    if (String.IsNullOrEmpty(activeGroup))
                    Console.WriteLine(this.mclient.ToString() + " failed to activate the group " + groupName);
                }
            } else if (message.Contains("帰") || message.Contains("戻")) {
                string mes = "お家へ帰りまーす！";
                if (String.IsNullOrEmpty(this.home)) {
                    mes = "お家がありません・・・";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);

                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

                //this.mclient.Self.GoHome();
                string target = this.home;
                teleportcommand.setTarget(target);
                teleportcommand.Execute(fromUUID, fromName, message, type);

            //} else if (message.Contains("sethome")) {
            //    this.mclient.Self.SetHome();
            //    string mes = "ここをホームに設定しました";
            //    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
            //    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
            } else if (message.Contains("チャットモード")) {
                string mes = "";
                if (message.Contains("指名")) {
                    this.chatMode = ChatMode.Nominate;
                    mes = "チャットモードを指名モードに変更しました。" + this.nickname + "から始まる言葉のみ反応します。";
                } else if (message.Contains("全レス")) {
                    this.chatMode = ChatMode.All;
                    mes = "チャットモードを全レスモードに変更しました。うるさい場合は指名モードにしてください。";
                }
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
            } else if (message.Contains("チャットAPI")) {
                string mes = "";
                if (message.Contains("chatplus")) {
                    this.chatApi = ChatApi.chatplus;
                    mes = "チャットAPIをchatplusに変更しました。会話のドッジボールをお楽しみください。";
                } else if (message.Contains("mebo")) {
                    this.chatApi = ChatApi.mebo;
                    mes = "チャットAPIをmeboに変更しました。会話のラリーをお楽しみください。";
                }
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
            } else if (message.Contains("どこ")) {
                string mes = "Sim: " + this.mclient.Network.CurrentSim.ToString() + "' Position: " + this.mclient.Self.SimPosition.ToString();;
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
            } else if(message.Contains("チャンネルチャット")) {
                string mes = "";
                int index = message.IndexOf("チャンネルチャット");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    mes = "チャンネル番号を指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }
                int index3 = message.IndexOf(" ", index2+1);
                if (index3 == -1) {
                    mes = "押したいボタンを指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }

                string channel = message.Substring(index2+1, index3 - index2-1); // わからん！スペースで一回配列にしたほうがいいのか？
                string button = message.Substring(index3+1,message.Length - index3-1);
                mes = "チャンネル:" + Int32.Parse(channel).ToString() + ",押したいボタン:" + button;
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);

                // FIX ME: マイナスチャンネルでの発言が通らない
                this.mclient.Self.Chat(button, Int32.Parse(channel), ChatType.Normal);

                // MEGABOLTの実装
                //this.mclient.Self.ReplyToScriptDialog(ed.Channel, butindex, butlabel, ed.ObjectID);
            } else if (message.Contains("デバッグ")) {
            } else {
                idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id);
                idletalkcommand.Execute(fromUUID, fromName, message, type);
            }

        }

        void Self_AnimationsChanged(object? sender, AnimationsChangedEventArgs e) {
            var anims = e.Animations;
            //Console.WriteLine(anims.Count);

            // get current Animations UUID
            this.currentAnims.Clear();
            anims.ForEach(delegate(UUID anim){
                //Console.WriteLine(anim);
                this.currentAnims.Add(anim);
            });
        }
        private void updateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) {
            //foreach (Command c in Commands.Values)
            //    if (c.Active)
            //        c.Think();
            this.follow();
        }

        private void follow() {
            if (followTarget != null) {
                lock(this.mclient.Network.Simulators) {
                    //Console.WriteLine(followTarget);
                    foreach(var t in this.mclient.Network.Simulators) {
                        Avatar targetAv = t.ObjectsAvatars.Find(avatar => avatar.Name == followTarget);

                        //Avatar targetAv;
                        ////t.ObjectAvatarsはUUIDじゃないのか・・・
                        //if (t.ObjectsAvatars.TryGetValue(followTarget, out targetAv)) {
                        //}
                        if (targetAv != null) {
                            float DISTANCE_BUFFER = 1.0f;
                            float distance = 0.0f;
                            if (t == this.mclient.Network.CurrentSim) {
                                distance = Vector3.Distance(targetAv.Position, mclient.Self.SimPosition);
                            } else {
                                // FIXME: Calculate global distances
                            }

                            if (distance > DISTANCE_BUFFER) {
                                uint regionX, regionY;
                                Utils.LongToUInts(t.Handle, out regionX, out regionY);

                                double xTarget = (double)targetAv.Position.X + (double)regionX;
                                double yTarget = (double)targetAv.Position.Y + (double)regionY;
                                double zTarget = targetAv.Position.Z - 2f;
                                //Console.WriteLine(xTarget + "," + yTarget + "," + zTarget);

                                if (zTarget > 0) this.mclient.Self.AutoPilot(xTarget, yTarget, zTarget);
                            } else {
                                // We are in range of the target and moving, stop moving
                                this.mclient.Self.AutoPilotCancel();
                            }
                        }
                    }
                }
            }
        }

        private void AgentDataUpdateHandler(object? sender, PacketReceivedEventArgs e) {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
            if (p.AgentData.AgentID == this.mclient.Self.AgentID) {
                activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " ( " + Utils.BytesToString(p.AgentData.GroupTitle) + " )";
                GroupsEvent.Set();
            }
        }

        void Self_ScriptDialog(object? sender, ScriptDialogEventArgs e) {
            this.mclient.Self.Chat("チャンネル:" + e.Channel.ToString() + ",オブジェクトID:" + e.ObjectID, 0, ChatType.Normal);
            string mes = "インデックス:ボタン名:";
            foreach(var label in e.ButtonLabels) {
                mes += label + ",";
            }
            this.mclient.Self.Chat(mes, 0, ChatType.Normal);
            this.mclient.Self.Chat("チャンネルチャット チャンネル番号 ボタン名称", 0, ChatType.Normal);

        }

        void Inventory_InventoryObjectOfferd(object? sender, InventoryObjectOfferedEventArgs e) {
            Console.WriteLine("Accepting InventoryOffer" + e.Offer.FromAgentName + " " + e.Offer.Message);
            e.Accept = true;
        }
        void Frind_FrienshipOfferd(object? sender, FriendshipOfferedEventArgs e) {
            Console.WriteLine("Accepting Friendship:" + e.AgentName);
            this.mclient.Friends.AcceptFriendship(e.AgentID, e.SessionID);
        }

    }
}