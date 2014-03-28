using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class CornerPoints{
	public List<Vector3> corners = new List<Vector3>();
}

public class BallStateCornerSquare : State {

	private Ball ball = null;
	public override string name {get;set;}
	public bool midJourney = false;
	public override Main main { get; set;}


	public float lastTriggered;

	public BallStateCornerSquare(Ball ball)
	{
		name = "cornerSquare";
		this.ball = ball;
	}

	public override void TriggerSomething ()
	{

		lastTriggered = Time.time;	
	}

	public override void EntryActions(){

		this.main = GameObject.Find("Main Camera").GetComponent<Main>();
		//MonoBehaviour.print (main.corners.Count);

		if (this.ball.currentCorner == null) {
						var possibleCorners = main.corners.Where (x => x.taken == false);
						this.ball.currentCorner = possibleCorners.ElementAt (Random.Range (0, possibleCorners.Count () - 1));

						this.ball.currentCorner.taken = true;

						var height = this.ball.ballGameObject.transform.position.y;

						for (int i = 0; i < this.ball.currentCorner.points.Count; i++) {
								var newPoint = this.ball.currentCorner.points [i];
								newPoint.y = height;
								this.ball.currentCorner.points [i] = newPoint;
						}

				}
		
		MonoBehaviour.print ("cornerSquare");
		var light = this.ball.ballGameObject.GetComponent<Light> ();
		light.intensity = 5.0f;
		
		
		

		//baseSpline.transform.position = new Vector3(0,0,0);
		this.ball.walker.Spline = this.ball.spline;
		
		this.ball.spline.Clear();	
		
		var controlPoints = new Vector3[]{
			this.ball.ballGameObject.transform.position, 
			this.ball.currentCorner.points[0]
		} ;
		
		this.ball.spline.ControlPoints.Clear();
		
		this.ball.spline.Add (controlPoints);
		this.ball.walker.Spline = this.ball.spline;
		this.ball.walker.Clamping = CurvyClamping.Clamp;
		this.ball.walker.Speed = 3f;
		this.ball.walker.TF = 0.0f;	

		lastTriggered = Time.time;	
	


	}
	
	public void SetupActualLoop(){
		
		this.main = GameObject.Find("Main Camera").GetComponent<Main>();
		
		//MonoBehaviour.print ("random4point");
		var light = this.ball.ballGameObject.GetComponent<Light> ();
		light.intensity = 5.0f;
		
		
		

		//baseSpline.transform.position = new Vector3(0,0,0);
		this.ball.walker.Spline = this.ball.spline;
		
		this.ball.spline.Clear();

		/*
		var controlPoints = new Vector3[]{
			new Vector3(0f,height,0f),
			new Vector3(0f,height,10f),
			new Vector3(10f,height,10f),
			new Vector3(10f,height,0f)
		} ;*/

		var controlPoints = this.ball.currentCorner.points.ToArray();
		
		this.ball.spline.ControlPoints.Clear();
		
		this.ball.walker.Clamping = CurvyClamping.Loop;


		this.ball.spline.Add (controlPoints);
		this.ball.spline.Closed = true;
		this.ball.walker.Speed = 0.5f;
		//this.ball.walker.TF = 0.0f;


		//Sets up each of the follows to follow the lead spline, and positions them based on the number of balls in the group.

		for(int i = 0; i < this.ball.followers.Count; i++)
		{
			this.ball.followers[i].walker.Spline = this.ball.spline;
			var increment = 1.0f / (this.ball.followers.Count + 1);

			var position = this.ball.walker.TF - ((increment * i) + increment);
			if (position < 0.0f)
			{
				position += 1.0f;
			}
			this.ball.followers[i].walker.TF = position;
			this.ball.followers[i].walker.Clamping = CurvyClamping.Loop;
			this.ball.followers[i].walker.Speed = 0.5f;
			this.ball.followers[i].light.color = this.ball.light.color;
		}

	}
	
	public override void ExitActions ()
	{
		//throw new System.NotImplementedException ();
		main.colors.Find(x=>x.color == this.ball.light.color).taken = false;
		main.corners.Find (x => x == this.ball.currentCorner).taken = false;
		this.ball.currentCorner = null;
		this.ball.colourSet = false;
	}
	
	public override void DoActions(){
		
	}
	
	
	public override string CheckConditions(){
		if (this.ball.walker.TF == 1.0f) 
		{
			this.SetupActualLoop();
		}

		if ((Time.time - lastTriggered) > 1f) 
		{
			this.ball.walker.Spline = null;
			this.lastTriggered = Time.time;
			//this.returnToAimless = false;
			return "aimless";
		}
		
		return null; //say in random until end of route.
	}
}
