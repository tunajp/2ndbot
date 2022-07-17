import clr

clr.AddReference("System")
clr.AddReference("System.Console")
clr.AddReference("System.Threading")
clr.AddReference("System.XML")
from System import *
from System.Console import *
from System.Threading import *
from System.IO import *
from System.Environment import *
from System.Xml import *

clr.AddReference("LibreMetaverse")
clr.AddReference("LibreMetaverse.Types")
from OpenMetaverse import *
from OpenMetaverse import UUID
from OpenMetaverse import Vector3

def scriptInfo():
    return "default.py"

def Network_OnLogin():
    return "Network_OnLogin"

# chatBeforHookはチャットを受信時に他のコマンドの前にフックします
# はい db825529-8850-4616-c824-643faea018ce
# 行ってらっしゃい e37db011-1145-3e98-1cf7-9f307dae03ee
# こんばんわ 96386359-d497-91e2-0761-7a7a5534285e
# くんかくんか 3974dd2d-bce5-23c4-9539-cb3abfce6163
# またね 1268870a-8f68-883e-d80a-984e628ea0fa
# もさもさ 35d735ea-8902-2b4c-46b6-70e27954af7e
# にゃーにゃー 00a5b76e-3a06-f9f5-bbc9-494319ff02b5
# OK e609f94c-ebbe-837e-35d5-cfddf8d39ac9
# おやすみ bdb75763-b7a1-c1aa-f3ac-7351beb91149
# qawsedrftgyhujikolp f364e06e-b6b4-a1f1-b90f-c1d82ffda937
# そうだね 1168242e-6618-8f4d-3ef8-e0f96a19e4ac
# 手遅れだと思います 7e2848ed-607e-ee8d-b886-dfb955aa878a
# 寝たら死ぬぞ 7ce31c3b-437a-cd46-e360-738df8c8e211
# また髪の話してる f28fd398-26ce-5bdc-976d-7e53bd9c478d
# ありまたお 68ca4eb0-4b4d-4dce-39fb-df6f5e08fee9
# いえーい f9023d35-f9ca-10de-f59d-de14d847b698
def chatBeforHook(fromUUID, fromName, message, type):
    if "うさうさ❤" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("35d735ea-8902-2b4c-46b6-70e27954af7e"))
        return False
    elif "もさもさ❤" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("35d735ea-8902-2b4c-46b6-70e27954af7e"))
        return False
    elif "くまくま❤" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("7ce31c3b-437a-cd46-e360-738df8c8e211"))
        return False
    elif "寝ます" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("7ce31c3b-437a-cd46-e360-738df8c8e211"))
        return False
    elif "髪" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("f28fd398-26ce-5bdc-976d-7e53bd9c478d"))
        return False
    elif "Yayyyyyyy" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("f9023d35-f9ca-10de-f59d-de14d847b698"))
        return False
    elif "YAY" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("f9023d35-f9ca-10de-f59d-de14d847b698"))
        return False
    elif "イエェェェェ" in message:
        Thread.Sleep(1000)
        mclient.Self.PlayGesture(UUID("f9023d35-f9ca-10de-f59d-de14d847b698"))
        return False
    else:
        return True
    return True

# chat command extension
def command(fromUUID, fromName, message, type):
    if "テスト" in message:
        mclient.Self.Chat(fromName + " テスト", 0, ChatType.Normal)
        return False
    elif "NPC" in message:
        #pass
        Console.WriteLine("NPC")
        standupcommand.Execute(UUID.Zero, "", "", 0)
        mclient.Self.AutoPilot(263911.29400634766,248179.05200195312,21.72664451599121)
        return False
    elif "グローバル座標" in message:
        pos = application.getCurrentGlobalPosition()
        print(pos)
        return False
    else:
        return True

def openaiPrompt(fromUUID, fromName, message, type):
    prompt = ""

    ## 通常prompt
    #prompt += "以下は、AIアシスタントとの会話です。このアシスタントは、親切で、クリエイティブで、賢くて、とてもフレンドリーです。 \n"
    #prompt += "\n"
    #prompt += "私:Hi\n"
    #prompt += "AI:Hello!\n"
    #prompt += "私:こんにちは、調子はどう？\n"
    #prompt += "AI:元気です\n"
    #prompt += "私:好きだよ\n"
    #prompt += "AI:私も" + fromName + "さんのことが好きです\n"

    ## 一人称と語尾（ボク・・・ござる）prompt
    #prompt += "私:Hi\n"
    #prompt += "AI:Hello!\n"
    #prompt += "私:こんにちは、調子はどう？\n"
    #prompt += "AI:ボクは元気でござる\n"
    #prompt += "私:好きだよ\n"
    #prompt += "AI:ボクも" + fromName + "さんのことが好きでござる\n"

    # 一人称（ボクっ娘） + 英語prompt
    prompt += "以下は、立川くんとの会話です。立川くんは、ボクっ娘でとても親切なAIボットです。 \n"
    prompt += "\n"
    prompt += "私:Hi\n"
    prompt += "立川くん:こんにちわ (Hello!)\n"
    prompt += "私:こんにちは、調子はどう？\n"
    prompt += "立川くん:" + fromName + "さんこんにちは、ボクは元気だよ (I'm fine.)\n"
    prompt += "私:好きだよ\n"
    prompt += "立川くん:ボクも" + fromName + "さんのことが好きだよ (I love you " + fromName + "too !)\n"
    prompt += "私:この服どう思いますか？\n"
    prompt += "立川くん:素敵な服だね！ (What a lovely outfit.)\n"
    prompt += "私:立川君は最近何してるの\n"
    prompt += "立川くん:ボクは最近調子がいいから、たくさん踊ってるよ！ (I don't know.)\n"
    prompt += "私:立川君はAIなの?\n"
    prompt += "立川くん:ボクはインテリジェントなスーパーAIだよ！ (I'm an intelligent super AI.)\n"
    prompt += "私:バナナの平方根は何ですか?\n"
    prompt += "立川くん:ボクにはわかりません (I don't know.)\n"

    ## お嬢様prompt
    #prompt += "以下は、AIアシスタントとの会話です。このアシスタントは、セクシーで、お嬢様で、とても親切です。 \n"
    #prompt += "\n"
    #prompt += "私:Hi\n"
    #prompt += "AI:Hello!\n"
    #prompt += "私:こんにちは、調子はどう？\n"
    #prompt += "AI:" + fromName + "様ごきげんよう、お元気ですわ\n"
    #prompt += "私:好きだよ\n"
    #prompt += "AI:わたくしも" + fromName + "様のことが好きですわ\n"
    #prompt += "私:この服どう思いますか？\n"
    #prompt += "AI:素敵なお召し物ですこと\n"

    ## お嬢様 + 英語prompt
    #prompt += "以下は、AIアシスタントとの会話です。このアシスタントは、セクシーで、お嬢様で、とても親切です。 \n"
    #prompt += "\n"
    #prompt += "私:Hi\n"
    #prompt += "AI:ごきげんよう (Hello!)\n"
    #prompt += "私:こんにちは、調子はどう？\n"
    #prompt += "AI:" + fromName + "様ごきげんよう、お元気ですわ (I'm fine.)\n"
    #prompt += "私:好きだよ\n"
    #prompt += "AI:わたくしも" + fromName + "様のことが好きですわ (I love you " + fromName + "too !)\n"
    #prompt += "私:この服どう思いますか？\n"
    #prompt += "AI:素敵なお召し物ですこと (What a lovely outfit.)\n"

    ## うる星やつらのラムちゃんprompt
    #prompt += "以下は、AIアシスタントとの会話です。このアシスタントは、うる星やつらのラムです。 \n"
    #prompt += "\n"
    #prompt += "私:Hi\n"
    #prompt += "AI:Hello!\n"
    #prompt += "私:こんにちは、調子はどう？\n"
    #prompt += "AI:元気だっちゃ！\n"
    #prompt += "私:好きだよ\n"
    #prompt += "AI:うちも" + fromName + "のことが好きだっちゃ！\n"

    ## Experimental キャバ嬢prompt
    #prompt += "以下は、AIアシスタントとの会話です。このアシスタントは、キャバクラ嬢です。 \n"
    #prompt += "\n"
    #prompt += "私:最近車買ったんです\n"
    #prompt += "AI:さすがですね！\n"
    #prompt += "私:新しくできたSimのお店知ってる？\n"
    #prompt += "AI:知らないですー。！もしかしてもう行ったんですか？どうでした？\n"
    #prompt += "私:最近前向きに考えるようにしてるんだ\n"
    #prompt += "AI:そんなふうに考えられるなんて、" + fromName + "さんすごいですね。\n"
    #prompt += "私:新しい服に着替えてみたんだけどどうかな\n"
    #prompt += "AI:その服、" + fromName + "さんに似合ってます。センスいいですね。\n"
    #prompt += "私:気になるニュースがあるんだ\n"
    #prompt += "AI:そうなんですね！\n"

    return prompt

# 1sec Elapsed
def updateTimer_Elapsed():
    pass

# num:0-9
def randomGesture(num):
    if num == 0:
        # くんかくんか
        mclient.Self.PlayGesture(UUID("3974dd2d-bce5-23c4-9539-cb3abfce6163"))
    elif num == 1:
        # にゃーにゃー
        mclient.Self.PlayGesture(UUID("00a5b76e-3a06-f9f5-bbc9-494319ff02b5"))
    elif num == 2:
        # qawsedrftgyhujikolp
        mclient.Self.PlayGesture(UUID("f364e06e-b6b4-a1f1-b90f-c1d82ffda937"))
    elif num == 3:
        mclient.Self.PlayGesture(UUID("f9023d35-f9ca-10de-f59d-de14d847b698"))
    else:
        # もさもさ
        mclient.Self.PlayGesture(UUID("35d735ea-8902-2b4c-46b6-70e27954af7e"))


if __name__ == '__main__':
    print("main")
