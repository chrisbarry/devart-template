using System.Collections;
using UnityEngine;
using Rug.Osc;
using System.Reflection;

public class ReceiveOscExample : MonoBehaviour {
	private string controllerName = "Unity Receiver"; 
	// Use this for initialization
	GameObject cube = null;

	public void Start () {
		AutoOscReceiver.AddReceiver(controllerName, 5001, true);

		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/vdmx/colorPlz", ReceiveColor); 
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/test/pos", ReceivePosition); 
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/test", gotSomething);
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/vdmx/string", ReceiveString); 
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/vdmx/disIsABool", ReceiveBool);
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/vdmx/sendFloat", ReceiveFloat);
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/vdmx/reloadLevel", ReloadLevel);
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/FromVDMX", DebugMessage);
		//this.cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
	}
	
	void OnDestroy () {
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/vdmx/colorPlz", ReceiveColor); 
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/test/pos", ReceivePosition);
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/test", gotSomething);
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/vdmx/string", ReceiveString);
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/vdmx/disIsABool", ReceiveBool);
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/vdmx/sendFloat", ReceiveFloat);
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/vdmx/reloadLevel", ReloadLevel);
	}
	public void ReloadLevel(OscMessage message){
		Debug.Log ("reloading level...");
		Application.LoadLevel(Application.loadedLevelName);
	}

	public void DebugMessage(OscMessage message){
		if (message[0] is float) {
			//Debug.Log ("float is " + message[0]);
			this.cube.transform.position = new Vector3((float)message[0]*100, 0.5f, 0f);
			this.cube.transform.localScale = new Vector3 (1.25f, 1.5f, 1f);
		}
	}
	public void gotSomething(OscMessage msg){
//		Debug.Log ("got something: " + msg[0]);
	}

	public void ReceiveString(OscMessage message){
		if(message[0] is string){
			Debug.Log ("string is " + message[0]);
		}
	}

	public void ReceiveFloat(OscMessage message){
		if(message[0] is float){
			Debug.Log ("float is " + message[0]);
		}
	}

	public void ReceiveInt(OscMessage message){
		if(message[0] is int){
			Debug.Log ("int is " + message[0]);
		}
	}

	public void ReceiveBool(OscMessage message){
		if(message[0] is bool){
			Debug.Log ("bool is " + message[0]);
		}
	}

	public void ReceiveColor (OscMessage message) {		
		if(message[0] is OscColor){
			OscColor col = (OscColor)message[0];
			Color32 color = new Color32((byte)col.R,(byte) col.G, (byte)col.B,(byte) col.A);
			renderer.material.color = color;
		}
	}

	public void ReceivePosition (OscMessage message) {
	
//		Debug.Log("Receive position"); 	
		// get the position from the message arguments 
		float x = (float)message [0];
		float y = (float)message [1];
		float z = (float)message [2];
		
		// assign the transform position from the x, y, z
		this.gameObject.transform.position = new Vector3 (x + 5, y + 5, z); 
	}

}
