using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(AutoOscReceiver))]
public class AutoOscReceiverInspector : Editor {
	private enum Server_Array_Error{
		NoErr,
		DuplicateServerName,
		DuplicateIPOrPort
	}

	private Color32 addReceiverColor = Color.cyan;
	private Color receiverListColor = new Color32((byte)111, (byte)138, (byte)255, (byte)255);

	//an index into the array of the 'currently editing' server description. gui only lets you edit one server at a time.
	private int editingLabelIdx = 0;
	//isEditingIdx is letting you know that currentHashEditing is in an edit-state
	private bool isEditingIdx = false;

	private OscReceiverDescription customDescription;
	//when you mark it to go to edit mode, it caches your current description. when you exit edit mode, it validates the change, if there are any changes.
	private OscReceiverDescription cachedDescription;

	private AutoOscReceiver sender;
	private SerializedObject sObject;
	private SerializedProperty oscReceiversPublicInterface;
	private string oscReceiversPublicInterfaceString = "oscReceiversPublicInterface";


	void OnEnable () {
		customDescription = new OscReceiverDescription("ReceiverName", 9000, true,false, "239.0.0.222");

		sObject = new SerializedObject(target);
		sender = (AutoOscReceiver)sObject.targetObject;
		oscReceiversPublicInterface = sObject.FindProperty(oscReceiversPublicInterfaceString);
	}
	
	public override void OnInspectorGUI () {
		sObject.Update();

		DrawIPAddresses();

		sObject.ApplyModifiedProperties();
	}


	public void DrawCustomAddress(OscReceiverDescription description, string addOrX, Action<OscServerDescription> theMethodToCall, bool showCustomServerLabel = false, bool allowEditMode = false, Action validateEditingCompletion = null, int idx = 0){
		
		bool isEditMode = true;
		if(allowEditMode){//if you allow edit mode, and the 
			if(idx == editingLabelIdx && isEditingIdx)
				isEditMode = isEditingIdx;
			else{
				isEditMode = false;
			}
		}
		
		Rect drawZone = GUILayoutUtility.GetRect(50f, 16f);
		Rect drawZone2 = drawZone;
		drawZone2.width = 50;
		if(GUI.Button (drawZone2, addOrX)){
			theMethodToCall(description);
			isEditingIdx = false;
		}
		drawZone.x +=40;
		if(showCustomServerLabel){
			//this label is only used for display when using it as a sort of editor dealy
			EditorGUI.LabelField(drawZone, "Custom server:");
			drawZone.x += 90;
		}
		else{
			//this is used for when in playmode and you want to edit a thing
			float w = drawZone.width;
			drawZone.width = 40;
			drawZone.x +=10;
			
			if(allowEditMode){
				if(isEditMode){
					if(GUI.Button (drawZone, "Done")){
						isEditingIdx = false;
						
						if(validateEditingCompletion != null){
							validateEditingCompletion();
						}
					}
				}
				else{
					if(GUI.Button (drawZone, "Edit")){
						editingLabelIdx = idx;
						isEditingIdx = true;
						cachedDescription = new OscReceiverDescription(description.Name, description.Port, description.registerWithBonjour, description.useMulticast, description.HostAddress);
					}
				}
			}
			
			drawZone.width = w;
			drawZone.x += 30;
		}
		if(isEditMode){
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
			description.Name = EditorGUI.TextField(drawZone, description.Name);
			if(userHitReturn && allowEditMode){
				isEditingIdx = false;				
				if(validateEditingCompletion != null){
					validateEditingCompletion();
				}
			}
			
		}
		else{
			EditorGUI.LabelField(drawZone, description.Name);
		}	
		
		drawZone = GUILayoutUtility.GetRect(50f, 16);
		drawZone.width += 20;
		drawZone.y +=5;
		drawZone.height +=1;
//		EditorGUI.LabelField(drawZone, "IP:");
//		drawZone.x +=20;
		drawZone.width = 100;
		
		if(isEditMode){
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
//			description.HostAddress = EditorGUI.TextField(drawZone, description.HostAddress);
			description.useMulticast = EditorGUI.ToggleLeft(drawZone,"multicast", description.useMulticast);

			if(userHitReturn && allowEditMode){
				isEditingIdx = false;				
				if(validateEditingCompletion != null){
					validateEditingCompletion();
				}
			}
			
		}		
		else
		{
			EditorGUI.LabelField(drawZone,(description.useMulticast ? "☑ " : "☐ ") + "multicast");
//			EditorGUI.LabelField(drawZone, description.HostAddress);
		}

		drawZone.x +=80;
		drawZone.width = 120;
		if(isEditMode){
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
			//			description.HostAddress = EditorGUI.TextField(drawZone, description.HostAddress);
			description.registerWithBonjour = EditorGUI.ToggleLeft(drawZone,"registerBonjour", description.registerWithBonjour);
			
			if(userHitReturn && allowEditMode){
				isEditingIdx = false;				
				if(validateEditingCompletion != null){
					validateEditingCompletion();
				}
			}
			
		}		
		else
		{
			EditorGUI.LabelField(drawZone,(description.registerWithBonjour ? "☑ " : "☐ ") + "registerBonjour");
		}

		drawZone.x +=110;
		EditorGUI.LabelField(drawZone, "Port:");
		drawZone.x +=30;
		drawZone.width = 70;
		if(isEditMode){
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
			description.Port = EditorGUI.IntField(drawZone, description.Port);
			if(userHitReturn && allowEditMode){
				isEditingIdx = false;				
				if(validateEditingCompletion != null){
					validateEditingCompletion();
				}
			}
		}
		else{
			EditorGUI.LabelField(drawZone, description.Port.ToString());
		}


		if(description.useMulticast){
			drawZone = GUILayoutUtility.GetRect(50f, 16);
			drawZone.width += 20;
			drawZone.y +=5;
			drawZone.height +=1;
			EditorGUI.LabelField(drawZone, "IP:");
			drawZone.x +=20;
			drawZone.width = 120;

			if(isEditMode){
				bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
				description.HostAddress = EditorGUI.TextField(drawZone, description.HostAddress);
//				description.useMulticast = EditorGUI.ToggleLeft(drawZone,"multicast", description.useMulticast);
				
				if(userHitReturn && allowEditMode){
					isEditingIdx = false;				
					if(validateEditingCompletion != null){
						validateEditingCompletion();
					}
				}
				
			}		
			else
			{
//				EditorGUI.LabelField(drawZone,(description.useMulticast ? "☑ " : "☐ ") + "multicast");
				EditorGUI.LabelField(drawZone, description.HostAddress);
			}
		}
		GUILayout.Space (6);
	}


	public void AddCustomServerToOscReceiversPlayMode(OscServerDescription descrip){
		Server_Array_Error existsAlready = existsInArray(customDescription.Name,
		                                                 customDescription.HostAddress,
		                                                 customDescription.Port);
		switch(existsAlready){
			case Server_Array_Error.DuplicateServerName: 
			{
				Debug.LogError("could not add, server: \'" + customDescription.Name +"\' already exists in the array.");
				break;
			}
			case Server_Array_Error.DuplicateIPOrPort: 
			case Server_Array_Error.NoErr: 
			{
				sender.oscReceiversPublicInterface.Add(new OscReceiverDescription(customDescription.Name, customDescription.Port, customDescription.registerWithBonjour, customDescription.useMulticast, customDescription.HostAddress));
				if(Application.isPlaying){
				sender.oscReceiversPublicInterface.Last().connectReceiver();
				}
				break;
			}
		}
	}


	private Server_Array_Error existsInArray(string n, string hostname, int testport){
		Server_Array_Error nameErr = Server_Array_Error.NoErr;
		Server_Array_Error ipErr = Server_Array_Error.NoErr;
		
		foreach(OscServerDescription srv in sender.oscReceiversPublicInterface){
			if(srv.Name == n){
				//log error here			
				nameErr = Server_Array_Error.DuplicateServerName;
				//name errors take precedence- if you find a name conflict, exit immediately. if you find both a name and ip conflict, return the name error.
				return nameErr;
			}
			else if(srv.HostAddress == hostname && srv.Port == testport){
				//log error here
				ipErr = Server_Array_Error.DuplicateIPOrPort;
			}
		}
		
		if(ipErr != Server_Array_Error.NoErr && nameErr != Server_Array_Error.NoErr)
			return nameErr;
		
		return ipErr;
	}

	public void DrawIPAddresses(){
		Color defaultColor = GUI.backgroundColor;
		GUI.backgroundColor = addReceiverColor;
		EditorGUI.indentLevel++;
		EditorGUILayout.HelpBox("Add a Receiver: ",MessageType.None);

		DrawCustomAddress(customDescription, "add", AddCustomServerToOscReceiversPlayMode, true, false);
		
		GUILayout.Space(4);
		Rect drawLatestDerp = GUILayoutUtility.GetRect(0f, 1);
		GUI.backgroundColor = defaultColor;

		GUI.Box(drawLatestDerp, "");
		GUI.backgroundColor = receiverListColor;
		if(oscReceiversPublicInterface.arraySize == 0){
				EditorGUILayout.HelpBox("No OSC Receivers set up to connect! Please add one.",MessageType.Warning);
		}
		else
			EditorGUILayout.HelpBox("Receivers:",MessageType.None);
		
		for (int i = 0; i < sender.oscReceiversPublicInterface.Count; i++)
		{
//			SerializedProperty elementProperty = oscReceiversPublicInterface.GetArrayElementAtIndex(i);
//			//all we do here is either delete the property ("X"), display its label, or edit. 
			DrawCustomAddress(sender.oscReceiversPublicInterface[i], //the property to draw
			                              "X", //the top left button, in this case 'x'
			                  RemoveReceiverFromList, //what to do when you press the 'x'
			                  false, true, OnEditModeCompleteSoCheckForCachedDescriptionChanges, i
			                              );
//			
		}
		
		
		EditorGUI.indentLevel--;
	}


	//validate any changes...
	public void OnEditModeCompleteSoCheckForCachedDescriptionChanges()
	{
		if(editingLabelIdx >= sender.oscReceiversPublicInterface.Count){
			Debug.Log ("GUI editing error!");
			return;
		}

		OscReceiverDescription description = sender.oscReceiversPublicInterface[editingLabelIdx];

		bool nameChanged = false;
		bool ipChanged = false;
		bool portChanged = false;
		bool registerBonjourChanged = false;
		bool useMulticastChanged = false;

		if(cachedDescription.Name != description.Name)
			nameChanged = true;
		if(cachedDescription.Port != description.Port)
			portChanged = true;
		if(cachedDescription.HostAddress != description.HostAddress)
			ipChanged = true;
		if(cachedDescription.useMulticast != description.useMulticast)
			useMulticastChanged = true;
		if(cachedDescription.registerWithBonjour != description.registerWithBonjour)
			registerBonjourChanged = true;

		if(registerBonjourChanged){
			if(Application.isPlaying)
				description.connectReceiver();
		}

		if(useMulticastChanged){
			if(Application.isPlaying)
				description.connectReceiver();
		}

		string newName = description.Name;
		int newPort = description.Port;
		string newAddress = description.HostAddress;
		//temporarily revert description to cached values, check if new value exists in array, then modify if you're allowed.

		if(nameChanged){		
			description.Name = cachedDescription.Name;
			Server_Array_Error err = existsInArray(newName, description.HostAddress, description.Port);
			switch(err)
			{
				case Server_Array_Error.DuplicateServerName: 
				{
					Debug.LogError("could not use \'" + description.Name + "\' as the server name, it already exists in the array.");
					break;
				}
				case Server_Array_Error.DuplicateIPOrPort: 				
					//fall through, it's ok if ip's the same if you modify the name- that's expected. 				
				case Server_Array_Error.NoErr: 
				{
					description.Name = newName;
					//you already changed the name...just reconnect plz.
					if(Application.isPlaying)
						description.connectReceiver();

					break;
				}
			}
		}
		if(ipChanged){
			description.HostAddress = cachedDescription.HostAddress;

			Server_Array_Error err = existsInArray(description.Name, newAddress, description.Port);
			switch(err)
			{
			case Server_Array_Error.DuplicateIPOrPort: 
			{
				Debug.LogError("could not edit IP/port of server \'" + cachedDescription.Name + "\'- that IP/port already exists in the array.");						
				break;
			}
			case Server_Array_Error.DuplicateServerName: 				
				//fall through, it's ok if name is the same.
			case Server_Array_Error.NoErr: 
			{
				description.HostAddress = newAddress;

				if(Application.isPlaying)
					description.connectReceiver();
				break;
			}
			}
		}
		if(portChanged){
			description.Port = cachedDescription.Port;
			Server_Array_Error err = existsInArray(description.Name, description.HostAddress, newPort);
			switch(err)
			{
				case Server_Array_Error.DuplicateIPOrPort: 
				{
					Debug.LogError("could not edit IP/port of server \'" + cachedDescription.Name + "\'- that IP/port already exists in the array.");						
					break;
				}
				case Server_Array_Error.DuplicateServerName: 				
					//fall through, it's ok if name is the same.
				case Server_Array_Error.NoErr: 
				{
					
					description.Port = newPort;
					if(Application.isPlaying)
						description.connectReceiver();
					break;
				}
			}
		}
	}

	public void RemoveReceiverFromList(OscServerDescription descrip){

		for(int i =0; i < sender.oscReceiversPublicInterface.Count; i++){
			if(sender.oscReceiversPublicInterface[i].Name == descrip.Name){
				if(Application.isPlaying)
					sender.oscReceiversPublicInterface[i].Destroy();
				sender.oscReceiversPublicInterface.RemoveAt(i);
			}
		}
	}


}
