﻿namespace KBEngine
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using MessageID = System.UInt16;

    public class Message
    {
    	public MessageID id = 0;
        public string name;
        public Int16 msglen = -1;
        public System.Reflection.MethodInfo handler = null;
        public System.Reflection.MethodInfo[] argtypes = null;
        public sbyte argsType = 0;

        public static Dictionary<MessageID, Message> loginappMessages = new Dictionary<MessageID, Message>();
        public static Dictionary<MessageID, Message> baseappMessages = new Dictionary<MessageID, Message>();
        public static Dictionary<MessageID, Message> clientMessages = new Dictionary<MessageID, Message>();

        public static Dictionary<string, Message> messages = new Dictionary<string, Message>();

        public static void clear()
        {
            loginappMessages = new Dictionary<MessageID, Message>();
            baseappMessages = new Dictionary<MessageID, Message>();
            clientMessages = new Dictionary<MessageID, Message>();
            messages = new Dictionary<string, Message>();

            bindFixedMessage();
        }

        public static void bindFixedMessage()
        {
            // 引擎协议说明参见: http://www.kbengine.org/cn/docs/programming/clientsdkprogramming.html
            Message.messages["Loginapp_importClientMessages"] =
                new Message(5, "importClientMessages", 0, 0, new List<Byte>(), null);
            Message.messages["Loginapp_hello"] =
                new Message(4, "hello", -1, -1, new List<Byte>(), null);

            Message.messages["Baseapp_importClientMessages"] =
                new Message(207, "importClientMessages", 0, 0, new List<Byte>(), null);
            Message.messages["Baseapp_importClientEntityDef"] =
                new Message(208, "importClientMessages", 0, 0, new List<Byte>(), null);
            Message.messages["Baseapp_hello"] =
                new Message(200, "hello", -1, -1, new List<Byte>(), null);

            Message.messages["Client_onHelloCB"] =
                new Message(521, "Client_onHelloCB", -1, -1, new List<Byte>(),
                            KBEngineApp.app.GetType().GetMethod("Client_onHelloCB"));
            Message.clientMessages[Message.messages["Client_onHelloCB"].id] =
                Message.messages["Client_onHelloCB"];

            Message.messages["Client_onImportClientMessages"] =
                new Message(518, "Client_onImportClientMessages", -1, -1, new List<Byte>(),
                            KBEngineApp.app.GetType().GetMethod("Client_onImportClientMessages"));
            Message.clientMessages[Message.messages["Client_onImportClientMessages"].id] =
                Message.messages["Client_onImportClientMessages"];
        }

        public Message(MessageID msgid, string msgname, Int16 length, sbyte argstype,
                       List<Byte> msgargtypes, System.Reflection.MethodInfo msghandler)
        {
            id = msgid;
            name = msgname;
            msglen = length;
            handler = msghandler;
            argsType = argstype;

            argtypes = new System.Reflection.MethodInfo[msgargtypes.Count];
            for(int i=0; i<msgargtypes.Count; i++)
            {
                argtypes[i] = StreamRWBinder.bindReader(msgargtypes[i]);
                if(argtypes[i] == null)
                {
                    Dbg.ERROR_MSG("Message::Message(): bindReader(" + msgargtypes[i] + ") is error!");
                }
            }

            // Dbg.DEBUG_MSG(string.Format("Message::Message(): ({0}/{1}/{2})!",
            //	msgname, msgid, msglen));
        }

        public object[] createFromStream(MemoryStream msgstream)
        {
            if(argtypes.Length <= 0)
                return new object[]{msgstream};

            object[] result = new object[argtypes.Length];

            for(int i=0; i<argtypes.Length; i++)
            {
                result[i] = argtypes[i].Invoke(msgstream, new object[0]);
            }

            return result;
        }

        public void handleMessage(MemoryStream msgstream)
        {
            if(argtypes.Length <= 0)
            {
                if(argsType < 0)
                    handler.Invoke(KBEngineApp.app, new object[]{msgstream});
                else
                    handler.Invoke(KBEngineApp.app, new object[]{});
            }
            else
            {
                handler.Invoke(KBEngineApp.app, createFromStream(msgstream));
            }
        }
    }
}
