using UnityEngine;
using System.Collections;

public class BallStateAimless : State {

	private Ball ball = null;
	public override string name {get;set;}
	public override Main main { get; set; }

	public BallStateAimless(Ball ball)
	{
		name = "aimless";
		this.ball = ball;
		//self.leaf_id = None
	}

	public override void TriggerSomething ()
	{

	}

	public void RandomDestination(){
		this.ball.destination = new Vector3(Random.Range(-5,5),this.ball.ballGameObject.transform.position.y,Random.Range(-5,5));
	}

	public override void DoActions(){
			if (Random.Range(0,200) == 1)
			{
				//MonoBehaviour.print("moving");
				this.RandomDestination();
				this.ball.moving = true;
				Vector3 heading = this.ball.destination - this.ball.ballGameObject.transform.position;
				this.ball.rigidbody.AddForce(heading * 3);
			}

				
		if (this.ball.light.intensity != 0.0f) 
		{
			this.ball.light.intensity = this.ball.light.intensity * 0.6f;
		}	

	}

	
	public override string CheckConditions(){


		var nearestBallLarger = this.ball.GetNearestBall(5.0f);

		if (nearestBallLarger != null &&
		    nearestBallLarger.hasfollower == false && 
		    (nearestBallLarger.brain.activeState.GetType() == typeof(BallStateRandom4Point) || 
		 	nearestBallLarger.brain.activeState.GetType() == typeof(BallStateFollow)))
		    {
			if (nearestBallLarger.leader == null)
			{
				ball.target = nearestBallLarger;
				//MonoBehaviour.print (nearestBallLarger.name);
				return "follow";
			}
			else 
			{
			if (nearestBallLarger.leader.brain.activeState.GetType() != typeof(BallStateCornerSquare))
				{
				ball.target = nearestBallLarger;
				//MonoBehaviour.print (nearestBallLarger.name);
				return "follow";
				}
			}
		}
		    
		var nearestBall = this.ball.GetNearestBall(1.5f); //nearest ball, within 2.0f radius

		if (nearestBall == null)
		{
			return null;
		}

		return "avoid";

	 //Always stay in aimless for now..
	}

	public override void EntryActions(){
		//MonoBehaviour.print ("aimless");
		this.ball.speed = 10;
		//self.ant.speed = 120. + randint(-30, 30)
		//this.RandomDestination();
	}

	public override void ExitActions ()
	{
		this.ball.beatsWhileHeld = 0;
		//throw new System.NotImplementedException ();
	}
}
