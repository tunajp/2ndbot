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
        openai,
    }
    public class MyApplication {
        MyClient mclient;
        LoginParams ClientLogin;

        List<UUID> currentAnims;
        private System.Timers.Timer updateTimer;
        private string? followTarget;

        private bool randomChat;

        private bool loiter;
        private DateTime lastLoiterDateTime;
        private Vector3 loiterStartRegionPos;
        static public DateTime lastChatDateTime;
        private List<Primitive>? MovementTargetPrims;

        string? activeGroup;

        string nickname;
        string? home;
        string? bed;
        string chatplus_apikey;
        string chatplus_agentname;
        string? mebo_apikey;
        string? mebo_agent_id;
        string? openai_apikey;
        string? script;
        string owner;

        ChatMode chatMode; // 0:指名モード 1:全レス
        ChatApi chatApi; // 0:chatplus 1:mebo(free plan:1000/month) 2:openai
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);

        private ManualResetEvent ItemEvent = new ManualResetEvent(false);

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        private Logger logger;

        private IdleTalkCommand idletalkcommand;
        private StandupCommand standupcommand;
        private TeleportCommand teleportcommand;
        private MoveCommand movecommand;
        private AnimationCommand animationcommand;
        private CreateNotecardCommand createnotecardcommand;
        private InventoryListCommand inventorylistcommand;

        private Microsoft.Scripting.Hosting.ScriptEngine scriptEngine;
        private Microsoft.Scripting.Hosting.ScriptScope scriptScope;

        public MyApplication(string firstname, string lastname, string pass, string start, string nickname, string? home,string? bed, string chatplus_apikey, string chatplus_agentname, string? mebo_apikey, string? mebo_agent_id, string? openai_apikey, string? script, string owner) {

            AssemblyLoadContext.Default.Unloading += MethodInvokedOnSigTerm;

            this.logger = new Logger(firstname + "_" + lastname);

            this.chatMode = ChatMode.Nominate;
            this.chatApi = ChatApi.chatplus;
            this.currentAnims = new List<UUID>();
            this.followTarget = null;
            this.randomChat = true;
            this.loiter = false;
            this.lastLoiterDateTime = DateTime.Now;
            MyApplication.lastChatDateTime = DateTime.Now;

            this.nickname = nickname;
            this.home = home;
            this.bed = bed;
            this.chatplus_apikey = chatplus_apikey;
            this.chatplus_agentname = chatplus_agentname;
            this.mebo_apikey = mebo_apikey;
            this.mebo_agent_id = mebo_agent_id;
            this.openai_apikey = openai_apikey;
            this.script = script;
            this.owner = owner;

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
            this.mclient.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
            this.mclient.Friends.FriendshipOffered += Frind_FrienshipOfferd;
            this.mclient.Objects.ObjectProperties += Objects_OnObjectProperties;
            this.mclient.Groups.GroupInvitation += Group_GroupInvitation;

            // https://github.com/cinderblocks/libremetaverse/blob/master/LibreMetaverse.GUI/AvatarList.cs
            this.mclient.Grid.CoarseLocationUpdate += Grid_CoarseLocationUpdate;
            this.mclient.Network.SimChanged += Network_OnCurrentSimChanged;
            this.mclient.Objects.AvatarUpdate += Objects_OnNewAvatar;
            this.mclient.Objects.TerseObjectUpdate += Objects_OnObjectUpdated;

            string firstName = firstname;
            string lastName = lastname;
            string password = pass;

            this.ClientLogin = mclient.Network.DefaultLoginParams(firstName, lastName, password, "2ndbot", "1.0");
            this.ClientLogin.Start = start;

            this.mclient.Network.BeginLogin(this.ClientLogin);

            updateTimer = new System.Timers.Timer(1000);
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
            updateTimer.Start();

            this.idletalkcommand = new IdleTalkCommand(this.mclient);
            this.standupcommand = new StandupCommand(this.mclient);
            this.teleportcommand = new TeleportCommand(this.mclient);
            this.movecommand = new MoveCommand(this.mclient);
            this.animationcommand = new AnimationCommand(this.mclient);
            this.createnotecardcommand = new CreateNotecardCommand(this.mclient);
            this.inventorylistcommand = new InventoryListCommand(this.mclient);

            this.scriptEngine = IronPython.Hosting.Python.CreateEngine();
            this.scriptScope = scriptEngine.CreateScope();
            this.scriptScope.SetVariable("application", this);
            this.scriptScope.SetVariable("mclient", this.mclient);
            this.scriptScope.SetVariable("idletalkcommand", this.idletalkcommand);
            this.scriptScope.SetVariable("standupcommand", this.standupcommand);
            this.scriptScope.SetVariable("teleportcommand", this.teleportcommand);
            this.scriptScope.SetVariable("movecommand", this.movecommand);
            this.scriptScope.SetVariable("animationcommand", this.animationcommand);
            this.scriptScope.SetVariable("createnotecardcommand", this.createnotecardcommand);
            try {
               this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                dynamic scriptInfo = this.scriptScope.GetVariable(@"scriptInfo");
                var info = scriptInfo();
                Console.WriteLine(info);
            } catch(Exception e) {
               Console.WriteLine(e.Message);
            }
        }

        void MethodInvokedOnSigTerm(AssemblyLoadContext sender) {
            Console.WriteLine("SIGHUP");
        }
        async void Network_OnLogin(object? sender, LoginProgressEventArgs e) {
            String mes = await SecondLifeFeedCommand.feed();
            if (e.Status == LoginStatus.Success) {
                Console.WriteLine("Login success");
                this.loiterStartRegionPos = this.mclient.Self.SimPosition;
                this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic Network_OnLogin = this.scriptScope.GetVariable(@"Network_OnLogin");
                    var info = Network_OnLogin();
                    Console.WriteLine(info);
                } catch(Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (e.Status == LoginStatus.Failed) {
                Console.WriteLine("Login Failed");
                Program.myAppCount -= 1;
                if (Program.myAppCount == 0) {
                    Console.WriteLine(mes);
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
            if (e.Message.Length > 0) this.logger.ChatLog(e.FromName + ":" + e.Message);
            //if (e.Message.Length > 0 && (this.mclient.MasterKey == e.SourceID || (this.mclient.MasterName == e.FromName && !this.mclient.AllowObjectMaster))) {
            //    this.mclient.Self.Chat(e.Message, 0, ChatType.Normal);
            //}
            Console.WriteLine("発言者:" + e.SourceID + " ," + this.nickname + ":" + this.mclient.Self.AgentID);
            if (e.Message.Length > 0 && e.SourceID != this.mclient.Self.AgentID) {
                if (!idletalkcommand.IsJapanese(e.Message) && e.SourceType != ChatSourceType.Agent) {
                    Console.WriteLine("Ignore this as it is not a chat from agent.");
                    return;
                }
                string message = e.Message;
                message = message.Trim();

                Console.WriteLine(message);

                bool continueCommand = true;
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic chatBeforHook = this.scriptScope.GetVariable(@"chatBeforHook");
                    continueCommand = chatBeforHook(e.SourceID, e.FromName, message, 0);
                } catch(Exception ex) {
                    Console.WriteLine(ex.Message);
                }
                if (!continueCommand) {
                    return;
                }

                if (message.Trim().StartsWith(this.nickname)) {
                    message = message.Replace("　", " ");
                    this.command(e.SourceID, e.FromName, message, 0);
                } else {
                    if (this.chatMode == ChatMode.All) {
                        Console.WriteLine(this.mclient.Self.AgentID + " " + e.SourceID);
                        idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id, this.openai_apikey);
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
                    this.loiter = false;
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
                        this.logger.IMLog(e.IM.FromAgentName, e.IM.Message);
                        this.command(e.IM.FromAgentID, e.IM.FromAgentName, message, 1);
                    }
                    break;
                case InstantMessageDialog.GroupInvitation:
                    Console.WriteLine("GroupInvitation:" + e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    break;
                default:
                    Console.WriteLine("ignored:" + e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    break;
            }
        }

        async void command(UUID fromUUID, string fromName, string message ,int type) {
            bool idleTalkExecute = false;
            if (message.Contains("コマンド") || message.Contains("ヘルプ") || message.Contains("一覧")) {
                string commandList = Constants.COMMANDS;
                this.mclient.Say(fromUUID, commandList, 0, type, false);
            } else if(message.Contains("マニュアル")) {
                createnotecardcommand.Execute(fromUUID, fromName, message, type);
            } else if (message.Contains("ギャル文字変換")) {
                var arr1 = message.Split(' ');
                if (arr1.Length == 1) {
                    string mes = "引数を指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                this.mclient.Say(fromUUID, this.mclient.GetGalMoji(arr1[arr1.Length-1]), 0, type);
            } else if (message.Contains("ギャル文字")) {
                string mes = "";
                string arg = message.ToLower();
                if (arg.Contains("on")) {
                    this.mclient.galMojiMode = true;
                    mes = "ギャル文字をONにしました。OFFにするとき「立川君ギャル文字 OFF」と命令してください。";
                } else if (arg.Contains("off")) {
                    this.mclient.galMojiMode = false;
                    mes = "ギャル文字をOFFにしました。ONにするとき「立川君ギャル文字 ON」と命令してください。";
                }
                this.mclient.Say(fromUUID, mes, 0, type);
            } else if (message.Contains("座")) {
                var arr1 = message.Split(' ');
                if (arr1.Length == 1) {
                    string mes = "UUIDを指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

                string target_uuid_string = arr1[arr1.Length-1];
                Console.WriteLine("target:" + target_uuid_string);

                UUID target = new UUID(target_uuid_string);
                this.mclient.Self.RequestSit(target, Vector3.Zero);
                this.mclient.Self.Sit();
            } else if (message.Contains("タッチ")) {
                var arr1 = message.Split(' ');
                if (arr1.Length == 1) {
                    string mes = "UUIDを指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
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
                    } else {
                        string mes = "オブジェクトが見つかりませんでした";
                        this.mclient.Say(fromUUID, mes, 0, type);
                    }
                }
            } else if (message.Contains("休")) {
                if (String.IsNullOrEmpty(this.bed)) {
                    string mes = "ベッドがありません・・・";
                    this.mclient.Say(fromUUID, mes, 0, type);
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
                this.loiter = false;
                this.mclient.Self.AutoPilotCancel();
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

                //followTarget = e.SourceID;
                followTarget = fromName;
                Console.WriteLine(followTarget.ToString());

            } else if (message.Contains("止")) {
                followTarget = null;
                this.loiter = false;
                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);
                this.mclient.Self.AutoPilotCancel();
            } else if (message.Contains("テレポ")) {
                this.loiter = false;
                int index = message.IndexOf("テレポ");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "宛先をsim名/x/y/zで指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }

                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);
                string target = message.Substring(index2+1, message.Length - index2-1);
                Console.WriteLine(target);

                teleportcommand.setTarget(target);
                teleportcommand.Execute(fromUUID, fromName, message, type);

            } else if (message.Contains("終了")) {
                if (fromName != this.owner) {
                    string mes = "Exit order revoked.";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
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
                    this.mclient.Say(fromUUID, mes, 0, type);
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
                this.loiter = false;
                string mes = "お家へ帰りまーす！";
                if (String.IsNullOrEmpty(this.home)) {
                    mes = "お家がありません・・・";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                this.mclient.Say(fromUUID, mes, 0, type);

                standupcommand.setCurrentAnims(this.currentAnims);
                standupcommand.Execute(fromUUID, fromName, message, type);

                //this.mclient.Self.GoHome();
                string target = this.home;
                teleportcommand.setTarget(target);
                teleportcommand.Execute(fromUUID, fromName, message, type);

            //} else if (message.Contains("sethome")) {
            //    this.mclient.Self.SetHome();
            //    string mes = "ここをホームに設定しました";
            //    this.mclient.Say(fromUUID, mes, 0, type);
            } else if (message.Contains("チャットモード")) {
                string mes = "";
                if (message.Contains("指名")) {
                    this.chatMode = ChatMode.Nominate;
                    mes = "チャットモードを指名モードに変更しました。" + this.nickname + "から始まる言葉のみ反応します。";
                } else if (message.Contains("全レス")) {
                    this.chatMode = ChatMode.All;
                    mes = "チャットモードを全レスモードに変更しました。うるさい場合は指名モードにしてください。";
                }
                this.mclient.Say(fromUUID, mes, 0, type);
            } else if (message.Contains("チャットAPI")) {
                string mes = "";
                if (message.Contains("chatplus")) {
                    this.chatApi = ChatApi.chatplus;
                    mes = "チャットAPIをchatplusに変更しました。会話のドッジボールをお楽しみください。";
                } else if (message.Contains("mebo")) {
                    this.chatApi = ChatApi.mebo;
                    mes = "チャットAPIをmeboに変更しました。会話のラリーをお楽しみください。";
                } else if (message.Contains("openai")) {
                    this.chatApi = ChatApi.openai;
                    mes = "チャットAPIをOpenAIに変更しました。イーロン・マスクをお楽しみください。";
                }
                this.mclient.Say(fromUUID, mes, 0, type);
            } else if (message.Contains("どこ")) {
                string mes = "Sim: " + this.mclient.Network.CurrentSim.ToString() + "' Position: " + this.mclient.Self.SimPosition.ToString();
                Vector3 globalPos = this.getCurrentGlobalPosition();
                mes += "\n" + "グローバル座標:" + globalPos.X + "," + globalPos.Y + "." + globalPos.Z;
                this.mclient.Say(fromUUID, mes, 0, type);
            } else if(message.Contains("チャンネルチャット")) {
                string mes = "";
                int index = message.IndexOf("チャンネルチャット");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    mes = "チャンネル番号を指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                int index3 = message.IndexOf(" ", index2+1);
                if (index3 == -1) {
                    mes = "押したいボタンを指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }

                string channel = message.Substring(index2+1, index3 - index2-1); // わからん！スペースで一回配列にしたほうがいいのか？
                string button = message.Substring(index3+1,message.Length - index3-1);
                mes = "チャンネル:" + Int32.Parse(channel).ToString() + ",押したいボタン:" + button;
                this.mclient.Say(fromUUID, mes, 0, type);

                // FIX ME: マイナスチャンネルでの発言が通らない
                this.mclient.Self.Chat(button, Int32.Parse(channel), ChatType.Normal);

                // MEGABOLTの実装
                //this.mclient.Self.ReplyToScriptDialog(ed.Channel, butindex, butlabel, ed.ObjectID);
            } else if (message.Contains("ボタンクリック")) {
                string mes = "";
                int index = message.IndexOf("ボタンクリック");
                int index2 = message.IndexOf(" ", index);
                string arrString = message.Substring(index2+1, message.Length - index2-1);
                string[] arr =  arrString.Split(' ');

                if (arr.Length == 0) {
                    mes = "チャンネル番号を指定してください";
                } else if (arr.Length == 1) {
                    mes = "インデックスを指定してください";
                } else if (arr.Length == 2) {
                    mes = "ボタン名を指定してください";
                } else if (arr.Length == 3) {
                    mes = "オブジェクトIDを指定してください";
                }
                if (mes.Length > 0) {
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                string channel = arr[0];
                string btnIndex = arr[1];
                string btnLabel = arr[2];
                string objectId = arr[3];
                //mes = channel + "," + btnIndex + "," + btnLabel + "," + objectId;
                //this.mclient.Say(fromUUID, mes, 0, type);

                this.mclient.Self.ReplyToScriptDialog(Int32.Parse(channel), Int32.Parse(btnIndex), btnLabel, UUID.Parse(objectId));
            } else if (message.Contains("前へ")) {
                movecommand.Forward();
            } else if (message.Contains("後ろへ")) {
                movecommand.Back();
            } else if (message.Contains("右へ")) {
                movecommand.Right();
            } else if (message.Contains("左へ")) {
                movecommand.Left();
            } else if (message.Contains("うろうろ")) {
                //this.findRandomObject();
                //this.targetPrim = this.getNextPrim();
                this.followTarget = null;
                this.loiterStartRegionPos = this.mclient.Self.SimPosition;
                this.loiter = true;
            } else if (message.Contains("ランダム発言")) {
                string mes = "";
                string arg = message.ToLower();
                if (arg.Contains("on")) {
                    this.randomChat = true;
                    mes = "ランダム発言をONにしました。邪魔なときな「立川君ランダム発言 OFF」と命令してください。";
                } else if (arg.Contains("off")) {
                    this.randomChat = false;
                    mes = "ランダム発言をOFFにしました。寂しいときな「立川君ランダム発言 ON」と命令してください。";
                }
                this.mclient.Say(fromUUID, mes, 0, type);
            } else if (message.Contains("アニメリスト")) {
                animationcommand.list(fromUUID, fromName, message, type);
            } else if (message.Contains("アニメ")) {
                int index = message.IndexOf("アニメ");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "アニメーション名を指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                string animName = message.Substring(index2+1, message.Length - index2-1);
                animationcommand.play(animName);
            } else if (message.Contains("踊")) {
                Random r = new System.Random();
                int next = r.Next(1, 7);
                string danceanim = "DANCE" + next.ToString();
                animationcommand.play(danceanim);
            } else if (message.Contains("Second Life") || message.Contains("SecondLife") || message.Contains("セカンドライフ")) {
                this.mclient.Say(fromUUID, await SecondLifeFeedCommand.feed(), 0, type);
            } else if (message.Contains("アイテムリスト")) {
                UUID folderUUID = UUID.Zero;
                List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(this.mclient.Inventory.Store.RootFolder.UUID);
                if (contents != null) {
                    foreach (InventoryBase i in contents) {
                        if (i.Name == "Objects") {
                            folderUUID = i.UUID;
                            break;
                        }
                        //if (i is InventoryFolder folder) {
                        //    // TODO: Nest
                        //}
                    }
                } else {
                    Console.WriteLine("Objects folder not found");
                    return;
                }

                InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
                this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

                ItemEvent.WaitOne(30000, false);
                List<InventoryItem> items = new List<InventoryItem>();
                contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
                if (contents == null) {
                    Console.WriteLine("Failed to get contents of " + "Objects");
                    return;
                }
                string mes = "";
                foreach (InventoryBase item in contents)
                {
                    mes += item.Name + ",\n";
                }
                this.mclient.Say(fromUUID, mes, 0, type);

            } else if (message.Contains("attach")) {
                if (fromName != this.owner) {
                    string mes = "attach order revoked." + fromName + "," + this.owner;
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                int index = message.IndexOf("attach");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "アイテム名称を指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                string itemName = message.Substring(index2+1, message.Length - index2-1);
                Console.WriteLine(itemName);

                UUID folderUUID = UUID.Zero;
                List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(this.mclient.Inventory.Store.RootFolder.UUID);
                if (contents != null) {
                    foreach (InventoryBase i in contents) {
                        if (i.Name == "Objects") {
                            folderUUID = i.UUID;
                            break;
                        }
                        //if (i is InventoryFolder folder) {
                        //    // TODO: Nest
                        //}
                    }
                } else {
                    Console.WriteLine("Objects folder not found");
                    return;
                }

                //UUID folderUUID = new UUID("d5131324-0362-4556-992e-65a912d515dd"); // Objects
                InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
                this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

                ItemEvent.WaitOne(30000, false);
                //UUID itemUUid = new UUID("0ba3cfeb-7417-7be4-0df9-1288625d5470"); // Fairy Wings I SUPPORT UKRAINE v11
                ////FIX ME: item null
                //InventoryItem item = this.mclient.Inventory.FetchItem(itemUUid, this.mclient.Self.AgentID, 1000);
                //if (item == null) {
                //    Console.WriteLine("attach item is null");
                //    return;
                //}
                ////InventoryItem item = (InventoryItem)this.mclient.Inventory.Store[itemUUid];
                /*List<InventoryBase> */contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
                List<InventoryItem> items = new List<InventoryItem>();
                if (contents == null) {
                    Console.WriteLine("Failed to get contents of " + "Objects");
                    return;
                }
                foreach (InventoryBase item in contents)
                {
                    if (item is InventoryItem inventoryItem) {
                        items.Add(inventoryItem);
                        if (inventoryItem.Name.Contains(itemName)) {
                            this.mclient.Appearance.Attach(inventoryItem, AttachmentPoint.Default);
                            Console.WriteLine(inventoryItem.Name + "," + inventoryItem.UUID.ToString());
                            break;
                        }
                    }
                }
            } else if (message.Contains("detach")) {
                int index = message.IndexOf("detach");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "アイテム名称を指定してください";
                    this.mclient.Say(fromUUID, mes, 0, type);
                    return;
                }
                string itemName = message.Substring(index2+1, message.Length - index2-1);
                Console.WriteLine(itemName);

                UUID folderUUID = UUID.Zero;
                List<InventoryBase> contents = this.mclient.Inventory.Store.GetContents(this.mclient.Inventory.Store.RootFolder.UUID);
                if (contents != null) {
                    foreach (InventoryBase i in contents) {
                        if (i.Name == "Objects") {
                            folderUUID = i.UUID;
                            break;
                        }
                        //if (i is InventoryFolder folder) {
                        //    // TODO: Nest
                        //}
                    }
                } else {
                    Console.WriteLine("Objects folder not found");
                    return;
                }
                //UUID folderUUID = new UUID("d5131324-0362-4556-992e-65a912d515dd"); // Objects
                InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
                this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

                ItemEvent.WaitOne(30000, false);
                //UUID itemUUid = new UUID("0ba3cfeb-7417-7be4-0df9-1288625d5470"); // Fairy Wings I SUPPORT UKRAINE v11
                ////FIX ME: item null
                //InventoryItem item = this.mclient.Inventory.FetchItem(itemUUid, this.mclient.Self.AgentID, 1000);
                ////InventoryItem item = (InventoryItem)this.mclient.Inventory.Store[itemUUid];
                //if (item == null) {
                //    Console.WriteLine("detach item is null");
                //    return;
                //}
                /*List<InventoryBase> */contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
                List<InventoryItem> items = new List<InventoryItem>();
                if (contents == null) {
                    Console.WriteLine("Failed to get contents of " + "Objects");
                    return;
                }
                foreach (InventoryBase item in contents)
                {
                    if (item is InventoryItem inventoryItem) {
                        items.Add(inventoryItem);
                        if (inventoryItem.Name.Contains(itemName)) {
                            this.mclient.Appearance.Detach(inventoryItem);
                            Console.WriteLine(inventoryItem.Name + "," + inventoryItem.UUID.ToString());
                            break;
                        }
                    }
                }
            } else if (message.Contains("インベントリ表示")) {
                this.inventorylistcommand.Execute(fromUUID, fromName, message, type);
            } else if (message.Contains("Appearance")) {
                // CAUTION: 現在実装が不完全です

                //string[] args = new String[]{"Clothing/kani"}; // Clothing/kani
                //string appearance = args.Aggregate(string.Empty, (current, t) => current + (t + " "));
                //appearance = appearance.TrimEnd();
                //// FIX ME: フォルダが見つけられない
                //UUID folder = this.mclient.Inventory.FindObjectByPath(this.mclient.Inventory.Store.RootFolder.UUID, this.mclient.Self.AgentID, appearance, 20 * 1000);
                //if (folder == UUID.Zero) {
                //    Console.WriteLine("Outfit path " + appearance + " not found");
                //    return;
                //}

                // ERROR - Failed to fetch the current agent wearables, cannot safely replace outfit
                // AppearanceManager.cs if (needsCurrentWearables && !GetAgentWearables())
                // kani 0b329337-02d8-0aad-4202-ece611bf500e
                // nuko ef676e23-ea67-0abb-6e67-03e9d499e43a
                UUID folderUUID = new UUID("0b329337-02d8-0aad-4202-ece611bf500e");
                InventoryFolder folder = (InventoryFolder)this.mclient.Inventory.Store[folderUUID];
                this.mclient.Inventory.RequestFolderContents(folder.UUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);

                ItemEvent.WaitOne(30000, false);
                List<InventoryBase> contents =  this.mclient.Inventory.FolderContents(folderUUID, this.mclient.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);
                List<InventoryItem> items = new List<InventoryItem>();
                if (contents == null) {
                    Console.WriteLine("Failed to get contents of " + "nuko");
                    return;
                }
                foreach (InventoryBase item in contents)
                {
                    if (item is InventoryItem inventoryItem)
                        items.Add(inventoryItem);
                }
                var lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
                lockSlim.EnterWriteLock();
                this.mclient.Appearance.ReplaceOutfit(items, false);
                lockSlim.ExitWriteLock();
                this.mclient.Appearance.RequestSetAppearance(true);
            } else if (message.Contains("shoot")) {
                this.mclient.Self.Movement.Mouselook = true;
                this.mclient.Self.Movement.MLButtonDown = true;
                this.mclient.Self.Movement.SendUpdate();

                this.mclient.Self.Movement.MLButtonUp = true;
                this.mclient.Self.Movement.MLButtonDown = false;
                this.mclient.Self.Movement.FinishAnim = true;
                this.mclient.Self.Movement.SendUpdate();

                this.mclient.Self.Movement.Mouselook = false;
                this.mclient.Self.Movement.MLButtonUp = false;
                this.mclient.Self.Movement.FinishAnim = false;
                this.mclient.Self.Movement.SendUpdate();
            } else if (message.Contains("デバッグ")) {
            } else {
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic scommand = this.scriptScope.GetVariable(@"command");
                    idleTalkExecute = scommand(fromUUID, fromName, message, type);
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
            if(idleTalkExecute) {
                idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id, this.openai_apikey);
                idletalkcommand.Execute(fromUUID, fromName, message, type);
            }

        }

        private Primitive? getNextPrim() {
            if (this.MovementTargetPrims == null) return null;
            int primsCount = MovementTargetPrims.Count;
            Random r = new System.Random();
            int next = r.Next(0, primsCount);
            int i=0;
            foreach (Primitive p in MovementTargetPrims)
            {
                if (i == next) {
                    //target
                    Console.WriteLine(p.Properties.Name);
                    return p;
                }
                i++;
            }
            return null;
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
            if (this.randomChat) {
                TimeSpan d = DateTime.Now - MyApplication.lastChatDateTime;
                if (d.TotalSeconds > Constants.RANDOM_CHAT_TIMER) {
                    Console.WriteLine("ランダムな発言");
                    Random r = new System.Random();
                    int max = 3; // 0-2
                    if (this.chatApi == ChatApi.openai) {
                        max = 2; // 0-1
                    }
                    int nextAction = r.Next(0, max);
                    if (nextAction == 0) {
                        int ramdomGestureNum = r.Next(0, 10); // 0-9
                        try {
                            this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                            dynamic randomGesture = this.scriptScope.GetVariable(@"randomGesture");
                            randomGesture(ramdomGestureNum);
                            MyApplication.lastChatDateTime = DateTime.Now;
                        } catch(Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    } else if (nextAction == 1) {
                        idletalkcommand.rondomMessage();
                    } else {
                        idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id, this.openai_apikey);
                        idletalkcommand.Execute(UUID.Zero, "", Constants.RANDOM_CHAT_SEED_MESSAGE, 0);
                    }
                }
            }

            if (this.loiter == true) {

                TimeSpan d = DateTime.Now - this.lastLoiterDateTime;
                if (d.TotalSeconds > Constants.LOITER_TIMER) {
                    Random r = new System.Random();
                    int nextAction = r.Next(0, 3); // 0-2
                    standupcommand.setCurrentAnims(this.currentAnims);
                    standupcommand.Execute(UUID.Zero, "", "", 0);

                    if (nextAction != 0) {
                        Console.WriteLine("開始場所:" + this.loiterStartRegionPos.X + "," + this.loiterStartRegionPos.Y);
                        int targetX = r.Next(Constants.LOITER_RADIUS*-1, Constants.LOITER_RADIUS);
                        int targetY = r.Next(Constants.LOITER_RADIUS*-1, Constants.LOITER_RADIUS);

                        Console.WriteLine("ターゲット位置:" + (this.loiterStartRegionPos.X + targetX) + "," + (this.loiterStartRegionPos.Y + targetY));

                        Vector3 newDirection;
                        newDirection.X = (float)this.loiterStartRegionPos.X + targetX;
                        newDirection.Y = (float)this.loiterStartRegionPos.Y + targetY;
                        newDirection.Z = (float)this.loiterStartRegionPos.Z;
                        this.mclient.Self.Movement.TurnToward(newDirection);
                        this.mclient.Self.Movement.SendUpdate(false);

                        uint regionX, regionY;
                        foreach(var t in this.mclient.Network.Simulators) {
                            Utils.LongToUInts(t.Handle, out regionX, out regionY);
                            double xTarget = (double)this.loiterStartRegionPos.X + targetX + (double)regionX;
                            double yTarget = (double)this.loiterStartRegionPos.Y + targetY + (double)regionY;
                            double zTarget = this.loiterStartRegionPos.Z - 2f;
                            this.mclient.Self.AutoPilot(xTarget, yTarget, zTarget);
                            break;
                        }
                    } else {
                        this.findRandomObject();
                        Primitive? targetPrim = this.getNextPrim();
                        if (targetPrim != null) {
                            Console.WriteLine("Sit->" + targetPrim.Properties.Name);
                            //this.mclient.Self.Touch(targetPrim.LocalID);
                            this.mclient.Self.RequestSit(targetPrim.ID, Vector3.Zero);
                            this.mclient.Self.Sit();
                        }

                    }

                    this.lastLoiterDateTime = DateTime.Now;
                }


            }
            try {
                this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                dynamic updateTimer_Elapsed = this.scriptScope.GetVariable(@"updateTimer_Elapsed");
                var info = updateTimer_Elapsed();
                //Console.WriteLine(info);
            } catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }

        private void findRandomObject() {
            float radius = 5;
            Vector3 location = this.mclient.Self.SimPosition;
            List<Primitive> prims = this.mclient.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                }
            );

            // *** request properties of these objects ***
            bool complete = RequestObjectProperties(prims, 250);

            this.MovementTargetPrims = new List<Primitive>(prims);

        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }

            this.mclient.Objects.SelectObjects(this.mclient.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }

        void Objects_OnObjectProperties(object? sender, ObjectPropertiesEventArgs e)
        {
            lock (PrimsWaiting)
            {
                Primitive? prim;
                if (PrimsWaiting.TryGetValue(e.Properties.ObjectID, out prim))
                {
                    prim.Properties = e.Properties;
                }
                PrimsWaiting.Remove(e.Properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }

        void Network_OnCurrentSimChanged(object? sender, SimChangedEventArgs e) {
            Console.WriteLine("OnCurrentSimChanged:" + e.ToString());
        }
        void Grid_CoarseLocationUpdate(object? sender, CoarseLocationUpdateEventArgs e) {
            List<UUID> newEntries = e.NewEntries;
            foreach(UUID t in newEntries) {
                Console.WriteLine("Grid_CoarseLocationUpdate:" + e.Simulator.Name + "," + t.ToString());
            }
        }
        void Objects_OnNewAvatar(object? sender, AvatarUpdateEventArgs e)
        {
            //Console.WriteLine("OnNewAvatar:" + e.Avatar.Name);
        }

        void Objects_OnObjectUpdated(object? sender, TerseObjectUpdateEventArgs e)
        {
            if (e.Update.Avatar) {
                Avatar av;
                e.Simulator.ObjectsAvatars.TryGetValue(e.Update.LocalID, out av);
                //Console.WriteLine("OnObjectUpdated:" + av.Name);
            }
        }

        public static Vector3? getTargetPos(MyClient client,string targetName) {
            if (targetName != null) {
                lock(client.Network.Simulators) {
                    //Console.WriteLine(followTarget);
                    foreach(var t in client.Network.Simulators) {
                        Avatar targetAv = t.ObjectsAvatars.Find(avatar => avatar.Name == targetName);
                        if (targetAv != null) {
                            return targetAv.Position;
                        }
                    }
                    return null;
                }
            } else {
                return null;
            }
        }
        private void follow() {
            if (followTarget != null) {
                lock(this.mclient.Network.Simulators) {
                    //Console.WriteLine(followTarget);
                    foreach(var t in this.mclient.Network.Simulators) {
                        //Console.WriteLine(t.Name); // MEMO:Simulatorsは現在いるリージョンを含め十字に5個のリージョンが返ってくる
                        Avatar targetAv = t.ObjectsAvatars.Find(avatar => avatar.Name == followTarget);

                        //Avatar targetAv;
                        ////t.ObjectAvatarsはUUIDじゃないのか・・・
                        //if (t.ObjectsAvatars.TryGetValue(followTarget, out targetAv)) {
                        //}

                        // MEMO:追いかけてる途中でリージョンをまたいた場合と、別のリージョンから呼びかけた場合で挙動が異なる
                        // 追いかけてる途中でリージョンをまたぐと、ターゲットがすべてのSimulatarsから全く見つからなくなる
                        // おそらくアバターのアップデート情報を取得する必要があると思われる
                        // 別のリージョンから呼びかけた場合、別のSimulatorでターゲットアバターが見つかるので、
                        // FIXME: Calculate global distancesを対処すればいいと思われる
                        // 全く別の問題として、アバターが見えない区画設定になっている場所へ移動するとターゲットアバターが見つからなくなる問題があるが、解決方法は多分ない
                        //if (targetAv == null) {
                        //    Console.WriteLine("follow missing:" + targetAv + " in " + t.Name);
                        //}
                        if (targetAv != null) {
                            //Console.WriteLine("Target:" + (double)targetAv.Position.X + "," + (double)targetAv.Position.Y); // 231.68441772460938,118.41368103027344

                            float DISTANCE_BUFFER = 1.0f;
                            float distance = 0.0f;
                            if (t == this.mclient.Network.CurrentSim) {
                                distance = Vector3.Distance(targetAv.Position, mclient.Self.SimPosition);
                                //Console.WriteLine(distance.ToString());
                            } else {
                                // FIXME: Calculate global distances
                                Console.WriteLine("target->" + t.Name + "," + "current->" + this.mclient.Network.CurrentSim.Name);
                            }

                            if (distance > DISTANCE_BUFFER) {
                                uint regionX, regionY;
                                Utils.LongToUInts(t.Handle, out regionX, out regionY);

                                double xTarget = (double)targetAv.Position.X + (double)regionX;
                                double yTarget = (double)targetAv.Position.Y + (double)regionY;
                                double zTarget = targetAv.Position.Z - 2f;
                                //Console.WriteLine("Region:" + regionX + "," + regionY); // 263680,248064 多分そのリージョンの0,0座標
                                //Console.WriteLine("Target:" + (double)targetAv.Position.X + "," + (double)targetAv.Position.Y); // 231.68441772460938,118.41368103027344
                                //Console.WriteLine("AutoPilot:" + xTarget + "," + yTarget);

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
            string mes = "インデックス:ボタン名: ";
            int i=0;
            foreach(var label in e.ButtonLabels) {
                mes += i.ToString() + ":" + label + ",";
                i++;
            }
            this.mclient.Self.Chat(mes, 0, ChatType.Normal);
            //this.mclient.Self.Chat("チャンネルチャット チャンネル番号 ボタン名称", 0, ChatType.Normal);
            this.mclient.Self.Chat("ボタンクリック チャンネル番号 インデックス ボタン名 オブジェクトID", 0, ChatType.Normal);
        }

        void Inventory_InventoryObjectOfferd(object? sender, InventoryObjectOfferedEventArgs e) {
            Console.WriteLine("Accepting InventoryOffer" + e.Offer.FromAgentName + " " + e.Offer.Message);
            e.Accept = true;
        }

        void Inventory_OnFolderUpdated(object? sender, FolderUpdatedEventArgs e) {
            ItemEvent.Set();
            this.inventorylistcommand.UpdateFolder(e.FolderID);
        }
        void Frind_FrienshipOfferd(object? sender, FriendshipOfferedEventArgs e) {
            Console.WriteLine("Accepting Friendship:" + e.AgentName);
            this.mclient.Friends.AcceptFriendship(e.AgentID, e.SessionID);
        }

        void Group_GroupInvitation(object?sender, GroupInvitationEventArgs e) {
            Console.WriteLine("GroupInvitation:" + e.FromName + "," + e.Message);
            // TODO:グループ招待をどうするかOwnerにIMを送信して返事で決めさせる
            //e.Accept = true;
            e.Accept = false;
        }

        public Vector3 getCurrentGlobalPosition() {
            Vector3 pos = this.mclient.Self.SimPosition;
            uint regionX, regionY;
            foreach(var t in this.mclient.Network.Simulators) {
                Utils.LongToUInts(t.Handle, out regionX, out regionY);
                double xTarget = (double)this.loiterStartRegionPos.X + (double)regionX;
                double yTarget = (double)this.loiterStartRegionPos.Y + (double)regionY;
                double zTarget = this.loiterStartRegionPos.Z - 2f;
                this.mclient.Self.AutoPilot(xTarget, yTarget, zTarget);
                return new Vector3((float)xTarget, (float)yTarget, (float)zTarget);
            }
            return Vector3.Zero;
        }

    }
}