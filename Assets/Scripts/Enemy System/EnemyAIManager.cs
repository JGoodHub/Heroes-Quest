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

	public HashSet<EncounterController> encounters;

    public bool combatActive = false;

    //-----METHODS-----

    /// <summary>
    /// Setup all the encounters in the world
    /// </summary>
    public void Initialise () {
		encounters = new HashSet<EncounterController>(gameObject.GetComponentsInChildren<EncounterController>());
		foreach (EncounterController encounter in encounters) {
			encounter.Initialise();
		}
    }

    /// <summary>
    /// Starts the BeginTurnCoroutine
    /// </summary>
    public void BeginTurn () {
        StartCoroutine(BeginTurnCoroutine());
	}

    /// <summary>
    /// Processes the turn for each encounter
    /// </summary>
    /// <returns></returns>
    IEnumerator BeginTurnCoroutine() {
        combatActive = false;

        foreach (EncounterController encounter in encounters) {
            if (encounter.encounterActive == true) {               
                combatActive = true;
                encounter.ProcessTurn();
                while (encounter.turnRunning) {
                    yield return null;
                }

            }
        }

        GameManager.instance.EndEnemyTurn();
    }

    /// <summary>
    /// Get all the perception vertices of all encounters
    /// </summary>
    /// <returns>All perception vertices of all encounters</returns>
	public HashSet<Vertex> GetAllEncounterVertices () {
		HashSet<Vertex> triggerCollection = new HashSet<Vertex>();
		foreach (EncounterController encounter in encounters) {
			foreach (Vertex triggerVertex in encounter.TriggerVertices) {
				triggerCollection.Add(triggerVertex);
			}
		}
		return triggerCollection;
	}

    /// <summary>
    /// Find the encounter that a vertex is part of
    /// </summary>
    /// <param name="vertex">The vertex to search</param>
    /// <returns>The encounter that the vertex is part of</returns>
	public EncounterController GetEncounterThatVertexIsPartOf (Vertex vertex) {
		foreach(EncounterController encounter in encounters) {
			if (encounter.TriggerVertices.Contains(vertex)) {
				return encounter;
			}
		}

		return null;
	}

}
