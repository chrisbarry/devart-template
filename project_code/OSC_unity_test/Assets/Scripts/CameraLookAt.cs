using UnityEngine;
using System.Collections;

public class CameraLookAt : MonoBehaviour {

	public GameObject target = null;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (target != null)
		{
			transform.LookAt(target.transform);
			transform.RotateAround(target.transform.position, Vector3.up, Time.deltaTime*10);
		}
	}
}
