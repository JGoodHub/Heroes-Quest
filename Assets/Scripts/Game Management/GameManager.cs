using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static GameManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

	//-----VARIABLES-----

	public GameObject runtimeObjectsPool;
	public bool mapEditorModeEnabled;

    //-----METHODS-----

    void Start() {
		//Initialise all the manager scripts in the game
        TileManager.instance.Initialise();
		CameraManager.instance.Initialise();
		UIManager.instance.Initialise();
		LightingManager.instance.Initialise();

		if (mapEditorModeEnabled) {
			TileManager.instance.EnableLiveUpdateMode();
			UIManager.instance.HideUI();
		} else {
			PathManager.instance.Initialise();
			AbilityBook.instance.Initialise();		
			PlayerManager.instance.Initialise();		
			EnemyAIManager.instance.Initialise();
			QuestManager.instance.Initialise();				
			InteractablesManager.instance.Initialise();

			//Start the players turn immediately
			StartPlayersTurn();
		}		
    }

	//Start the players turn
	public void StartPlayersTurn () {
		UIManager.instance.EnableUI();
		PlayerManager.instance.BeginTurn();
	}

	//End the players turn and start the enemies
	public void EndPlayersTurn () {
		UIManager.instance.DisableUI();
		PlayerManager.instance.EndTurn();
		StartEnemyTurn();
	}

	//Start the enemies turn
	public void StartEnemyTurn () {
		EnemyAIManager.instance.BeginTurn();
	}

	//End the enemies turn
	public void EndEnemyTurn () {
		StartPlayersTurn();
	}



}
