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
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class UnityBonjourBrowser : MonoBehaviour {

	private uint @interface = 0;
	private AddressProtocol address_protocol = AddressProtocol.IPv4;
	protected string domain = "local.";
	protected string type = "_osc._udp";
	private bool resolve_shares = true;
	private ServiceBrowser browser;

	public List<OscServerDescription> availableOscServers = new List<OscServerDescription>();
	[HideInInspector]
	public static string[] oscServerStrings = new string[0];

	public delegate void BonjourServiceNotificationHandler(OscServerDescription description);
	public static event BonjourServiceNotificationHandler BonjourServiceAdded;
	public static event BonjourServiceNotificationHandler BonjourServiceRemoved;

	protected static UnityBonjourBrowser _instance = null;
	public static UnityBonjourBrowser Instance
	{
		get { 

			if(_instance == null)
			{
				GameObject zcfnInstance = new GameObject();
				zcfnInstance.name = "__UnityBonjourBrowser Instance";
				_instance = zcfnInstance.AddComponent<UnityBonjourBrowser>();
			}

			return _instance;		
		}
	}




//	void Awake()
//	{
//	}
	
	// Use this for initialization
	void OnEnable() {
		if(!Application.HasProLicense())
		{
			Debug.LogError ("UnityBonjour only works if you have a pro license. Sorry!");
			DestroyImmediate (this);
		}
		
		if (_instance != null && _instance != this)
		{
			Debug.Log ("DESTROYING! you can only have one bonjour browser at a time!");
			DestroyImmediate(this);
			return;
		}
		_instance = this;

		availableOscServers = new List<OscServerDescription>();
		browser = new ServiceBrowser();
		browser.ServiceAdded += OnServiceAdded;
		browser.ServiceRemoved += OnServiceRemoved;		
		//		browser.Browse (type, "local.");
		browser.Browse (@interface, address_protocol, type, domain);		
//		Debug.Log ("starting the service browser.");
	}

	private void OnDisable(){
		if(browser != null){
			browser.Stop ();
			browser = null;
		}
	}

//
//	public static void RefreshServerIPs(){
//		UnityBonjourBrowser.Instance.RefreshIPs();
//	}
//
//	private void RefreshIPs(){
//		foreach(IResolvableService svc in browser){
//				svc.Resolve(true);
//		}
//	}

	
	private void OnServiceAdded(object o, ServiceBrowseEventArgs args)
	{
		System.Console.WriteLine("*** SERVICE ADDED name: " +  args.Service.Name+ 
		          ", type: " + args.Service.RegType + 
		          ", domain: " + args.Service.ReplyDomain + 
		          ", addr protocol: " + args.Service.AddressProtocol +
		          ", serviceName: " + args.Service.Name +
		          ", servicePort: "  + args.Service.Port + 
		          ", netinterface: "  + args.Service.NetworkInterface
		          );
		
		if(resolve_shares) {
			args.Service.Resolved += OnServiceResolved;
			args.Service.Resolve(true);
		}
	}

	#if UNITY_EDITOR
	private void Update(){
		//prevent leaks in editor when editing/saving code, by forcing play mode to stop when compiling.
		if(EditorApplication.isCompiling) {
			EditorApplication.isPlaying = false;
		}
	}
	#endif


	public void addServer(ServiceResolvedEventArgs args){
		OscServerDescription srv = getOscServerDescriptionWithName(args.Service.Name);
		if(srv == null){
			if(args.Service.HostEntry != null && args.Service.HostEntry.AddressList.Length > 0){
				srv = new OscServerDescription(args.Service.Name, args.Service.HostEntry.AddressList[0].ToString (), (int)args.Service.Port, true);
				availableOscServers.Add(srv);
			}
		updateServerStringNames();
		}

		if(BonjourServiceAdded != null){
			BonjourServiceAdded(srv);
		}
	}

	public OscServerDescription getOscServerDescriptionWithName(string name){
		foreach(OscServerDescription server in availableOscServers){
			if(server.Name == name){
				return server;
			}
		}
		return null;
	}
	
	public void removeServer(ServiceBrowseEventArgs args){
		OscServerDescription srv = getOscServerDescriptionWithName(args.Service.Name);

		if(BonjourServiceRemoved != null && srv != null){
			BonjourServiceRemoved(srv);
		}

		if(srv != null){
				availableOscServers.Remove(srv);
		}

		updateServerStringNames();
	}

	public void updateServerStringNames(){
		oscServerStrings = availableOscServers.Select(m => m.Name).ToArray();
	}


	private void OnServiceRemoved(object o, ServiceBrowseEventArgs args)
	{        
		System.Console.WriteLine("*** SERVICE REMOVED name: " +  args.Service.Name+ 
		          ", type: " + args.Service.RegType + 
		          ", domain: " + args.Service.ReplyDomain + 
		          ", addr protocol: " + args.Service.AddressProtocol +
		          ", serviceName: " + args.Service.Name +
		          ", servicePort: "  + args.Service.Port + 
		          ", netinterface: "  + args.Service.NetworkInterface
		          );

		removeServer(args);
	}

	private void OnServiceResolved(object o, ServiceResolvedEventArgs args)
	{

//		string details = args.Service.FullName + " " +  args.Service.HostEntry.AddressList[0] + " " +  args.Service.HostEntry.HostName + " " + args.Service.Port + " " + 
//			args.Service.NetworkInterface + " " + args.Service.AddressProtocol;
//		Debug.Log (details);


		addServer(args);
		//saved for posterity...
//		ITxtRecord record = args.Service.TxtRecord;
//		int record_count = record != null ? record.Count : 0;
//		if(record_count > 0) 
//		{
//			for(int i = 0, n = record.Count; i < n; i++) 
//			{
//				TxtRecordItem item = record.GetItemAt(i);
//				Debug.Log("{0} = '{1}'" + " " + item.Key + " " + item.ValueString);
//				if(i < n - 1) {
//					Debug.Log(", ");
//				}
//			}
//		}


	}





}
