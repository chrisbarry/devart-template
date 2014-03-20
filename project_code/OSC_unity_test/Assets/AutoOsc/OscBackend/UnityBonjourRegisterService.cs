using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnityBonjourRegisterService : MonoBehaviour {

	private OscReceiverDescription receiveController;

	//make sure you set the receiveController once you instantiate this object.
	public OscReceiverDescription ReceiveController{
		get { return receiveController; }
		set { receiveController = value; }
	}

	private short port = 8000;
	public short Port {
		get { return (short)port; }
		set {
			//
			if(value != port){
//				Debug.Log ("changing port to + " + value);
				port = (short)value;
				RefreshRegistration();
			}
		}
	}

	public Mono.Zeroconf.RegisterService regService;
	private string regDomain = "local.";
	private string regType = "_osc._udp.";
	
	void Start()
	{
		if(!Application.HasProLicense())
		{
			Debug.LogError ("UnityBonjour only works if you have a pro license. Sorry!");
			Destroy (this.gameObject);
		}

		this.gameObject.hideFlags = HideFlags.HideInInspector;
		this.gameObject.hideFlags = HideFlags.HideInHierarchy;
	}

	#if UNITY_EDITOR
	private void Update(){
		//prevent leaks in editor when editing/saving code, by forcing play mode to stop when compiling.
		if(EditorApplication.isCompiling) {
			EditorApplication.isPlaying = false;
		}
	}
	#endif

	private void OnDisable(){
		Stop();
	}

	public void Stop(){
		if(regService != null){
			regService.Stop();
			regService = null;
		}
	}
			
	public void RefreshRegistration(){
		Stop();
		StopAllCoroutines();
		StartCoroutine(Register(0.5f));
	}

	private IEnumerator Register(float wait){
		yield return new WaitForSeconds(wait);
		regService = new Mono.Zeroconf.RegisterService();
		regService.Name ="Unity." + System.Net.Dns.GetHostName()+"."  + receiveController.Name; //the receiveController's gameObject's name should be guaranteed to be unique.
		regService.ReplyDomain = regDomain;
		regService.RegType = regType;
		regService.Port = (short)port;
		regService.Register();
	}

}
