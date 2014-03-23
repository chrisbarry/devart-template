using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StateMachine : MonoBehaviour {

	private List<State> states = new List<State>();
	private State activeState = null;

	public void AddState(State state)
	{
		this.states.Add(state);
	}

	public void Think(){
		if (this.activeState == null)
			return;
		this.activeState.DoActions();
		var newStateName = this.activeState.CheckConditions();
		if (newStateName != null)
			this.SetState(newStateName);
	}

	public void SetState(string newStateName){
		if (this.activeState != null)
			this.activeState.ExitActions();
		this.activeState = this.states.Find(i=>i.name == newStateName); //This line is wrong, need to lookup state by state name;
		this.activeState.EntryActions();
	}

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
