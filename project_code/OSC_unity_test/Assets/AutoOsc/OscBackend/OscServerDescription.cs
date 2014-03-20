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
public class OscServerDescription{
	//if the Name is the first property here, it's a nice little hack to get the array names to match.
	[SerializeField]
	public string Name = "";
	[SerializeField]
	public string HostAddress = "";
//	[SerializeField, HideInInspector]
//	private bool isABonjourServer = false;


	//convenience method to get the HostAddress as IP
	public IPAddress HostAddressAsIP{
		get {
			IPAddress outAddr;
			if(IPAddress.TryParse(HostAddress, out outAddr)){
				return outAddr;
			}
			Debug.LogError (HostAddress + " is not a valid ip address!");
			return IPAddress.Parse("0.0.0.0");
		}
	}
	
	[SerializeField]
	public int Port; 
	
	public OscServerDescription(){}
	
	public OscServerDescription(string n, string host, int p, bool isBonjour = false ){
		Name = n;
		HostAddress = host;
		Port = p;
//		isABonjourServer = isBonjour;
	}
	
}