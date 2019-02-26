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
    public HashSet<EnemyController> enemiesSet = new HashSet<EnemyController>();

    //-----METHODS-----

	//Setup Method
    public void Initialise () {
		//Find all enemies with the "enemy" tag and their character controllers in the enemy set
		foreach (GameObject gameObj in GameObject.FindGameObjectsWithTag("Enemy")) {
			enemiesSet.Add((EnemyController) gameObj.GetComponent<CharacterController>());
		}

		//Initialise all the enemies
		foreach(EnemyController e in enemiesSet) {
			e.Initialise();
		}
    }

	//Called at the start of the enemies turn
    public void BeginTurn () {
		if (enemiesSet.Count > 0) {
			foreach (EnemyController enemyController in enemiesSet) {
				enemyController.CharacterData.ApplyChangeToData(new StatChange(ResourceType.ACTIONPOINTS, 3));
			}

			StartCoroutine(TurnCoroutine());
		} else {
			GameManager.instance.EndEnemyTurn();
		}
	}

	//Coroutine for the enemies turn
	IEnumerator TurnCoroutine () {
		yield return new WaitForSeconds(1.5f);

		//Iterate through each enemy in the instance (all enemies in the scene for now)
		foreach (EnemyController enemy in enemiesSet) {
			//Set the cameras focus to the current enemy
			CameraManager.instance.SetTrackingTarget(enemy.MovementController);

			//Iterate through each hero in the players party
			CharacterController targetHero = null;
			Path currentShortestPath = null;
			foreach (CharacterController hero in PlayerManager.instance.HeroSet) {
				//Find the hero with the shortest length path to the enemy and target that hero
				if (targetHero != null) {
					Path alternateShortestPath = PathfindingManager.instance.FindShortestPathBetween(enemy.MovementController.CurrentVertex, hero.MovementController.CurrentVertex);
					if (alternateShortestPath.GetPathLength() < currentShortestPath.GetPathLength()) {
						targetHero = hero;
						currentShortestPath = alternateShortestPath;
					}
				} else {
					targetHero = hero;
					currentShortestPath = PathfindingManager.instance.FindShortestPathBetween(enemy.MovementController.CurrentVertex, hero.MovementController.CurrentVertex);
				}
			}

			//Trim the path until its within the enemies action points
			while (enemy.MovementController.APCostOfMovement(currentShortestPath) >= enemy.CharacterData.GetResourceOfType(ResourceType.ACTIONPOINTS)) {
				currentShortestPath.TrimPath(1);
			}

			//Move the enemy along that path and wait for them to rest the last vertex
			enemy.MovementController.MoveCharacter(currentShortestPath, 0f);
			while (enemy.MovementController.CurrentlyMoving) {
				yield return null;
			}

			//Set the hero as the enemies target and attack them
			enemy.SetTarget(targetHero);
			enemy.CastAbility(0);
			while (AbilityManager.instance.abilityRunning) {
				//Wait one frame
				yield return null;
			}				
		}

		//Focus the camera on the players selected hero
		CameraManager.instance.SetTrackingTarget(PlayerManager.instance.SelectedHero.MovementController);
		while (CameraManager.instance.TrackingTargetReached == false) {
			Debug.Log("MOVING");
			//Wait one frame
			yield return null;
		}
		CameraManager.instance.ClearTrackingTarget();

		//End the enemies turn
		GameManager.instance.EndEnemyTurn();
	}

}
