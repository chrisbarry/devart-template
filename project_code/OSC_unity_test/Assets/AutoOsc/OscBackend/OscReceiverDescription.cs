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
using Mono.Zeroconf;
using System.Linq;
using Rug.Osc;
[Serializable]
public class OscReceiverDescription : OscServerDescription{

	private bool hasEverBeenConnected = false;

	[SerializeField]
	public bool registerWithBonjour = true;
	[SerializeField]
	public bool useMulticast = false;
	[SerializeField]
	public int MaxMessagesToProcessPerFrame = 10;
	public Rug.Osc.OscNamespaceManager Manager { 
		get 
		{
			if(m_Manager == null)
				m_Manager = new Rug.Osc.OscNamespaceManager();
			return m_Manager; 
		} 
	}
	private UnityBonjourRegisterService registerService;


	//for trying a different port - will attempt port+1 up to 5 times and then find a random port.
	private const int maxConnectRetries = 5;
	private int currConnectRetries = 0;


	private Rug.Osc.OscReceiver m_Receiver;

	// Namespace manager instance
	private Rug.Osc.OscNamespaceManager m_Manager; 

	public OscReceiverDescription(string theName, int receiverPort,bool registerBonjourService, bool multi = false, string multiAddr = ""){
		//force no bonjour use if no pro or platform doesn't support it
		registerWithBonjour = registerBonjourService;
		if(!AutoOscReceiver.supportsBonjour()){
			registerWithBonjour = false;
		}

		if(String.IsNullOrEmpty(multiAddr)){
			multiAddr = "239.0.0.222";
		}
		Name = theName;
		Port = receiverPort;
		useMulticast = multi;
		HostAddress = multiAddr;
	}

	public void connectReceiver(){
		if(m_Manager == null)
			m_Manager = new Rug.Osc.OscNamespaceManager();

		currConnectRetries = 0;
		Connect();
	}

	void nowConnected(){
		hasEverBeenConnected = true;

		if(registerWithBonjour){
			GameObject regServiceGO = new GameObject();
			regServiceGO.name = "BonjourService" + Name;
			registerService = regServiceGO.AddComponent<UnityBonjourRegisterService>();
			if(registerService){
				registerService.ReceiveController = this;
				registerService.Port = (short)Port;
				registerService.RefreshRegistration();
			}
		}
	}

	private void Connect(){
		// Ensure that the receiver is disconnected
		if(hasEverBeenConnected)
			disconnectReceiver(); 		
		
		if(currConnectRetries == maxConnectRetries){
			Debug.LogError("AutoOscReceiver: max retries connecting- connecting with dynamic port.");
			currConnectRetries++;
			Port = 0;
		}
		else if(currConnectRetries > maxConnectRetries){
			Debug.LogError("AutoOscReceiver: no more retries. could not connect. network issues?");
			return;
		}
		
		
		currConnectRetries++;
		// The address to listen on to 
		IPAddress address = IPAddress.Any; 		
		// The port to listen on 
		int thePort = Port;	
		
		IPAddress multiAddress;
		
		//if we could parse the IP, the ip is a multi address, and multicast is actually enabled
		if(IPAddress.TryParse(HostAddress, out multiAddress) && OscSocket.IsMulticastAddress(multiAddress) && useMulticast){
			m_Receiver = new OscReceiver(address, multiAddress,thePort);
		}
		else{
			m_Receiver = new OscReceiver(address,thePort);
		}
		// Connect the receiver
		try{
			m_Receiver.Connect ();
			if(Port == 0){
				Port = m_Receiver.LocalPort;
			}
			// We are now connected, fire anything that has to happen to the osc receiver while connected.
			nowConnected();
			//			Debug.Log ("Connected Receiver"); 
		}
		catch (System.Net.Sockets.SocketException e) {
			if(e.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse){
				Debug.LogError("Exception on OSC start! port " + Port + " already bound. Trying again at port '"+ (Port+1) +"'... error: " + e.ToString ());
				Port++;
				Connect();
			}
		}
		
	}

	//disconnects receiver, does not destroy registered methods.
	public void disconnectReceiver () {		
		// If the receiver exists
		if (m_Receiver != null) {			
			// Disconnect the receiver
			//			Debug.Log ("Disconnecting Receiver"); 			
			m_Receiver.Dispose (); 			
			// Nullifiy the receiver 
			m_Receiver = null;
		}
	
		if(registerService){
			MonoBehaviour.Destroy(registerService.gameObject);
		}

	}

	//disconnects and destroys registered methods.
	public void Destroy(){
		disconnectReceiver();
		if(m_Manager != null){
			m_Manager.Dispose (); 
			m_Manager = null;
		}
	}

	// Update is called once per frame
	public void Update () {
		
		#if UNITY_EDITOR
		//prevent leaks in editor when editing/saving code, by forcing play mode to stop when compiling.
		if(UnityEditor.EditorApplication.isCompiling) {
			UnityEditor.EditorApplication.isPlaying = false;
		}
		#endif
		
		int i = 0; 
		
		// if we are in a state to recieve
		while (i++ < MaxMessagesToProcessPerFrame && m_Receiver != null && 
		       m_Receiver.State == OscSocketState.Connected)
		{
			OscPacket packet;
			
			// get the next message this will not block
			if (m_Receiver.TryReceive(out packet) == false) 
			{
				return; 
			}
			if(m_Manager == null){
//				Debug.Log ("manager's null: " + Name + " " + Port);
				return;
			}
			
			switch (m_Manager.ShouldInvoke(packet))
			{
			case OscPacketInvokeAction.Invoke:
				// Debug.Log ("Received packet");
				m_Manager.Invoke(packet);
				break;
			case OscPacketInvokeAction.DontInvoke:
				Debug.LogWarning ("Cannot invoke");
				Debug.LogWarning (packet.ToString()); 
				break;
			case OscPacketInvokeAction.HasError:
				Debug.LogError ("Error reading osc packet, " + packet.Error);
				Debug.LogError (packet.ErrorMessage);
				break;
			case OscPacketInvokeAction.Pospone:
				Debug.Log ("Postponed bundle");
				Debug.Log (packet.ToString()); 
				break;
			default:
				break;
			}											
		}
	}	


}