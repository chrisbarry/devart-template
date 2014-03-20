using UnityEngine;
using System.Collections;

public class SendOscExample : MonoBehaviour {
	
	private Vector3 m_LastPosition; 

	public string senderName1 = "BCsWAT VDMX 1";
	private string senderName2 = "another destination";

	void Start () {
		//AddSender takes a OscServerDescription, which has the following arguments:
		//a string that serves as its unique id to be used in AutoOscSender.Send(),
		//the ip address to send to,
		//and the port to send to.
//		AutoOscSender.AddSender(new OscServerDescription(senderName2, "127.0.0.1", 5000));

		//alternatively, you can add a bonjour server name, and when Unity finds that Bonjour service has come online, it should automatically get the ip/port and connect.
		//3rd alternative- you may use the GUI in the editor inspector which does the same thing- anything added in the GUI will get added as a sender.
//		AutoOscSender.AddSender(senderName1);
	}
		
	// Update is called once per frame
	void Update () {	
		//you can easily send to multiple senders by just inserting a different string name to .Send()
		//the controller names are stored in AutoOscSender.oscSenderDictionaryNames (and are just the .Keys of oscSenderDictionary)
		AutoOscSender.Send(senderName1, new Rug.Osc.OscMessage("/test", Random.Range (0.0f, 100.0f)));

		//here's an example of sending the transform position vector, to a different ip/port etc
		AutoOscSender.Send(senderName1, new Rug.Osc.OscMessage("/rotation", transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));
	}
}
