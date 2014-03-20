using System;
using System.Net;
using System.Collections;
using UnityEngine;
using Rug.Osc;
using System.Collections.Generic;
using System.Linq;

public class AutoOscReceiver : MonoBehaviour {
	
	public List<OscReceiverDescription>oscReceiversPublicInterface = new List<OscReceiverDescription>();

	private static AutoOscReceiver _instance = null;	
	public static AutoOscReceiver Instance {
		get{
			if(_instance == null){
				GameObject myInstanceObject = new GameObject();
				myInstanceObject.name = "_Unity Osc Receiver(Instance)";
				_instance = myInstanceObject.AddComponent<AutoOscReceiver>();			
			}
			return _instance;
		}
	}

	private void Awake(){
		if(_instance == null){
			_instance = this;
		}
		else{
			DestroyImmediate(this);
		}
	}

	void Start(){
		foreach(OscReceiverDescription descrip in oscReceiversPublicInterface){
			descrip.connectReceiver();
		}
	}

	void Update () {
		foreach(OscReceiverDescription descrip in oscReceiversPublicInterface){
			descrip.Update();
		}
	}


	public static void AddReceiver(string receiverName, int receiverPort, bool registerWithBonjour, bool useMulticast = false, string multicastIPAddress = ""){
		AutoOscReceiver.Instance.addReceiver(receiverName, receiverPort, registerWithBonjour, useMulticast, multicastIPAddress);
	}


	protected void addReceiver(string receiverName, int receiverPort, bool registerWithBonjour, bool useMulticast = false, string multicastIPAddress = ""){
		//first find if the name exists. iterate on the name
		int addedNumber = 1;
		bool foundMatch = false;
		string finalReceiverName = receiverName;
		//iterate once, find a match.
		if(!isReceiverNameUnique(receiverName)){
		   while(!isReceiverNameUnique(receiverName + addedNumber)){
				//incrementing...
				addedNumber++;
			}

			finalReceiverName = receiverName + addedNumber;
		}

		oscReceiversPublicInterface.Add (new OscReceiverDescription(finalReceiverName, receiverPort, registerWithBonjour, useMulticast, multicastIPAddress));
		oscReceiversPublicInterface.Last().connectReceiver();
	}

	public static bool supportsBonjour(){ // add any other tests here- like platforms supported, etc

#if UNITY_EDITOR_OSX
		return Application.HasProLicense();
#elif UNITY_IPHONE
		return Application.HasProLicense();
#elif UNITY_STANDALONE_OSX
		return Application.HasProLicense();
#else
		return false;
#endif
	}

	//returns false if it isn't unique, and adds 1 to addedNumber. returns true if it was unique.
	public bool isReceiverNameUnique(string name){
		foreach(OscReceiverDescription descrip in oscReceiversPublicInterface){
			if(descrip.Name == name){
					return false;
				}
		}
		return true;
	}
	
	public static void RegisterReceiveMethod(string receiverName, string address, OscMessageEvent msgEvent){
		foreach(OscReceiverDescription descrip in AutoOscReceiver.Instance.oscReceiversPublicInterface){
			if(descrip.Name == receiverName){
				descrip.Manager.Attach(address, msgEvent);
			}
		}
	}

	public static void UnregisterReceiveMethod(string receiverName, string address, OscMessageEvent msgEvent){
		if(_instance == null)
			return;

		foreach(OscReceiverDescription descrip in AutoOscReceiver.Instance.oscReceiversPublicInterface){
			if(descrip.Name == receiverName && descrip.Manager != null){
				descrip.Manager.Detach(address, msgEvent);
			}
		}
	}


	// OnDestroy is called when the object is destroyed
	public void OnDestroy () {
		foreach(OscReceiverDescription descrip in oscReceiversPublicInterface){
			descrip.Destroy();
		}
	}
}
