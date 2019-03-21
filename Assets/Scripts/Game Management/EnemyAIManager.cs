using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIManager : MonoBehaviour {

     //-----SINGLETON SETUP-----

	public static EnemyAIManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

	//Set of all enemies in the game
	public HashSet<EnemyEncounter> encounters;

    //-----METHODS-----

	//Setup Method
    public void Initialise () {
		encounters = new HashSet<EnemyEncounter>(gameObject.GetComponentsInChildren<EnemyEncounter>());
		foreach (EnemyEncounter encounter in encounters) {
			encounter.Initialise();
		}
    }

	//Called at the start of the enemies turn
    public void BeginTurn () {
		if (encounters.Count > 0) {
			foreach (EnemyEncounter encounter in encounters) {
				if (encounter.encounterActive) {					
					encounter.ProcessTurn();
				}
			}
		}

		GameManager.instance.EndEnemyTurn();
	}

	public HashSet<Vertex> GetAllEncounterTriggerVertices () {
		HashSet<Vertex> triggerCollection = new HashSet<Vertex>();
		foreach(EnemyEncounter encounter in encounters) {
			foreach(Vertex triggerVertex in encounter.TriggerVertices) {
				triggerCollection.Add(triggerVertex);
			}
		}
		return triggerCollection;
	}

	public EnemyEncounter GetSpecificEncounter (Vertex charVertex) {
		foreach(EnemyEncounter encounter in encounters) {
			if (encounter.TriggerVertices.Contains(charVertex)) {
				return encounter;
			}
		}

		return null;
	}

}
