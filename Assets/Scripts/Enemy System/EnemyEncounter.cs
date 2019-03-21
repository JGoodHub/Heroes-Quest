using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEncounter : MonoBehaviour {

    //-----VARIABLES-----
    
    public CharacterController[] enemies;

    private Vertex encounterCentreVertex;
    public float perceptionRange;
    private HashSet<Vertex> triggerVertices;
    public HashSet<Vertex> TriggerVertices { get => triggerVertices; }

    public bool encounterActive = false;

    //-----METHODS-----

    public void Initialise () {
        //Initialise all the enemies in this encounter
		foreach (EnemyController e in enemies) {
			e.Initialise();
		}

        encounterCentreVertex = CalculateEnemyGroupCenterVertex();
        triggerVertices = PathManager.instance.GetVerticesInRange(encounterCentreVertex, perceptionRange, true);
    }

    public Vertex CalculateEnemyGroupCenterVertex () {
        Vector2 averageTilePosition = Vector2.zero;
        foreach (CharacterController enemy in enemies) {
            averageTilePosition += enemy.MovementController.GraphObstacle.CurrentVertex.GraphCoordinates;
        }

        averageTilePosition /= enemies.Length;
        return PathManager.instance.GetClosestVertexToCoordinates(Vector2Int.RoundToInt(averageTilePosition));
    }

    public void ProcessTurn () {
        triggerVertices.Clear();

        if (enemies.Length > 0) {
            StartCoroutine(EncounterTurnCoroutine());
        } else {
            //All the enemies are dead, the player beat the encounter, reward them with xp
        }
    }

    IEnumerator EncounterTurnCoroutine () {
        yield return new WaitForSeconds(Random.Range(0.25f, 0.75f));

		//Iterate through each enemy in the instance (all enemies in the scene for now)
		foreach (EnemyController enemy in enemies) {
			//Set the cameras focus to the current enemy and restore some the enemies AP
			CameraManager.instance.SetTrackingTarget(enemy.MovementController);
            enemy.CharacterData.ApplyChangeToData(new StatChange(ResourceType.ACTIONPOINTS, 3));

			yield return new WaitForSeconds(0.5f);

			//Iterate through each hero in the players party
			CharacterController targetHero = null;
			Path currentShortestPath = null;
			foreach (CharacterController hero in PlayerManager.instance.HeroSet) {
				//Find the hero with the shortest length path to the enemy and target that hero
				if (targetHero != null) {
					Path alternateShortestPath = PathManager.instance.FindShortestPathBetween(enemy.MovementController.GraphObstacle.CurrentVertex, hero.MovementController.GraphObstacle.CurrentVertex);
					if (alternateShortestPath.GetPathLength() < currentShortestPath.GetPathLength()) {
						targetHero = hero;
						currentShortestPath = alternateShortestPath;
					}
				} else {
					targetHero = hero;
					currentShortestPath = PathManager.instance.FindShortestPathBetween(enemy.MovementController.GraphObstacle.CurrentVertex, hero.MovementController.GraphObstacle.CurrentVertex);
				}
			}

			//Trim the path until its within the enemies action points
			while (enemy.MovementController.APCostOfMovement(currentShortestPath) >= enemy.CharacterData.GetResourceOfType(ResourceType.ACTIONPOINTS)) {
				currentShortestPath.TrimPath(1);
			}

			Debug.Log(currentShortestPath.GetPathLength() + ", " + enemy.MovementController.APCostOfMovement(currentShortestPath));

			//Move the enemy along that path and wait for them to rest the last vertex
			enemy.MovementController.MoveCharacter(currentShortestPath, 0f);
			while (enemy.MovementController.CurrentlyMoving) {
				yield return null;
			}

			//Set the hero as the enemies target and attempt to attack them
			enemy.SetTarget(targetHero);
			enemy.CastAbility(0);
			while (AbilityManager.instance.abilityRunning) {
				yield return null;
			}				
		}

		//Focus the camera on the players selected hero
		CameraManager.instance.SetTrackingTarget(PlayerManager.instance.SelectedHero.MovementController);
		while (CameraManager.instance.TrackingTargetReached == false) {
			//Wait one frame
			yield return null;
		}
		CameraManager.instance.ClearTrackingTarget();

		//End the enemies turn
		GameManager.instance.EndEnemyTurn();
    }

    //-----GIZMOS-----
    public bool drawGizmos;
    void OnDrawGizmos () {
        if (drawGizmos) {
            if (Application.isPlaying) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(CalculateEnemyGroupCenterVertex().WorldPosition, 0.8f);

                foreach (Vertex triggerVertex in triggerVertices) {
					Gizmos.DrawSphere(triggerVertex.WorldPosition, 0.4f);
                }
            }
        }
    }

}
