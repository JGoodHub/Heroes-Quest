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

    public bool combatActive = false;

    //-----METHODS-----

    /// <summary>
    /// Setup Method
    /// </summary>
    public void Initialise () {
		encounters = new HashSet<EnemyEncounter>(gameObject.GetComponentsInChildren<EnemyEncounter>());
		foreach (EnemyEncounter encounter in encounters) {
			encounter.Initialise();
		}
    }

    /// <summary>
    /// Called at the start of the enemies turn
    /// </summary>
    public void BeginTurn () {
        combatActive = false;

		foreach (EnemyEncounter encounter in encounters) {
			if (encounter.encounterActive) {
                combatActive = true;
				encounter.ProcessTurn();
			}
		}

		GameManager.instance.EndEnemyTurn();
	}

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
	public HashSet<Vertex> GetAllEncounterVertices () {
		HashSet<Vertex> triggerCollection = new HashSet<Vertex>();
		foreach(EnemyEncounter encounter in encounters) {
			foreach(Vertex triggerVertex in encounter.TriggerVertices) {
				triggerCollection.Add(triggerVertex);
			}
		}
		return triggerCollection;
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vertex"></param>
    /// <returns></returns>
	public EnemyEncounter GetEncounterThatVertexIsPartOf (Vertex vertex) {
		foreach(EnemyEncounter encounter in encounters) {
			if (encounter.TriggerVertices.Contains(vertex)) {
				return encounter;
			}
		}

		return null;
	}

}
