using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    
     //-----SINGLETON SETUP-----

	public static UIManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----ENUMS-----

    public enum CursorType {DEFAULT, MOVE, INTERACT, SPEACH};

    //-----VARIABLES-----

	private Camera mainCamera;

	[Header("UI Canvas Root")]
	public Canvas uiRootCanvas;
    
	[Header("Selected Character UI Elements")]
	public Image healthBarFill;
	public Text healthBarText;
	public Image manaBarFill;
	public Text manaBarText;
	public Image actionPointsFill;
	public Image experienceBarFill;

	[Header("Enemy Status Bar UI Elements")]
	public GameObject enemyBarWindow;
	public Text enemyName;
	public Image enemyHealthBarFill;
	public Text enemyHealthBarText;

	[Header("Turn Management UI Elements")]
	public Button endTurnButton;

    [Header("Action Button UI Elements")]
    public Button[] actionButtons;

    [Header("Ability Button UI Elements")]
	public Button[] abilityButtons;

	[Header("Action points Cost Text")]
	public Text actionPointsText;

	[Header("Quest Status Elements")]
	public Text questTitleText;
	public Text questDescriptionText;
	public Text questExperienceText;
    public GameObject speechInteractionWindow;
    public Text speechInteractionText;

	[Header("Tile Editor UI Elements")]
	public GameObject tileEditorWindow;

    //-----METHODS-----

	//Setup Method
    /// <summary>
    /// 
    /// </summary>
	public void Initialise () {
		mainCamera = Camera.main;
		HideEnemyStatusBar();
		HideFeetCostText();
        HideSpeechChat();
	}

	//Change the users cursor to the type parsed
    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    public void ChangeCursorTo (CursorType type) {
        //TODO
    }

    /// <summary>
    /// 
    /// </summary>
	public void UnhideUI () {
		uiRootCanvas.enabled = true;
	}

    /// <summary>
    /// 
    /// </summary>
	public void HideUI () {
		uiRootCanvas.enabled = false;
	}

    /// <summary>
    /// 
    /// </summary>
	public void SetToTileEditorMode () {
		DisableGameUI();
		tileEditorWindow.SetActive(true);
	}

    /// <summary>
    /// 
    /// </summary>
	public void SetToGameMode () {
		tileEditorWindow.SetActive(false);
	}

	//Update the health, mana and action points to that of the selected hero
    /// <summary>
    /// 
    /// </summary>
	public void UpdateHeroStatusBar () {
		//Get the current selected hero
		CharacterData heroData = PlayerManager.instance.SelectedHero.CharacterData;

		//Update the health bar
		healthBarFill.fillAmount = (float) heroData.GetResourceOfType(ResourceType.HEALTH) / heroData.maxHealth;
		healthBarText.text = heroData.GetResourceOfType(ResourceType.HEALTH) + "/" + heroData.maxHealth;

		//Update the mana bar
		manaBarFill.fillAmount = (float) heroData.GetResourceOfType(ResourceType.MANA) / heroData.maxMana;
		manaBarText.text = heroData.GetResourceOfType(ResourceType.MANA) + "/" + heroData.maxMana;

		//Update the action points
		actionPointsFill.fillAmount = (float) heroData.GetResourceOfType(ResourceType.ACTIONPOINTS) / heroData.maxActionPoints;

		//Update the experience bar
		experienceBarFill.fillAmount = (float) heroData.Experience / heroData.LevelToExperience(heroData.Level + 1);
	}

	//Unhide and update the enemies status bar
    /// <summary>
    /// 
    /// </summary>
    /// <param name="enemyController"></param>
	public void ShowStatusBarForEnemy (CharacterController enemyController) {
		//Get the enemy to display
		CharacterData enemyData = enemyController.CharacterData;

		//Active the UI element
		enemyBarWindow.SetActive(true);

		//Update to the new enemies data
		enemyName.text = enemyData.characterName;
		enemyHealthBarFill.fillAmount = (float) enemyData.GetResourceOfType(ResourceType.HEALTH) / enemyData.maxHealth;
		enemyHealthBarText.text = enemyData.GetResourceOfType(ResourceType.HEALTH) + "/" + enemyData.maxHealth;
	}

	//Hide the enemies status bar
    /// <summary>
    /// 
    /// </summary>
	public void HideEnemyStatusBar () {
		enemyBarWindow.SetActive(false);
	}

	//Make the end turn button interactable
    /// <summary>
    /// 
    /// </summary>
	public void EnableEndTurnButton () {
		endTurnButton.interactable = true;
	}

	//Make the end turn button interactable
    /// <summary>
    /// 
    /// </summary>
	public void DisableEndTurnButton () {
		endTurnButton.interactable = false;
	}

	//Enable the ability buttons
    /// <summary>
    /// 
    /// </summary>
	public void EnableAbilityButtons () {
        foreach (Button button in abilityButtons) {
            button.interactable = true;
        }
	}

	//Disable the ability buttons
    /// <summary>
    /// 
    /// </summary>
	public void DisableAbilityButtons () {
        foreach (Button button in abilityButtons) {
            button.interactable = false;
        }
	}

	//Enable the move action button
    /// <summary>
    /// 
    /// </summary>
	public void EnableActionButton () {
        foreach (Button button in actionButtons) {
            button.interactable = true;
        }	
	}

	//Disable the move action button
    /// <summary>
    /// 
    /// </summary>
	public void DisableActionButton () {
        foreach (Button button in actionButtons) {
            button.interactable = false;
        }
	}

	//Activate the UI
    /// <summary>
    /// 
    /// </summary>
	public void EnableGameUI () {
		EnableEndTurnButton();
		EnableAbilityButtons();
        EnableActionButton();
	}

	//Deactivate the UI
    /// <summary>
    /// 
    /// </summary>
	public void DisableGameUI () {
		DisableEndTurnButton();
		DisableAbilityButtons();
        DisableActionButton();
	}

	//Update the position and text of the AP cost text
    /// <summary>
    /// 
    /// </summary>
    /// <param name="newWorldPosition"></param>
    /// <param name="newValue"></param>
	public void UpdateFeetCostText (Vector3 newWorldPosition, int newValue) {
		actionPointsText.gameObject.SetActive(true);
		actionPointsText.text = newValue + "/" + PlayerManager.instance.SelectedHero.MovementController.speedInFeet + " ft";
		actionPointsText.rectTransform.position = mainCamera.WorldToScreenPoint(newWorldPosition);		
	}

	//Hide the AP cost Text
    /// <summary>
    /// 
    /// </summary>
	public void HideFeetCostText () {
		actionPointsText.gameObject.SetActive(false);
	}

	//Changes the colour of the Ap text to black
    /// <summary>
    /// 
    /// </summary>
	public void SetSpeedCostToValid () {
		actionPointsText.color = Color.black;
	}

	//Changes the colour of the Ap text to red
    /// <summary>
    /// 
    /// </summary>
	public void SetSpeedCostToInvalid () {
		actionPointsText.color = Color.red;
	}

	//Update the status of the current quest
    /// <summary>
    /// 
    /// </summary>
	public void UpdateQuestStatus () {
		QuestComponent questComp = QuestManager.instance.activeQuest;
        if (questComp == null) {
            questTitleText.text = "";
            questDescriptionText.text = "";
            questExperienceText.text = "";
        } else {
            questTitleText.text = QuestManager.instance.activeQuest.title;
            questDescriptionText.text = QuestManager.instance.activeQuest.description;
            questExperienceText.text = "Reward: " + QuestManager.instance.activeQuest.xpReward + " XP";
        }
	}

    public void ShowSpeechChat (string characterName, string speechText) {
        speechInteractionWindow.SetActive(true);        

        StartCoroutine(SpeechChatCoroutine(characterName, speechText));
    }

    IEnumerator SpeechChatCoroutine (string characterName, string speechText) {
        int charPtr = 0;
        speechInteractionText.text = characterName + ":\n\"";
        while (charPtr < speechText.Length) {
            DisableGameUI();
            speechInteractionText.text += speechText[charPtr];
            charPtr++;
            yield return new WaitForSeconds(0.025f);
        }

        speechInteractionText.text += "\"";
    }

    public void HideSpeechChat() {
        speechInteractionWindow.SetActive(false);
        EnableGameUI();
    }

}
