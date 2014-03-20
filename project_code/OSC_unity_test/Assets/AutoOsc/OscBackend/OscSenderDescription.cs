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

[Serializable]
public class OscSenderDescription : OscServerDescription{
	private Rug.Osc.OscSender m_oscSender;
	
	//hasHostIP is false when you register a server with simply a name, and expect to receive an IP/Port from Bonjour
	private bool hasHostIP = false;
	#region Constructor
	public OscSenderDescription(string n){
		Name = n;
		hasHostIP = false;
	}	
	public OscSenderDescription(OscServerDescription serverDescrip){
		Name = serverDescrip.Name;
		HostAddress = serverDescrip.HostAddress;
		Port = serverDescrip.Port;
		hasHostIP = true;
	}
	public OscSenderDescription(string n, string host, int p){
		Name = n;
		HostAddress = host;
		Port = p;
		hasHostIP = true;
	}
	#endregion

	public void RefreshBackingDescription(OscServerDescription serverDescrip){
		Name = serverDescrip.Name;
		HostAddress = serverDescrip.HostAddress;
		Port = serverDescrip.Port;
		hasHostIP = true;
	}

	public Rug.Osc.OscSender oscSender{
		get { return m_oscSender; }
		set { m_oscSender = value; }
	}

	public void connectSender(){
		disconnectSender (); 
		// The address to send to 
		// Create an instance of the sender
		if(hasHostIP){
//			Debug.Log ("CONNECTING TO" + Name + ", : " + HostAddress);
			m_oscSender = new Rug.Osc.OscSender(HostAddressAsIP, 0, Port);
			// Connect the sender
			m_oscSender.DisconnectTimeout = 10;
			m_oscSender.Connect();
		}
	}

	public void disconnectSender () {	

		// If the sender exists
		if (m_oscSender != null) {
//			Debug.Log ("DISCONNECTING FROM" + Name + ", : " + HostAddress);
			m_oscSender.Dispose (); 
			// Nullifiy the sender 
			m_oscSender = null;
		}
	}
}