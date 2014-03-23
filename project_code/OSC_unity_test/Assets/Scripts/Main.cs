using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

	// Use this for initialization

	public List<Ball> balls = new List<Ball>();
	public GameObject floor;


	void Start () {

		for (int i = 0; i < 8; i++)
		{
			balls.Add(new Ball("ball" + i.ToString(), new Vector3(Random.Range(-10,10),5,Random.Range(-10,10))));
		}

		//var verts = floor.GetComponent<MeshFilter>().mesh.vertices;

		/*
pygame.init()
screen = pygame.display.set_mode(SCREEN_SIZE, 0, 32)
world = World()
w, h = SCREEN_SIZE
clock = pygame.time.Clock()
ant_image = pygame.image.load("ant.png").convert_alpha()
leaf_image = pygame.image.load("leaf.png").convert_alpha()
spider_image = pygame.image.load("spider.png").convert_alpha()
# Add all our ant entities
for ant_no in xrange(ANT_COUNT):
www.it-ebooks.info
ant = Ant(world, ant_image)
ant.location = Vector2(randint(0, w), randint(0, h))
ant.brain.set_state("exploring")
world.add_entity(ant)
while True:
for event in pygame.event.get():
if event.type == QUIT:
return
time_passed = clock.tick(30)
# Add a leaf entity 1 in 20 frames
if randint(1, 10) == 1:
leaf = Leaf(world, leaf_image)
leaf.location = Vector2(randint(0, w), randint(0, h))
world.add_entity(leaf)
# Add a spider entity 1 in 100 frames
if randint(1, 100) == 1:
spider = Spider(world, spider_image)
spider.location = Vector2(-50, randint(0, h))
spider.destination = Vector2(w+50, randint(0, h))
world.add_entity(spider)
world.process(time_passed)
world.render(screen)
pygame.display.update()
*/
	}
	
	// Update is called once per frame
	void Update () {
		//def proccess:
		//time_passed_seconds = time_passed / 1000.0
		//for entity in self.entities.itervalues():
		//	entity.process(time_passed_seconds)

		foreach(var ball in balls)
		{
			ball.Process(Time.deltaTime);
		}

	}
}
