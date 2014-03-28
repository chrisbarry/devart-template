using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ball{


	public Vector3 location = new Vector3();
	public Vector3 destination = new Vector3();

	public float speed = 0f;

	// Use this for initialization
	public GameObject ballGameObject; 
	public string name;
	public StateMachine brain = new StateMachine();
	public bool moving = false;
	public Rigidbody rigidbody;
	public GameObject baseSpline;
	public CurvySpline spline;
	public SplineWalker walker;
	public Main main;
	public BallActions ballActions;
	public Ball target; //the next in the chain
	public bool hasfollower;
	public List<Ball> followers;
	public bool colourSet = false;
	public Ball leader; // the front of the line
	public ParticleSystem ps;
	public Light light;
	public CornerHolder currentCorner;
	public int beatsWhileHeld = 0;

	public bool resetspline = false;

	public void SetupSplineAndDoIt(){
		//baseSpline.transform.position = new Vector3(0,0,0);
		walker.Spline = this.spline;

		spline.Clear();	

		var controlPoints = new Vector3[]{
			this.ballGameObject.transform.position, 
			new Vector3(Random.Range(-10,10),this.ballGameObject.transform.position.y,Random.Range(-10,10)),
			new Vector3(Random.Range(-10,10),this.ballGameObject.transform.position.y,Random.Range(-10,10)),
			new Vector3(Random.Range(-10,10),this.ballGameObject.transform.position.y,Random.Range(-10,10))
		};


		spline.ControlPoints.Clear();

		spline.Add (controlPoints);
		walker.TF = 0.0f;

	}

	public Ball(string name, Vector3 position){
		this.followers = new List<Ball>();
		this.hasfollower = false;




		this.main = GameObject.Find("Main Camera").GetComponent<Main>();
		this.speed = 10f;
		this.destination = position;
		this.name = name;
		this.brain.AddState(new BallStateAimless(this));
		this.brain.AddState (new BallStateRandom4Point (this));
		this.brain.AddState (new BallStateAvoid (this));
		this.brain.AddState (new BallStateFollow (this));
		this.brain.AddState (new BallStateCornerSquare (this));

		this.brain.SetState("aimless");
		this.ballGameObject = (GameObject)MonoBehaviour.Instantiate(Resources.Load("Prefabs/Sphere"),position,new Quaternion());
		this.ballGameObject.name = name;
		this.ballActions = this.ballGameObject.AddComponent<BallActions>();
		this.ballGameObject.tag = "ballTag";

		this.location = position;
		this.rigidbody = this.ballGameObject.GetComponent<Rigidbody>();
		this.moving = false;

		this.baseSpline = new GameObject("baseSpline" + name);
		this.spline = this.baseSpline.AddComponent<CurvySpline>();
		this.walker = this.ballGameObject.AddComponent<SplineWalker>();

		walker.Clamping = CurvyClamping.Clamp;
		walker.Speed = 20f;

		this.light = this.ballGameObject.GetComponent<Light>();

		//this.ps = this.ballGameObject.GetComponent<ParticleSystem>();
		//this.ps.enableEmission = false;
	}

	public void Process(float timePassed){
		//this.location = this.ball.transform.position;
		//this.CheckForCollision();
		this.brain.Think();

		if (this.ballGameObject.transform.position.y < -20f) {
			this.ballGameObject.transform.position = new Vector3(0,5,0);
		}
	}


	public void Trigger()
	{
		if (this.brain.activeState.GetType () == typeof(BallStateAimless) || this.brain.activeState.GetType () == typeof(BallStateAvoid)) 
		{
			this.brain.SetState("random4point");
			beatsWhileHeld++;
		}
		else
		{
			this.brain.activeState.TriggerSomething();
			beatsWhileHeld++;
		}
	}


	public Ball GetNearestBall()  {		
		var nearestDistanceSqr = Mathf.Infinity;
		Ball nearestObj = null;		
		// loop through each tagged object, remembering nearest one found
		foreach (var obj in main.balls) {
			
			var objectPos = obj.ballGameObject.transform.position;
			var distanceSqr = (objectPos - this.ballGameObject.transform.position).sqrMagnitude;
			
			if (distanceSqr < nearestDistanceSqr && distanceSqr > 0.1f) { //Otherwise the nearest thing is itself!
				nearestObj = obj;
				nearestDistanceSqr = distanceSqr;
			}
		}		
		return nearestObj;
	}

	public Ball GetNearestBall(float radius)  {		
		var nearestDistanceSqr = Mathf.Infinity;
		Ball nearestObj = null;		
		// loop through each tagged object, remembering nearest one found
		foreach (var obj in main.balls) {
			
			var objectPos = obj.ballGameObject.transform.position;
			var distanceSqr = (this.ballGameObject.transform.position - objectPos).sqrMagnitude;
			
			if (distanceSqr < nearestDistanceSqr && distanceSqr > 0.1f && distanceSqr < (radius * radius)) { //Otherwise the nearest thing is itself!
				nearestObj = obj;
				nearestDistanceSqr = distanceSqr;
			}
		}		
		return nearestObj;
	}



	/*
	public void CheckForCollision(){
			foreach (var ball in main.balls)
			{
				if (ball.ballGameObject.name != this.ballGameObject.name && ball.brain.activeState.GetType() != typeof(BallStateRandom4Point))
				{
					//MonoBehaviour.print(Vector3.Distance(ball.ball.transform.position, this.ball.ball.transform.position));
					if (Vector3.Distance(ball.ballGameObject.transform.position, this.ballGameObject.transform.position) < 1.5)
					{
						//ball.ballActions.Flash();
						ball.brain.SetState("avoid");
					}
				}				
			}
	}*/

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
