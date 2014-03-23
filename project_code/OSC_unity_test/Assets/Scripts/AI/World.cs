using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class World : MonoBehaviour {


	public List<GameObject> Entities = new List<GameObject>();
	public int entityId = 0;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		//def proccess:
		//time_passed_seconds = time_passed / 1000.0
		//for entity in self.entities.itervalues():
		//	entity.process(time_passed_seconds)

		//def render:
		//# Draw the background and all the entities
		//surface.blit(self.background, (0, 0))
		//	for entity in self.entities.values():
		//		entity.render(surface)
	}
	
	void AddEntity(GameObject gameObject){
		//# Stores the entity then advances the current id
		//self.entities[self.entity_id] = entity
		//entity.id = self.entity_id
		//self.entity_id += 1
	}


	
}
