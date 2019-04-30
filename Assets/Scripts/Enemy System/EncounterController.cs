using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncounterController : MonoBehaviour {

    //-----VARIABLES-----
    
    public HashSet<CharacterController> enemies;

    private Vertex encounterCentreVertex;
    public float perceptionRange;
    private HashSet<Vertex> triggerVertices;
    public HashSet<Vertex> TriggerVertices { get => triggerVertices; }

    [HideInInspector] public bool encounterActive = false;
    [HideInInspector] public bool turnRunning = false;

    public GameObject miniMapMarker;

    //-----METHODS-----

    /// <summary>
    /// Setup references for all enemies in the encounters enemies set and calculate the perception vertices
    /// </summary>
    public void Initialise () {
        enemies = new HashSet<CharacterController>(GetComponentsInChildren<CharacterController>());
        foreach (EnemyController enemy in enemies) {
			enemy.Initialise();
            enemy.encounterInstance = this;
		}

        //Calculate the encounters 
        encounterCentreVertex = CalculateEnemyGroupCenterVertex();
        triggerVertices = PathManager.instance.GetVerticesInRange(encounterCentreVertex, perceptionRange, true);

        miniMapMarker.transform.position = new Vector3(encounterCentreVertex.worldPosition.x, miniMapMarker.transform.position.y, encounterCentreVertex.worldPosition.z);
        miniMapMarker.transform.localScale = Vector3.one * (perceptionRange * 1.116f);
    }

    /// <summary>
    /// Calculate the average vertex between all enemies
    /// </summary>
    /// <returns></returns>
    public Vertex CalculateEnemyGroupCenterVertex () {
        Vector2 averageTilePosition = Vector2.zero;
        foreach (CharacterController enemy in enemies) {
            averageTilePosition += enemy.MovementController.GraphObstacle.currentVertex.graphCoordinates;
        }

        averageTilePosition /= enemies.Count;
        return PathManager.instance.GetClosestVertexToCoordinates(Vector2Int.RoundToInt(averageTilePosition));
    }

    /// <summary>
    /// Start the ProcessTurnCoroutine or close the encounter if all enemies are dead
    /// </summary>
    public void ProcessTurn () {
        triggerVertices.Clear();

        if (enemies.Count > 0) {
            StartCoroutine(EncounterTurnCoroutine());
        } else {
            CloseEncounter();
        }
    }
    
    /// <summary>
    /// Move and attack with each enemy in the encounter
    /// </summary>
    /// <returns></returns>
    IEnumerator EncounterTurnCoroutine () {
        turnRunning = true;
        yield return new WaitForSeconds(Random.Range(0.25f, 0.75f));        

		//Iterate through each enemy in the instance (all enemies in the scene for now)
		foreach (EnemyController enemy in enemies) {
			//Set the cameras focus to the current enemy and restore the enemies movement and action
			CameraManager.instance.SetTrackingTarget(enemy.gameObject);
            enemy.CharacterData.actionAvailable = true;
            enemy.MovementController.ResetDistanceMoved();

			yield return new WaitForSeconds(0.5f);

			//Iterate through each hero in the players party
			CharacterController targetHero = null;
			Path currentShortestPath = null;
			foreach (CharacterController hero in PlayerManager.instance.HeroSet) {
				//Find the hero with the shortest length path to the enemy and target that hero
				if (targetHero == null || currentShortestPath == null) {
                    targetHero = hero;
                    currentShortestPath = PathManager.instance.FindShortestPathBetween(enemy.MovementController.GraphObstacle.currentVertex, hero.MovementController.GraphObstacle.currentVertex);
                } else {					
                    Path alternateShortestPath = PathManager.instance.FindShortestPathBetween(enemy.MovementController.GraphObstacle.currentVertex, hero.MovementController.GraphObstacle.currentVertex);
                    if (alternateShortestPath.length < currentShortestPath.length) {
                        targetHero = hero;
                        currentShortestPath = alternateShortestPath;
                    }
                }
			}

            if (currentShortestPath != null) {
                //Trim the path until its within the enemies action points
                while (enemy.MovementController.CanMoveDistance(currentShortestPath.length) == false) {
                    currentShortestPath.TrimPath(1);
                }

                //Move the enemy along that path and wait for them to rest the last vertex
                enemy.MovementController.MoveCharacter(currentShortestPath);
                while (enemy.MovementController.CurrentlyMoving) {
                    yield return null;
                }
            }

            //Set the hero as the enemies target and attempt to attack them
			enemy.SetTarget(targetHero);
			enemy.CastAbility(0);
			while (AbilityManager.instance.abilityRunning) {
                yield return null;
			}
		}

        
        CameraManager.instance.SetTrackingTarget(PlayerManager.instance.SelectedHero.gameObject);
        while (CameraManager.instance.TrackingTargetReached == false) {
			yield return null;
		}
        CameraManager.instance.ClearTrackingTarget();
        

        turnRunning = false;
    }

    /// <summary>
    /// Close the encounter and any associated quests
    /// </summary>
    public void CloseEncounter() {
        EnemyAIManager.instance.combatActive = false;
        encounterActive = false;

        QuestObjective questObjectiveScript = GetComponent<QuestObjective>();
        if (questObjectiveScript != null) {
            QuestComponent questComp = questObjectiveScript.TriggerNextQuest();
        }
    }

    //-----GIZMOS-----
    public bool drawPerceptionRange;
    /// <summary>
    /// Draw gizmos to the editor window
    /// </summary>
    void OnDrawGizmos () {
        if (drawPerceptionRange) {
            if (Application.isPlaying) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(CalculateEnemyGroupCenterVertex().worldPosition, 0.8f);

                foreach (Vertex triggerVertex in triggerVertices) {
					Gizmos.DrawSphere(triggerVertex.worldPosition, 0.4f);
                }
            }
        }
    }

}
