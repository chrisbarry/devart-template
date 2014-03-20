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
[CustomEditor(typeof(AutoOscSender))]
public class AutoOscSenderInspector : Editor {

	private enum Server_Array_Error{
		NoErr,
		DuplicateServerName,
		DuplicateIPOrPort
	}
	private Color32 addSenderColor = new Color32((byte)255, (byte)49, (byte)49, (byte)255);
	private Color senderListColor = new Color(181/255.0f, 143/255.0f, 182/255.0f, 1.0f);

	//an index into the array of the 'currently editing' server description. gui only lets you edit one server at a time.
	private int editingLabelIdx = 0;
	//isEditingIdx is letting you know that currentHashEditing is in an edit-state
	private bool isEditingIdx = false;
	//currentHashEditing is the index into the dictionary we are currently editing. it is similar to the index in an array like editingLabelIdx above.
	private string currentHashEditing = "";

	//needsKeySwap is so we can modify the dictionary after iteration is complete.
	private bool needsKeySwap = false;
	private string prevKey = "";
	private string newKey = "";

	private OscServerDescription customDescription = new OscServerDescription();
	//when you mark it to go to edit mode, it caches your current description. when you exit edit mode, it validates the change, if there are any changes.
	private OscServerDescription cachedDescription = new OscServerDescription();

	private AutoOscSender sender;
	private SerializedObject sObject;
	private SerializedProperty oscSendersPublicInterface;
	private int availableOscServersSize;
	[SerializeField]
	private int selectedBonjourBrowserPopupIndex = 0;
	private string oscSendersPublicInterfaceString = "oscSendersPublicInterface";
	void OnEnable () {
		customDescription = new OscServerDescription("ServerName", "127.0.0.1", 9000);
		sObject = new SerializedObject(target);
		sender = (AutoOscSender)sObject.targetObject;
//		isEditingIdx = false;
		oscSendersPublicInterface = sObject.FindProperty(oscSendersPublicInterfaceString);
	}

	public override void OnInspectorGUI () {
		sObject.Update();
		if(!Application.isPlaying)
			DrawIPAddressesEditMode(); //we use a different array in edit mode, due to non-serializable Dictionaries.
		else{
			DrawIPAddressesPlayMode();
		}
		sObject.ApplyModifiedProperties();
	}

	public void DrawIPAddressesEditMode(){
		Color defaultColor = GUI.backgroundColor;
		GUI.backgroundColor = addSenderColor;
		EditorGUI.indentLevel++;
//		Rect drawZone;
		EditorGUILayout.HelpBox("Add a Sender: ",MessageType.None);
//		if(UnityBonjourBrowser.oscServerStrings.Length > 0){
			GUILayout.BeginHorizontal();
			if(GUILayout.Button ("add", GUILayout.Width (50))){
				AddBonjourServerToOscSendersEditMode();
			}
			selectedBonjourBrowserPopupIndex = Mathf.Clamp(selectedBonjourBrowserPopupIndex, 0, UnityBonjourBrowser.oscServerStrings.Length-1);
			GUILayout.Label ("Bonjour server:");
			Rect drawPopup = GUILayoutUtility.GetRect(0f, 16);
			drawPopup.x = 143;
			drawPopup.width = 150;
			selectedBonjourBrowserPopupIndex = EditorGUI.Popup(drawPopup, selectedBonjourBrowserPopupIndex, UnityBonjourBrowser.oscServerStrings);
			GUILayout.EndHorizontal();
//		}
		GUILayout.Space(5);

		DrawCustomAddressEdit(customDescription, "add", AddCustomServerToOscSendersEditMode, true, false);

		GUILayout.Space(4);
		Rect drawLatestDerp = GUILayoutUtility.GetRect(0f, 1);

		GUI.backgroundColor = defaultColor;
		GUI.Box(drawLatestDerp, "");
		GUI.backgroundColor = senderListColor;
		if(oscSendersPublicInterface.arraySize == 0){
			EditorGUILayout.HelpBox("No OSC Senders set up to connect! Please add one.",MessageType.Warning);
		}
		else
			EditorGUILayout.HelpBox("Senders:",MessageType.None);

		for (int i = 0; i < oscSendersPublicInterface.arraySize; i++)
		{
			SerializedProperty elementProperty = oscSendersPublicInterface.GetArrayElementAtIndex(i);
			//all we do here is either delete the property ("X"), display its label, or edit. 
			DrawCustomAddressLabelGeneric(elementProperty, //the property to draw
			                       "X", //the top left button, in this case 'x'
			                       RemovePropertyFromList, //what to do when you press the 'x'
			                       i //the property's index in the array
			                       );

		}

			
		EditorGUI.indentLevel--;
	}
		
	public void AddBonjourServerToOscSendersEditMode(){
		if(UnityBonjourBrowser.Instance.availableOscServers == null || UnityBonjourBrowser.Instance.availableOscServers.Count <= 0){
			return;
		}
		int idx = Mathf.Clamp(selectedBonjourBrowserPopupIndex, 0, UnityBonjourBrowser.oscServerStrings.Length-1);
		Server_Array_Error existsAlready = existsInArray(UnityBonjourBrowser.Instance.availableOscServers[idx].Name,
		                                   UnityBonjourBrowser.Instance.availableOscServers[idx].HostAddress,
		                                   UnityBonjourBrowser.Instance.availableOscServers[idx].Port);
	
		if(existsAlready == Server_Array_Error.NoErr){
			sender.oscSendersPublicInterface.Add(UnityBonjourBrowser.Instance.availableOscServers[idx]);
//			foreach(OscServerDescription descrip in sender.oscSendersPublicInterface){
//				Debug.Log ("exists: " + descrip.Name + " " + descrip.HostAddress + " " + descrip.Port);
//			}
		}
		else{
			if(existsAlready == Server_Array_Error.DuplicateServerName)
				Debug.LogError("Could not add \'" + UnityBonjourBrowser.Instance.availableOscServers[idx].Name +"\' - already exists in array.");
			else if(existsAlready == Server_Array_Error.DuplicateIPOrPort)
					Debug.LogError("Could not add bonjour server - IP/Port of \'"+ UnityBonjourBrowser.Instance.availableOscServers[idx].HostAddress +":" +
				               UnityBonjourBrowser.Instance.availableOscServers[idx].Port +"\' already exists in array.");
		}
	}

	public void DrawCustomAddressEdit(OscServerDescription description, string addOrX, Action<OscServerDescription> theMethodToCall, bool showCustomServerLabel = false, bool allowEditMode = false, Action validateEditingCompletion = null){

		bool isEditMode = true;
		if(allowEditMode){//if you allow edit mode, and the 
			if(description.Name == currentHashEditing && isEditingIdx)
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
						currentHashEditing = description.Name;
						isEditingIdx = true;					
						cachedDescription = new OscServerDescription(description.Name, description.HostAddress, description.Port);
					}
				}
			}

			drawZone.width = w;
			drawZone.x += 30;
		}
		if(isEditMode){
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
			string prevName = description.Name;
			description.Name = EditorGUI.TextField(drawZone, description.Name);

			if(description.Name != prevName){
				currentHashEditing = description.Name;
			}
			//you need to update the oscSenderDictionary key here, too- from what it was, to what it is now. 

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
		EditorGUI.LabelField(drawZone, "IP:");
		drawZone.x +=20;
		drawZone.width = 120;

		if(isEditMode){
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
			description.HostAddress = EditorGUI.TextField(drawZone, description.HostAddress);

			if(userHitReturn && allowEditMode){
				isEditingIdx = false;				
				if(validateEditingCompletion != null){
					validateEditingCompletion();
				}
			}

		}		
		else
		{
			EditorGUI.LabelField(drawZone, description.HostAddress);
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
		GUILayout.Space (6);
	}

	public void AddCustomServerToOscSendersEditMode(OscServerDescription descrip){
		Server_Array_Error existsAlready = existsInArray(customDescription.Name,
		                                                 customDescription.HostAddress,
		                                                 customDescription.Port);
		switch(existsAlready){
			case Server_Array_Error.DuplicateIPOrPort: 
			{
				Debug.LogError("could not add, that ip or port already exists in the array.");
				break;
			}
			case Server_Array_Error.DuplicateServerName: 
			{
			Debug.LogError("could not add, server: \'" + customDescription.Name +"\' already exists in the array.");
				break;
			}
			case Server_Array_Error.NoErr: 
			{
//			Debug.Log ("ADDING!");
			sender.oscSendersPublicInterface.Add (new OscServerDescription(customDescription.Name, customDescription.HostAddress, customDescription.Port));
				break;
			}
		}
	}

	public void AddBonjourServerToOscSendersPlayMode(){
		if(UnityBonjourBrowser.Instance.availableOscServers == null || UnityBonjourBrowser.Instance.availableOscServers.Count <= 0){
			return;
		}
		int idx = Mathf.Clamp(selectedBonjourBrowserPopupIndex, 0, UnityBonjourBrowser.oscServerStrings.Length-1);
		Server_Array_Error existsAlready = existsInArray(UnityBonjourBrowser.Instance.availableOscServers[idx].Name,
		                                                 UnityBonjourBrowser.Instance.availableOscServers[idx].HostAddress,
		                                                 UnityBonjourBrowser.Instance.availableOscServers[idx].Port);
		
		if(existsAlready == Server_Array_Error.NoErr){
			sender.oscSenderDictionary.Add(UnityBonjourBrowser.Instance.availableOscServers[idx].Name, new OscSenderDescription(UnityBonjourBrowser.Instance.availableOscServers[idx]));
			sender.oscSenderDictionary[UnityBonjourBrowser.Instance.availableOscServers[idx].Name].connectSender();
		}
		else{
			if(existsAlready == Server_Array_Error.DuplicateServerName)
				Debug.LogError("Could not add \'" + UnityBonjourBrowser.Instance.availableOscServers[idx].Name +"\' - already exists in array.");
			else if(existsAlready == Server_Array_Error.DuplicateIPOrPort)
				Debug.LogError("Could not add bonjour server - IP/Port of \'"+ UnityBonjourBrowser.Instance.availableOscServers[idx].HostAddress +":" +
				               UnityBonjourBrowser.Instance.availableOscServers[idx].Port +"\' already exists in array.");
		}
	}

	public void AddCustomServerToOscSendersPlayMode(OscServerDescription descrip){
		Server_Array_Error existsAlready = existsInArray(customDescription.Name,
		                                                 customDescription.HostAddress,
		                                                 customDescription.Port);
		switch(existsAlready){
			case Server_Array_Error.DuplicateIPOrPort: 
			{
				Debug.LogError("could not add, that ip or port already exists in the array.");
				break;
			}
			case Server_Array_Error.DuplicateServerName: 
			{
				Debug.LogError("could not add, server: \'" + customDescription.Name +"\' already exists in the array.");
				break;
			}
			case Server_Array_Error.NoErr: 
			{
				//			Debug.Log ("ADDING!");
				sender.oscSenderDictionary.Add (customDescription.Name, new OscSenderDescription(customDescription));
				sender.oscSenderDictionary[customDescription.Name].connectSender();
				break;
			}
		}
	}

	private Server_Array_Error existsInDictionary(string n, string hostname, int testport){
		Server_Array_Error err = Server_Array_Error.NoErr;
		foreach(KeyValuePair<string, OscSenderDescription> srv in sender.oscSenderDictionary){
			if(srv.Key == n){
				//log error here
				err = Server_Array_Error.DuplicateServerName;
				return err;
			}
			else if(srv.Value.HostAddress == hostname && srv.Value.Port == testport){
				//log error here
				err = Server_Array_Error.DuplicateIPOrPort;
				return err;
			}
		}
		return err;
	}
	private Server_Array_Error existsInArray(string n, string hostname, int testport){
		Server_Array_Error nameErr = Server_Array_Error.NoErr;
		Server_Array_Error ipErr = Server_Array_Error.NoErr;

		//if we're playing, use the Dictionary, not the List.
		if(Application.isPlaying)
		{
			return existsInDictionary(n, hostname, testport);
		}

		foreach(OscServerDescription srv in sender.oscSendersPublicInterface){
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

	//validate any changes...
	public void OnEditModeCompleteSoCheckForCachedDescriptionChanges(){
		if(!sender.oscSenderDictionary.ContainsKey(cachedDescription.Name)){
			Debug.Log ("error! sender dictionary does not contain : " + cachedDescription.Name);
			return;
		}
		OscSenderDescription description = sender.oscSenderDictionary[cachedDescription.Name];
		bool nameChanged = false;
		bool ipChanged = false;
		bool portChanged = false;

		if(cachedDescription.Name != description.Name)
			nameChanged = true;
		if(cachedDescription.Port != description.Port)
			portChanged = true;
		if(cachedDescription.HostAddress != description.HostAddress)
			ipChanged = true;

		if(nameChanged){
			Server_Array_Error err = existsInArray(description.Name, description.HostAddress, description.Port);
			switch(err)
			{
				case Server_Array_Error.DuplicateServerName: 
				{
					Debug.LogError("could not use \'" + description.Name + "\' as the server name, it already exists in the array.");
					description.Name = cachedDescription.Name;
					break;
				}
				case Server_Array_Error.DuplicateIPOrPort: 				
					//fall through, it's ok if ip's the same if you modify the name- that's expected. 				
				case Server_Array_Error.NoErr: 
				{
					//you already changed the name...just reconnect plz.
					description.connectSender();
//					Debug.Log ("WINNAR: reconnecting with new NAME: " + description.Name);
					setKeysToSwap(cachedDescription.Name, description.Name);
					//todo: make a custom method to call, if valid.
					break;
				}
			}
		}
		if(ipChanged){
			Server_Array_Error err = existsInArray(description.Name, description.HostAddress, description.Port);
			switch(err)
			{
				case Server_Array_Error.DuplicateIPOrPort: 
				{
					Debug.LogError("could not edit IP/port of server \'" + cachedDescription.Name + "\'- that IP/port already exists in the array.");						
					description.HostAddress = cachedDescription.HostAddress;
					break;
				}
				case Server_Array_Error.DuplicateServerName: 				
					//fall through, it's ok if name is the same.
				case Server_Array_Error.NoErr: 
				{
//					Debug.Log ("reconnecting with new host address: " + description.HostAddress);
					description.connectSender();
					break;
				}
			}
		}
		if(portChanged){
			Server_Array_Error err = existsInArray(description.Name, description.HostAddress, description.Port);
			switch(err)
			{
			case Server_Array_Error.DuplicateIPOrPort: 
			{
				Debug.LogError("could not edit IP/port of server \'" + cachedDescription.Name + "\'- that IP/port already exists in the array.");						
				description.Port = cachedDescription.Port;
				break;
			}
			case Server_Array_Error.DuplicateServerName: 				
				//fall through, it's ok if name is the same.
			case Server_Array_Error.NoErr: 
			{
//				Debug.Log ("WINNAR: reconnecting with new Port: " + description.Port);
				description.connectSender();
				break;
			}
			}
		}
	}

	public void DrawCustomAddressLabelGeneric(SerializedProperty elementProperty, string addOrX, Action<int> methodOnAddOrX, int propertyIndex){
		SerializedProperty nameProp = elementProperty.FindPropertyRelative("Name");
		SerializedProperty addressProp = elementProperty.FindPropertyRelative("HostAddress");
		SerializedProperty portProp = elementProperty.FindPropertyRelative("Port");

		bool isEditMode = false;

		if(propertyIndex == editingLabelIdx && isEditingIdx){
			isEditMode = true;
		}
		Rect drawZone = GUILayoutUtility.GetRect(50f, 16f);
		Rect drawZone2 = drawZone;
		drawZone2.width = 50;
		if(GUI.Button (drawZone2, addOrX)){
			methodOnAddOrX(propertyIndex);
			return;
		}
		drawZone.x +=50;
		float w = drawZone.width;
		drawZone.width = 40;

		if(isEditMode){
			if(GUI.Button (drawZone, "Done")){
//				editingLabelIdx = labelIdx;
				isEditingIdx = false;
			}
		}
		else{
			if(GUI.Button (drawZone, "Edit")){
				editingLabelIdx = propertyIndex;
				isEditingIdx = true;
			}
		}


		drawZone.width = w;
		drawZone.x += 30;
		
		if(isEditMode){
			EditorGUI.BeginChangeCheck();

			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));
			if(userHitReturn)
				isEditingIdx = false;

			string prevName = nameProp.stringValue;
			EditorGUI.PropertyField(drawZone, nameProp, GUIContent.none);
			if(GUI.changed){
				Server_Array_Error err = existsInArray(nameProp.stringValue, addressProp.stringValue, portProp.intValue);
				switch(err){
					case Server_Array_Error.DuplicateServerName: 
					{
						Debug.LogError("could not use \'" + nameProp.stringValue + "\' as the server name, it already exists in the array.");
						nameProp.stringValue = prevName;
						break;
					}
					case Server_Array_Error.DuplicateIPOrPort: 				
						//fall through, it's ok if ip's the same if you only modify the name- that's expected. 				
					case Server_Array_Error.NoErr: 
					{
						//todo: make a custom method to call, if valid.
						break;
					}
				}
			}
			
			EditorGUI.EndChangeCheck();

		}
		else{
			EditorGUI.LabelField(drawZone, nameProp.stringValue);
//			EditorGUI.LabelField(drawZone, serverName);
		}
		
		drawZone = GUILayoutUtility.GetRect(50f, 16);
		drawZone.width += 20;
		drawZone.y +=5;
		drawZone.height +=1;
		EditorGUI.LabelField(drawZone, "IP:");
		drawZone.x +=20;
		drawZone.width = 120;
		if(isEditMode){
			EditorGUI.BeginChangeCheck();
			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));			
			if(userHitReturn)
				isEditingIdx = false;
			string prevAddr = addressProp.stringValue;
			EditorGUI.PropertyField(drawZone, addressProp,GUIContent.none);

			if(GUI.changed){
				Server_Array_Error err = existsInArray(nameProp.stringValue, addressProp.stringValue, portProp.intValue);
				switch(err){
					case Server_Array_Error.DuplicateIPOrPort: 	{
					Debug.LogError("could not edit IP/port of server \'" + nameProp.stringValue + "\'- that IP/port already exists in the array.");						
						addressProp.stringValue = prevAddr;
						break;
					}
					case Server_Array_Error.DuplicateServerName: 
					case Server_Array_Error.NoErr: 
					{
						//TODO: make custom function to call when valid.
						break;
					}
				}
			}
			
			EditorGUI.EndChangeCheck();
		}
		else
		{
			EditorGUI.LabelField(drawZone, addressProp.stringValue);
		}

		drawZone.x +=110;
		EditorGUI.LabelField(drawZone, "Port:");
		drawZone.x +=30;
		drawZone.width = 70;
		if(isEditMode){
			EditorGUI.BeginChangeCheck();

			bool userHitReturn = (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return));
			if(userHitReturn)
				isEditingIdx = false;

			int prevPort = portProp.intValue;
			EditorGUI.PropertyField(drawZone, portProp,GUIContent.none);
			if(GUI.changed){
				Server_Array_Error err = existsInArray(nameProp.stringValue, addressProp.stringValue, portProp.intValue);
				switch(err){
					case Server_Array_Error.DuplicateIPOrPort: 	{
						Debug.LogError("could not edit server, that port/IP combo already exists in the array.");						
						portProp.intValue = prevPort;
						break;
					}
					case Server_Array_Error.DuplicateServerName: 
					case Server_Array_Error.NoErr: 
					{
//						sender.oscSendersPublicInterface[labelIdx].Port = port;
						break;
					}
				}
			}
			
			EditorGUI.EndChangeCheck();
		}
		else{
			EditorGUI.LabelField(drawZone, portProp.intValue.ToString());
		}
		GUILayout.Space (6);
	}

	public void RemovePropertyFromList(int i){
		oscSendersPublicInterface.DeleteArrayElementAtIndex(i);
	}


	public void RemoveSenderFromDictionaryInPlayMode(OscServerDescription descrip){
		foreach(KeyValuePair<string,OscSenderDescription> iterDescription in sender.oscSenderDictionary){
			if(iterDescription.Key == descrip.Name){
				iterDescription.Value.disconnectSender();
				setKeysToSwap(iterDescription.Key, "");
			}
		}
	}
	
	public void DrawIPAddressesPlayMode(){
		Color defaultColor = GUI.backgroundColor;
		GUI.backgroundColor = addSenderColor;
		EditorGUI.indentLevel++;
		//		Rect drawZone;
		EditorGUILayout.HelpBox("Add a Sender: ",MessageType.None);
//		if(UnityBonjourBrowser.oscServerStrings.Length > 0){
			GUILayout.BeginHorizontal();
			if(GUILayout.Button ("add", GUILayout.Width (50))){
				AddBonjourServerToOscSendersPlayMode();
			}
			selectedBonjourBrowserPopupIndex = Mathf.Clamp(selectedBonjourBrowserPopupIndex, 0, UnityBonjourBrowser.oscServerStrings.Length-1);
			GUILayout.Label ("Bonjour server:");
			Rect drawPopup = GUILayoutUtility.GetRect(0f, 16);
			drawPopup.x = 143;
			drawPopup.width = 150;
			selectedBonjourBrowserPopupIndex = EditorGUI.Popup(drawPopup, selectedBonjourBrowserPopupIndex, UnityBonjourBrowser.oscServerStrings);
			GUILayout.EndHorizontal();
//		}
		GUILayout.Space(5);
		
		DrawCustomAddressEdit(customDescription, "add", AddCustomServerToOscSendersPlayMode, true, false);
		
		GUILayout.Space(4);
		Rect drawLatestDerp = GUILayoutUtility.GetRect(0f, 1);
		GUI.backgroundColor = defaultColor;

		GUI.Box(drawLatestDerp, "");
		GUI.backgroundColor = senderListColor;

		if(sender.oscSenderDictionary.Count == 0){
			EditorGUILayout.HelpBox("Not sending to any OSC servers! Please add one.",MessageType.Warning);
		}
		else
			EditorGUILayout.HelpBox("Senders:",MessageType.None);

		foreach(KeyValuePair<string,OscSenderDescription> descript in sender.oscSenderDictionary){
			DrawCustomAddressEdit((OscServerDescription)descript.Value, "X", RemoveSenderFromDictionaryInPlayMode, false, true, OnEditModeCompleteSoCheckForCachedDescriptionChanges);
		}
		swapKeys();

		EditorGUI.indentLevel--;
	}

	public void swapKeys(){
		if(needsKeySwap){
			needsKeySwap = false;
			if(sender.oscSenderDictionary.ContainsKey(prevKey)){
				OscSenderDescription descrip = sender.oscSenderDictionary[prevKey];
				sender.oscSenderDictionary.Remove(prevKey);
				if(!String.IsNullOrEmpty(newKey))
					sender.oscSenderDictionary.Add(newKey, descrip);
			}
		}
	}

	public void setKeysToSwap(string prevK, string newK){
		needsKeySwap = true;
		prevKey = prevK;
		newKey = newK;
	}

}


