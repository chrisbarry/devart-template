using UnityEngine;
using System.Collections;

public abstract class State {

	public abstract string name {get;set;}
	public abstract void DoActions();
	public abstract string CheckConditions();
	public abstract void EntryActions();
	public abstract void ExitActions();

}
