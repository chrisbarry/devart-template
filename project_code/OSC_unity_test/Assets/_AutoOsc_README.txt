AutoOsc for Unity beta by Brian Chasalow, 2013 
License: AutoOsc_LICENSE.txt
bugs/donations? Please email brian@chasalow.com

//Release notes:
v.0.8.1: renaming/structural/documentation fix, and 1 minor bugfix for editor gui
v.0.8.0: functional on OSX (Unity Pro) and iOS (Unity Basic)

Currently supports OSX (Unity Pro) and iOS (Unity Basic)
Osc Backend powered by a fork of Rug.OSC from https://bitbucket.org/brianchasalow/rug.osc
Bonjour powered by a fork of Mono.ZeroConf, incredibly-far-forked from the source repo, so maintained as part of this repo.
You may choose to send and receive to many or just one destinations simultaneously. Optionally supports multicast.

INSTRUCTIONS:
----------
Open the unitypackage in Unity Pro and import it into a new or existing project.
Go to build settings/player settings/api compatibility level and set it to '.net 2.0' not '.net 2.0 subset' or you will get build errors.

SENDING:
----------
//first, register a sender. there are 3 ways to do it:
1) from code: AutoOscSender.AddSender(new OscServerDescription("mySenderName", "127.0.0.1", 5000));
2) from code, if you are expecting a bonjour server: AutoOscSender.AddSender("mySenderName"); //(mySenderName would be the name your other app registers with Bonjour)
3) from the AutoOscSender GUI inspector: fill out your server info and click 'Add'.
        If a bonjour server is already online, you may click 'add' from the drop-down and it will automatically fill in the ip/port.

//Now, send some stuff!       
AutoOscSender.Send(senderName1, new Rug.Osc.OscMessage("/test", Random.Range (0.0f, 100.0f)));


RECEIVING:
----------
//first, register a receiver. there are 3 ways to do it: 
1) from code: AutoOscReceiver.AddReceiver(controllerName, 5001, true); 
2) from code: AutoOscReceiver.AddReceiver(controllerName, 5001, 
													false, //whether or not you want to register this name/port as a bonjour service.
													true, //optionally set isMulticast true if you want to receive multicast packets.
													"239.0.0.222"); //if isMulticast is true, set your multicast group to join here. 
													                //must be within specific ranges specified by multicast spec
3) from the AutoOscReceiver GUI inspector: fill out your server info here.
//now register some methods (i.e., ReceiveColor) to receive your 'osc address patterns' (i.e., /vdmx/colorPlz) in Start() and OnDestroy().
// AutoOscReceiver.RegisterReceiveMethod takes as parameters:
 //[string] the controllerName passed in to AutoOscReceiver.AddReceiver,
 //[string] the address pattern you want to receive,
 //the delegate method to call when your address pattern is received- this must be a function that takes OscMessage as a parameter.


void Start(){
		AutoOscReceiver.RegisterReceiveMethod(controllerName, "/vdmx/colorPlz", ReceiveColor); 
	}
void OnDestroy(){	
		AutoOscReceiver.UnregisterReceiveMethod(controllerName, "/vdmx/colorPlz", ReceiveColor); 
}
//and make sure the function you're passing takes an OscMessage as a parameter, and you parse it like so:
public void ReceiveColor (OscMessage message) {		
	if(message[0] is OscColor){
		OscColor col = (OscColor)message[0];
		Color32 color = new Color32((byte)col.R,(byte) col.G, (byte)col.B,(byte) col.A);
		renderer.material.color = color;
	}
}
//see ReceiveOscExample.cs for more examples of how to parse bool, int, float, string, Vector3 (or an arbitrary # count), etc.
//To recap/TL;DR: call AutoOscReceiver.AddReceiver and AutoOscReceiver.RegisterReceiveMethod in Start(), 
//call AutoOscReceiver.UnregisterReceiveMethod in OnDestroy(), and define a callback like ReceiveColor.

//TODO: 
Should fallback gracefully to no bonjour support if not using Pro, while still supporting OSC features. Needs Windows testing also.



