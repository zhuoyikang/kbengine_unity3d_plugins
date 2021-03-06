﻿using UnityEngine;
using KBEngine;
using System; 
using System.IO;  
using System.Text;
using System.Collections;

namespace KBEngine
{
	
public class PersistentInofs
{
	string persistentDataPath = "";
	
    public PersistentInofs(string path)
    {
    	persistentDataPath = path;
    	installEvents();
    	loadAll();
    }
        
	void installEvents()
	{
		KBEngine.Event.registerOut("onImportClientMessages", this, "onImportClientMessages");
		KBEngine.Event.registerOut("onImportServerErrorsDescr", this, "onImportServerErrorsDescr");
		KBEngine.Event.registerOut("onImportClientEntityDef", this, "onImportClientEntityDef");
		KBEngine.Event.registerOut("onVersionNotMatch", this, "onVersionNotMatch");
		KBEngine.Event.registerOut("onScriptVersionNotMatch", this, "onScriptVersionNotMatch");
		KBEngine.Event.registerOut("onServerDigest", this, "onServerDigest");
	}
	
	public bool loadAll()
	{
		byte[] loginapp_onImportClientMessages = loadFile (persistentDataPath, "loginapp_clientMessages." + 
		                                                   KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);

		byte[] baseapp_onImportClientMessages = loadFile (persistentDataPath, "baseapp_clientMessages." + 
		                                                  KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);

		byte[] onImportServerErrorsDescr = loadFile (persistentDataPath, "serverErrorsDescr." + 
		                                             KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);

		byte[] onImportClientEntityDef = loadFile (persistentDataPath, "clientEntityDef." + 
		                                           KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);

		if(loginapp_onImportClientMessages.Length > 0 && baseapp_onImportClientMessages.Length > 0)
		{
			KBEngineApp.app.importMessagesFromMemoryStream (loginapp_onImportClientMessages, baseapp_onImportClientMessages, onImportClientEntityDef, onImportServerErrorsDescr);
		}
		
		return true;
	}
	
	public void onImportClientMessages(string currserver, byte[] stream)
	{
		if(currserver == "loginapp")
			createFile (persistentDataPath, "loginapp_clientMessages." + 
			            KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion, stream);
		else
			createFile (persistentDataPath, "baseapp_clientMessages." + 
			            KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion, stream);
	}

	public void onImportServerErrorsDescr(byte[] stream)
	{
		createFile (persistentDataPath, "serverErrorsDescr." + 
		            KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion, stream);
	}
	
	public void onImportClientEntityDef(byte[] stream)
	{
		createFile (persistentDataPath, "clientEntityDef." + 
		            KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion, stream);
	}
	
	public void onVersionNotMatch(string verInfo, string serVerInfo)
	{
		clearMessageFiles();
	}

	public void onScriptVersionNotMatch(string verInfo, string serVerInfo)
	{
		clearMessageFiles();
	}
	
	public void onServerDigest(string currserver, string serverProtocolMD5, string serverEntitydefMD5)
	{
		// 我们不需要检查网关的协议， 因为登录loginapp时如果协议有问题已经删除了旧的协议
		if(currserver == "baseapp")
		{
			return;
		}
		
		if(loadFile(persistentDataPath, serverProtocolMD5 + serverEntitydefMD5).Length == 0)
		{
			clearMessageFiles();
			createFile(persistentDataPath, serverProtocolMD5 + serverEntitydefMD5, new byte[1]);
		}
	}
		
	public void clearMessageFiles()
	{
		deleteFile(persistentDataPath, "loginapp_clientMessages." + KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);
		deleteFile(persistentDataPath, "baseapp_clientMessages." + KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);
		deleteFile(persistentDataPath, "serverErrorsDescr." + KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);
		deleteFile(persistentDataPath, "clientEntityDef." + KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion);
		KBEngineApp.app.resetMessages();
	}
	
	public void createFile(string path, string name, byte[] datas)  
	{  
		deleteFile(path, name);
		Dbg.DEBUG_MSG("createFile: " + path + "/" + name);
		FileStream fs = new FileStream (path + "/" + name, FileMode.OpenOrCreate, FileAccess.Write);
		fs.Write (datas, 0, datas.Length);
		fs.Close ();
		fs.Dispose ();
	}  
   
   public byte[] loadFile(string path, string name)  
   {  
		FileStream fs;

		try{
			fs = new FileStream (path + "/" + name, FileMode.Open, FileAccess.Read);
		}
		catch (Exception e)
		{
			Dbg.DEBUG_MSG("loadFile: " + path + "/" + name);
			Dbg.DEBUG_MSG(e.ToString());
			return new byte[0];
		}

		byte[] datas = new byte[fs.Length];
		fs.Read (datas, 0, datas.Length);
		fs.Close ();
		fs.Dispose ();

		Dbg.DEBUG_MSG("loadFile: " + path + "/" + name + ", datasize=" + datas.Length);
		return datas;
   }  
   
   public void deleteFile(string path, string name)  
   {  
		Dbg.DEBUG_MSG("deleteFile: " + path + "/" + name);
		
		try{
        	File.Delete(path + "/"+ name);  
		}
		catch (Exception e)
		{
			Debug.LogError(e.ToString());
		}
   }  
}

}
