using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BallStateFollow2 : State {

	private Ball ball = null;
	public override string name {get;set;}
	public override Main main { get; set;}

	public BallStateFollow2(Ball ball)
	{
		name = "follow2";
		this.ball = ball;
		//self.leaf_id = None
		this.main = GameObject.Find("Main Camera").GetComponent<Main>();
	}

	public override void TriggerSomething ()
	{
		
	}

	public override void DoActions()
	{
		Vector3 avoidanceHeading = this.ball.target.ballGameObject.transform.position - this.ball.ballGameObject.transform.position;
		float distance = Vector3.Distance (this.ball.target.ballGameObject.transform.position, this.ball.ballGameObject.transform.position);
		ball.ballGameObject.rigidbody.AddForce(avoidanceHeading * (distance/5f));
	}

	
	public override string CheckConditions(){



		return null; //Always stay in avoid
	}



	public override void EntryActions()
	{
		this.ball.ballGameObject.GetComponent<Light>().color = this.ball.target.ballGameObject.GetComponent<Light>().color;
		this.ball.ballGameObject.GetComponent<Light>().intensity = this.ball.target.ballGameObject.GetComponent<Light>().intensity;

		this.ball.target.hasfollower = true;
		if (this.ball.target.brain.activeState.GetType () == typeof(BallStateRandom4Point)) {
			this.ball.leader = this.ball.target;
		} else {
			this.ball.leader = this.ball.target.leader;
		}
		this.ball.leader.followers.Add (this.ball);
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
