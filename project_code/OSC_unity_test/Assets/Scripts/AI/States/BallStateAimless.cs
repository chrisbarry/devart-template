using UnityEngine;
using System.Collections;

public class BallStateAimless : State {

	private Ball ball = null;
	public override string name {get;set;}


	public BallStateAimless(Ball ball)
	{
		name = "aimless";
		this.ball = ball;
		//self.leaf_id = None
	}

	public void RandomDestination(){
		//w, h = SCREEN_SIZE
		//self.ant.destination = Vector2(randint(0, w), randint(0, h))
		this.ball.destination = new Vector3(Random.Range(-10,10),this.ball.ball.transform.position.y,Random.Range(-10,10));
		MonoBehaviour.print("set destination");
	}

	public override void DoActions(){
		if (!this.ball.moving)
		{
			if (Random.Range(0,100) == 1)
			{
				this.RandomDestination();
				this.ball.moving = true;
			}
		}
			
	}

	public override string CheckConditions(){

		if (this.ball.location == this.ball.destination)
		{
			this.ball.moving = false;
		}

		return null; //Always stay in aimless for now..

		/*leaf = self.ant.world.get_close_entity("leaf", self.ant.location)
		if leaf is not None:
		self.ant.leaf_id = leaf.id
		return "seeking"
		# If the ant sees a spider attacking the base, go to hunting state
		spider = self.ant.world.get_close_entity("spider", NEST_POSITION, NEST_SIZE)
		if spider is not None:
		if self.ant.location.get_distance_to(spider.location) < 100.:
		self.ant.spider_id = spider.id
		return "hunting"
		return None
		*/
	}

	public override void EntryActions(){
		this.ball.speed = 10;
		//self.ant.speed = 120. + randint(-30, 30)
		//this.RandomDestination();
	}

	public override void ExitActions ()
	{
		//throw new System.NotImplementedException ();
	}
}
