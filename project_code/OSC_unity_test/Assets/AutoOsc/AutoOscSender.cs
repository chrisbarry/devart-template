using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rug.Osc;
using System.Linq;

public class AutoOscSender : MonoBehaviour {
	
	public Dictionary<string, OscSenderDescription>oscSenderDictionary = new Dictionary<string, OscSenderDescription>();
	//anything that gets added to this List (in the editor) will be serialized into oscSenderDictionary on player start.
	public List<OscServerDescription> oscSendersPublicInterface = new List<OscServerDescription>();

	#region Private/Protected Members
	private string[] oscSenderDictionaryKeys = new string[0];
	// Sender Instance for singleton.
	protected static AutoOscSender _instance = null;
	#endregion

	public static string[] OscSenderDictionaryKeys{
		get {
			return AutoOscSender.Instance.oscSenderDictionaryKeys;
		}
	}

	public static AutoOscSender Instance {
		get{
			if(_instance == null){
				GameObject myInstanceObject = new GameObject();
				myInstanceObject.name = "_Unity Osc Sender(Instance)";
				_instance = myInstanceObject.AddComponent<AutoOscSender>();

			}
			return _instance;
		}
	}
	
	public static void AddSender(OscServerDescription description, bool connectToSender = true){
		OscSenderDescription senderDescrip;
		if(!AutoOscSender.Instance.oscSenderDictionary.TryGetValue(description.Name, out senderDescrip)){
			//create a new OscSenderDescription from an OscServerDescription
			AutoOscSender.Instance.oscSenderDictionary.Add(description.Name, new OscSenderDescription(description));
		}
		//if there already EXISTS a sender in the dictionary with that name,
		else{
			//if it has a sender and it's connected, change its backing info and call connectSender() again (Which disconnects and reconnects for you)
			AutoOscSender.Instance.oscSenderDictionary[senderDescrip.Name].RefreshBackingDescription(description);
		}

		if(connectToSender){
			AutoOscSender.Instance.oscSenderDictionary[description.Name].connectSender();
		}
		AutoOscSender.Instance.oscSenderDictionaryKeys = AutoOscSender.Instance.oscSenderDictionary.Keys.ToArray();
	}


	public static void AddSender(string theName){
		OscSenderDescription senderDescrip;
		if(!AutoOscSender.Instance.oscSenderDictionary.TryGetValue(theName, out senderDescrip)){
			//create a new OscSenderDescription from an OscServerDescription
			AutoOscSender.Instance.oscSenderDictionary.Add(theName, new OscSenderDescription(theName));
		}
		//if there already EXISTS a sender in the dictionary with that name,
		else{
			//if you try to add a sender with just a name, and there already exists a name in the dictionary, dont do anything. you dont have any new info for it.
		}

		AutoOscSender.Instance.oscSenderDictionaryKeys = AutoOscSender.Instance.oscSenderDictionary.Keys.ToArray();
	}

	private void Awake(){
		if(_instance == null){
			_instance = this;
		}
		else{
			DestroyImmediate(this);
		}

		oscSenderDictionary.Clear();
	}

	//only sends the osc message if it exists and is connected.
	public static void Send(string controllerName, OscMessage msg){
		OscSenderDescription senderEntry;
		//find the osc sender entry in the list
		if(AutoOscSender.Instance.oscSenderDictionary.TryGetValue(controllerName, out senderEntry)){
			if(senderEntry.oscSender != null && senderEntry.oscSender.State == OscSocketState.Connected){
				senderEntry.oscSender.Send(msg);
			}
		}
	}

	void OnEnable () {

		//this is because we can't properly serialize dictionaries/hashtables yet...
		//add the senders, then connect to them afterwards.
		foreach(OscServerDescription description in oscSendersPublicInterface){
			AutoOscSender.AddSender(description, false);
		}

		foreach(KeyValuePair<string, OscSenderDescription> sender in oscSenderDictionary){
				sender.Value.connectSender();
		}

		UnityBonjourBrowser.BonjourServiceAdded += new UnityBonjourBrowser.BonjourServiceNotificationHandler(BonjourServiceAdded);
		UnityBonjourBrowser.BonjourServiceRemoved += new UnityBonjourBrowser.BonjourServiceNotificationHandler(BonjourServiceRemoved);

	}	

	public void BonjourServiceAdded(OscServerDescription description){
		//if a service was added, we need to check our senders and see if we have it in the list, and then refresh it.
		OscSenderDescription senderEntry;
		//find the osc sender entry in the list
		if(AutoOscSender.Instance.oscSenderDictionary.TryGetValue(description.Name, out senderEntry)){
			senderEntry.RefreshBackingDescription(description);
			senderEntry.connectSender();
		}
	}

	public void BonjourServiceRemoved(OscServerDescription description){
		//if a service was removed, we need to check our senders and see if we have it in the list, and then disconnect from that port.
		OscSenderDescription senderEntry;
		//find the osc sender entry in the list
		if(AutoOscSender.Instance.oscSenderDictionary.TryGetValue(description.Name, out senderEntry)){
			senderEntry.disconnectSender();
		}
	}

	public void OnDisable() {
		foreach(KeyValuePair<string, OscSenderDescription> sender in oscSenderDictionary){
				sender.Value.disconnectSender();
		}

		UnityBonjourBrowser.BonjourServiceAdded -= new UnityBonjourBrowser.BonjourServiceNotificationHandler(BonjourServiceAdded);
		UnityBonjourBrowser.BonjourServiceRemoved -= new UnityBonjourBrowser.BonjourServiceNotificationHandler(BonjourServiceRemoved);
	}

}
