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
        string? script;

        ChatMode chatMode; // 0:指名モード 1:全レス
        ChatApi chatApi; // 0:chatplus 1:mebo(free plan:1000/month)
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        private IdleTalkCommand idletalkcommand;
        private StandupCommand standupcommand;
        private TeleportCommand teleportcommand;
        private MoveCommand movecommand;
        private AnimationCommand animationcommand;
        private CreateNotecardCommand createnotecardcommand;

        private Microsoft.Scripting.Hosting.ScriptEngine scriptEngine;
        private Microsoft.Scripting.Hosting.ScriptScope scriptScope;

        public MyApplication(string firstname, string lastname, string pass, string start, string nickname, string? home,string? bed, string chatplus_apikey, string chatplus_agentname, string? mebo_apikey, string? mebo_agent_id, string? script) {

            AssemblyLoadContext.Default.Unloading += MethodInvokedOnSigTerm;

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
            this.script = script;

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
            this.mclient.Objects.ObjectProperties += Objects_OnObjectProperties;

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
                        this.command(e.IM.FromAgentID, e.IM.FromAgentName, message, 1);
                    }
                    break;
                default:
                    Console.WriteLine("ignored:" + e.IM.FromAgentID + " " + e.IM.FromAgentName + " " + e.IM.Message);
                    break;
            }
        }

        async void command(UUID fromUUID, string fromName, string message ,int type) {
            if (message.Contains("コマンド") || message.Contains("ヘルプ") || message.Contains("一覧")) {
                string commandList = Constants.COMMANDS;
                if (type == 0) this.mclient.Self.Chat(commandList, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, commandList);
            } else if(message.Contains("マニュアル")) {
                createnotecardcommand.Execute(fromUUID, fromName, message, type);
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
                this.loiter = false;
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
                string mes = "Sim: " + this.mclient.Network.CurrentSim.ToString() + "' Position: " + this.mclient.Self.SimPosition.ToString();
                Vector3 globalPos = this.getCurrentGlobalPosition();
                mes += "\n" + "グローバル座標:" + globalPos.X + "," + globalPos.Y + "." + globalPos.Z;
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
                if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
            } else if (message.Contains("アニメリスト")) {
                animationcommand.list(fromUUID, fromName, message, type);
            } else if (message.Contains("アニメ")) {
                int index = message.IndexOf("アニメ");
                int index2 = message.IndexOf(" ", index);
                if (index2 == -1) {
                    string mes = "アニメーション名を指定してください";
                    if (type == 0) this.mclient.Self.Chat(mes, 0, ChatType.Normal);
                    else if (type == 1) this.mclient.Self.InstantMessage(fromUUID, mes);
                    return;
                }
                string animName = message.Substring(index2+1, message.Length - index2-1);
                animationcommand.play(animName);
            } else if (message.Contains("踊")) {
                Random r = new System.Random();
                int next = r.Next(1, 7);
                string danceanim = "DANCE" + next.ToString();
                animationcommand.play(danceanim);
            } else if (message.Contains("Second Life") || message.Contains("セカンドライフ")) {
                this.mclient.Self.Chat(await SecondLifeFeedCommand.feed(), 0, ChatType.Normal);
            } else if (message.Contains("スクリプト")) {
                try {
                    this.scriptEngine.ExecuteFile(this.script, this.scriptScope);
                    dynamic scommand = this.scriptScope.GetVariable(@"command");
                    var scommand_ = scommand(fromUUID, fromName, message, type);
                    Console.WriteLine(scommand_);
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                }
            } else if (message.Contains("デバッグ")) {

            } else {
                idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id);
                idletalkcommand.Execute(fromUUID, fromName, message, type);
            }

        }

        private Primitive getNextPrim() {
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
                    int nextAction = r.Next(0, 3); // 0-2
                    if (nextAction == 0) {
                        idletalkcommand.setKeys(this.chatApi, this.chatplus_apikey, this.chatplus_agentname, this.mebo_apikey, this.mebo_agent_id);
                        idletalkcommand.Execute(UUID.Zero, "", Constants.RANDOM_CHAT_SEED_MESSAGE, 0);
                    } else {
                        idletalkcommand.rondomMessage();
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
                        Primitive targetPrim = this.getNextPrim();
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
                Primitive prim;
                if (PrimsWaiting.TryGetValue(e.Properties.ObjectID, out prim))
                {
                    prim.Properties = e.Properties;
                }
                PrimsWaiting.Remove(e.Properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
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