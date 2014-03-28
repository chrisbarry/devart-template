using UnityEngine;
using System.Collections;

public class BallStateAvoid : State {

	private Ball ball = null;
	public override string name {get;set;}
	public override Main main { get; set;}

	public BallStateAvoid(Ball ball)
	{
		name = "avoid";
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

		//If still moving quite fast, slow down, untill moving slowly again, and return to aimless
		if (this.ball.rigidbody.velocity.magnitude > 0.1f) 
		{
			this.ball.rigidbody.velocity = this.ball.rigidbody.velocity * 0.9f;
		}
		else
		{
			return "aimless";
		}
		return null; //Always stay in avoid
	}

	public override void EntryActions(){
		//MonoBehaviour.print ("avoid");
		ball.light.color = new Color (Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f), 0.5f);
		ball.light.intensity = 3.0f;
		var nearest = ball.GetNearestBall().ballGameObject;
		Vector3 avoidanceHeading = this.ball.ballGameObject.transform.position - nearest.transform.position;
		ball.ballGameObject.rigidbody.AddForce(avoidanceHeading * 100);
	}

	public override void ExitActions ()
	{
		//throw new System.NotImplementedException ();
	}


}
