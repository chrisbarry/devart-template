using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BallStateRandom4Point : State {

	private Ball ball = null;
	public override string name {get;set;}
	public bool midJourney = false;
	public override Main main { get; set;}

	public bool returnToAimless = false;
	public float lastTriggered;

	public BallStateRandom4Point(Ball ball)
	{
		name = "random4point";
		this.ball = ball;
	}

	public override void EntryActions(){
		
		this.main = GameObject.Find("Main Camera").GetComponent<Main>();
		
		//MonoBehaviour.print ("random4point");


		
		
		
		//MonoBehaviour.print (this.ball.colourSet);
		if (!this.ball.colourSet) 
		{
			var possibleColors = main.colors.Where(x=>x.taken == false);
			var color = possibleColors.ElementAt(Random.Range(0,possibleColors.Count()-1));
			color.taken = true;
			this.ball.light.color = color.color;
			this.ball.colourSet = true;
		}

		SetupLocation();
		lastTriggered = Time.time;	
	}

	public override void TriggerSomething()
	{
		if (Random.Range(0,4) == 0)
		{
			SetupLocation(); //every one in 4 beats, randomly, move to the beat.
		}
		//SetupLocation();
		lastTriggered = Time.time;	
	}

	public void SetupLocation(){
		//baseSpline.transform.position = new Vector3(0,0,0);
		this.ball.walker.Spline = this.ball.spline;
		
		this.ball.spline.Clear();	
		
		var controlPoints = new Vector3[]{
			this.ball.ballGameObject.transform.position, 
			new Vector3(Random.Range(-10,10),this.ball.ballGameObject.transform.position.y,Random.Range(-10,10)),
			new Vector3(Random.Range(-10,10),this.ball.ballGameObject.transform.position.y,Random.Range(-10,10)),
			new Vector3(Random.Range(-10,10),this.ball.ballGameObject.transform.position.y,Random.Range(-10,10))
		} ;
		
		this.ball.spline.ControlPoints.Clear();
		
		this.ball.spline.Add (controlPoints);
		this.ball.walker.Speed = 0.5f;
		this.ball.walker.TF = 0.0f;

		this.ball.light.intensity = 3f;

		foreach (var ball in this.ball.followers) 
		{
			ball.resetspline = true;
		}
	}

	
	public override void ExitActions ()
	{
		//throw new System.NotImplementedException ();
		main.colors.Find(x=>x.color == this.ball.light.color).taken = false;		
		this.ball.colourSet = false;
	}
	
	public override void DoActions(){


	}
	
	
	public override string CheckConditions(){

		if (this.ball.walker.TF == 1.0f) {
			this.SetupLocation();
		}

		if ((Time.time - lastTriggered) > 1f) 
		{
			this.ball.walker.Spline = null;
			this.lastTriggered = -1f;
			this.returnToAimless = false;
			return "aimless";
		}

		if (this.ball.beatsWhileHeld > 16)
		{
			return "cornerSquare";
		}
		
		return null; //say in random until end of route.
	}
}
