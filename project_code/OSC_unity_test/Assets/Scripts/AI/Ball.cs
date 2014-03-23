using UnityEngine;
using System.Collections;

public class Ball{


	public Vector3 location = new Vector3();
	public Vector3 destination = new Vector3();

	public float speed = 0f;

	// Use this for initialization
	public GameObject ball; 
	public string name;
	public StateMachine brain = new StateMachine();
	public bool moving = false;
	public Rigidbody Rigidbody;


	public Ball(string name, Vector3 position){
		this.speed = 10f;
		this.destination = position;
		this.name = name;
		this.brain.AddState(new BallStateAimless(this));
		this.brain.SetState("aimless");
		this.ball = (GameObject)MonoBehaviour.Instantiate(Resources.Load("Prefabs/Sphere"),position,new Quaternion());
		this.location = position;
		this.Rigidbody = this.ball.GetComponent<Rigidbody>();
	}

	public void Process(float timePassed){
		this.brain.Think();

		if (this.speed > 0f && this.location != this.destination){
			Vector3 heading = this.destination - this.location;
			var distance = heading.magnitude;
			var direction = heading / distance;
			var travel_distance = Mathf.Min(distance, timePassed * this.speed);
			this.location += travel_distance * direction;
			this.ball.transform.position = this.location;
			this.location = this.ball.rigidbody.transform.position;
		}
	}
	
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
