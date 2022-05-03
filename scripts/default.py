import clr

clr.AddReference("System")
clr.AddReference("System.Console")
clr.AddReference("System.XML")
from System import *
from System.Console import *
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

def command(fromUUID, fromName, message, type):
    if "テスト" in message:
        mclient.Self.Chat(fromName + "テスト", 0, ChatType.Normal)
    elif "NPC" in message:
        #pass
        Console.WriteLine("NPC")
        standupcommand.Execute(UUID.Zero, "", "", 0)
        mclient.Self.AutoPilot(263911.29400634766,248179.05200195312,21.72664451599121)
    elif "グローバル" in message:
        pos = application.getCurrentGlobalPosition()
        print(pos)
    else:
        mclient.Self.Chat(fromName + "IronPython", 0, ChatType.Normal)

# 1sec Elapsed
def updateTimer_Elapsed():
    pass

if __name__ == '__main__':
    print("main")
