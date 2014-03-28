using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BallStateFollow : State {

	private Ball ball = null;
	public override string name {get;set;}
	public override Main main { get; set;}

	public Light light;

	public BallStateFollow(Ball ball)
	{
		name = "follow";
		this.ball = ball;
		//self.leaf_id = None
		this.main = GameObject.Find("Main Camera").GetComponent<Main>();
	}

	public override void TriggerSomething ()
	{
		
	}

	public override void DoActions(){

	}

	
	public override string CheckConditions(){


		if (this.ball.resetspline) {
			SetupFollowSpline();
			this.ball.resetspline = false;
		}

		if (this.ball.target.brain.activeState.GetType() == typeof(BallStateAimless))
		{
			return "aimless";
		}

		return null; //Always stay in avoid
	}

	public void SetupFollowSpline(){

		this.ball.ballGameObject.GetComponent<Light>().intensity = this.ball.target.ballGameObject.GetComponent<Light>().intensity;
		this.ball.ballGameObject.GetComponent<Light>().color = this.ball.target.ballGameObject.GetComponent<Light>().color;
		
		var targetSpline = this.ball.target.spline;
		
		var existingControlPoints = targetSpline.ControlPoints;
		var totaldistance = targetSpline.Length;
		
		var position = this.ball.target.walker.TF;

		var distanceToFind = totaldistance * position;
		
		List<Vector3> controlPoints = new List<Vector3> ();
		controlPoints.Add (this.ball.ballGameObject.transform.position);
		
		float distanceTracked = 0.0f;
		//MonoBehaviour.print ("distanceToFind " + distanceToFind);
		
		//walk through the existing spline, and create a new spline, that joins it and follows it.
		for (int i = 0; i < existingControlPoints.Count - 1; i++) 
		{
			distanceTracked += Vector3.Distance(existingControlPoints[i].gameObject.transform.position, existingControlPoints[i+1].gameObject.transform.position);
			if (distanceTracked > distanceToFind)
			{
				controlPoints.Add(existingControlPoints[i].gameObject.transform.position);
			}
			//There was a problem here, becuase sometimes there would be no control points picked...
			//I wasn't += distanceTracked, I was just tracking each segment..
		}
		
		this.ball.spline.Clear();
		this.ball.spline.ControlPoints.Clear();
		this.ball.spline.Add(controlPoints.ToArray());
		
		this.ball.walker.Speed = this.ball.target.walker.Speed;
		this.ball.walker.Spline = this.ball.spline;		
		this.ball.walker.TF = 0.0f;

		}


	public override void EntryActions()
	{
		light = this.ball.ballGameObject.GetComponent<Light> ();
		this.ball.ballGameObject.GetComponent<Light>().color = this.ball.target.ballGameObject.GetComponent<Light>().color;
		this.ball.target.hasfollower = true;
		if (this.ball.target.brain.activeState.GetType () == typeof(BallStateRandom4Point)) {
			this.ball.leader = this.ball.target;
		} else {
			this.ball.leader = this.ball.target.leader;
		}
		this.ball.leader.followers.Add (this.ball);
		//MonoBehaviour.print("follow");
		SetupFollowSpline ();
	}

	public override void ExitActions ()
	{
		this.ball.target.followers.Clear();
		this.ball.walker.Spline = null;
		this.ball.ballGameObject.GetComponent<Light>().intensity = 0.0f;
		this.ball.target.hasfollower = false;
		this.ball.target = null;
		//throw new System.NotImplementedException ();
	}




}
