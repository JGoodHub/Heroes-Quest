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

    //-----METHODS-----

    void Start() {
		//Initialise all the manager scripts in the game
        TileManager.instance.Initialise();
		AbilityBook.instance.Initialise();		
        PlayerManager.instance.Initialise();		
		EnemyAIManager.instance.Initialise();
		AbilityManager.instance.Initialise();
		QuestManager.instance.Initialise();
		UIManager.instance.Initialise();	
		CameraManager.instance.Initialise();
		InteractablesManager.instance.Initialise();
		

		//Start the players turn immediately
		StartPlayersTurn();
    }

	//Start the players turn
	public void StartPlayersTurn () {
		UIManager.instance.EnableEndTurnButton();
		PlayerManager.instance.BeginTurn();
	}

	//End the players turn and start the enemies
	public void EndPlayersTurn () {
		UIManager.instance.DisableEndTurnButton();
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
