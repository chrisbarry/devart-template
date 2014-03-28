using UnityEngine;
using System.Collections;

public class BallActions : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	public void Flash(){
		StartCoroutine (FlashAction (1f));
	}

	IEnumerator FlashAction(float duration)		
	{
		
		//This is a coroutine
		
		Debug.Log("Start Wait() function. The time is: "+Time.time);
		
		Debug.Log( "Float duration = "+duration);

		var oldMaterial = this.transform.renderer.material;
		this.transform.renderer.material.color = Color.green;

		MeshRenderer gameObjectRenderer = this.GetComponent<MeshRenderer>();		
		Material newMaterial = new Material(Shader.Find("Diffuse"));
		newMaterial.color = Color.green;
		gameObjectRenderer.material = newMaterial;
		yield return new WaitForSeconds(duration);   //Wait

		gameObjectRenderer.material = oldMaterial;

		Debug.Log("End Wait() function and the time is: "+Time.time);
		
	}
}
