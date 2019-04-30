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

    /// <summary>
    /// Initialise all the manager scripts in the game
    /// </summary>
    void Start() {
        TileManager.instance.Initialise();
		CameraManager.instance.Initialise();
		UIManager.instance.Initialise();
		LightingManager.instance.Initialise();

		if (TileManager.instance.mapEditorModeEnabled) {
			TileManager.instance.EnableMapEditorMode();
			UIManager.instance.SetToTileEditorMode();
		} else {
			UIManager.instance.SetToGameMode();
			PathManager.instance.Initialise();
			AbilityCollection.instance.Initialise();		
			PlayerManager.instance.Initialise();		
			EnemyAIManager.instance.Initialise();
			QuestManager.instance.Initialise();				
			InteractablesManager.instance.Initialise();

			StartPlayersTurn();
		}		
    }

    /// <summary>
    /// Start the players turn
    /// </summary>
    public void StartPlayersTurn () {
        UIManager.instance.EnablePlayerControls();

        if (EnemyAIManager.instance.combatActive == false) {
            UIManager.instance.HideEndTurnButton();
        } else {
            UIManager.instance.ShowEndTurnButton();
            UIManager.instance.EnableEndTurnButton();
        }
        
		PlayerManager.instance.BeginTurn();
	}

    /// <summary>
    /// End the players turn and start the enemies
    /// </summary>
    public void EndPlayersTurn () {
        if (PlayerManager.instance.playersTurn) {
            UIManager.instance.DisableEndTurnButton();
            UIManager.instance.DisablePlayerControls();

            PlayerManager.instance.EndTurn();

            StartEnemyTurn();
        }
	}

    /// <summary>
    /// Start the enemies turn
    /// </summary>
    public void StartEnemyTurn () {
		EnemyAIManager.instance.BeginTurn();
	}

    /// <summary>
    /// End the enemies turn
    /// </summary>
    public void EndEnemyTurn () {
		StartPlayersTurn();
	}

}
