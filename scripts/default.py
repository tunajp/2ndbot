import clr

clr.AddReference("System")
from System import *

clr.AddReference("LibreMetaverse")
from OpenMetaverse import *

def scriptInfo():
    return "default.py"

def Network_OnLogin():
    return "Network_OnLogin"

def command(fromUUID, fromName, message, type):
    if "テスト" in message:
        mclient.Self.Chat(fromName + "テスト", 0, ChatType.Normal)
    else:
        mclient.Self.Chat(fromName + "IronPython", 0, ChatType.Normal)

if __name__ == '__main__':
    print("main")
