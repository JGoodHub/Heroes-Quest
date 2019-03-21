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

    public enum CursorType {DEFAULT, MOVE, INTERACT, ATTACK, STOP};

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
	public GameObject enemyBarObject;
	public Text enemyName;
	public Image enemyHealthBarFill;
	public Text enemyHealthBarText;

	[Header("Turn Management UI Elements")]
	public Button endTurnButton;

	[Header("Ability Button UI Elements")]
	public Button abilityOneButton;
	public Button abilityTwoButton;
	public Button abilityThreeButton;
	public Button abilityFourButton;

	[Header("Action Button UI Elements")]
	public Button moveButton;
	public Button interactButton;
	public Button attackButton;
	public Button stopButton;

	[Header("Action points Cost Text")]
	public Text actionPointsText;

	[Header("Quest Status Elements")]
	public Text questTitleText;
	public Text questDescriptionText;
	public Text questExperienceText;

    //-----METHODS-----

	//Setup Method
	public void Initialise () {
		mainCamera = Camera.main;
		HideEnemyStatusBar();
		HideAPCostText();
	}

	//Change the users cursor to the type parsed
    public void ChangeCursorTo (CursorType type) {
        //TODO
    }

	public void UnhideUI () {
		uiRootCanvas.enabled = true;
	}

	public void HideUI () {
		uiRootCanvas.enabled = false;
	}

	//Update the health, mana and action points to that of the selected hero
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
	public void ShowStatusBarForEnemy (CharacterController enemyToDisplay) {
		//Get the enemy to display
		CharacterData enemyData = enemyToDisplay.CharacterData;

		//Active the UI element
		enemyBarObject.SetActive(true);

		//Update to the new enemies data
		enemyName.text = enemyData.characterName;
		enemyHealthBarFill.fillAmount = (float) enemyData.GetResourceOfType(ResourceType.HEALTH) / enemyData.maxHealth;
		enemyHealthBarText.text = enemyData.GetResourceOfType(ResourceType.HEALTH) + "/" + enemyData.maxHealth;
	}

	//Hide the enemies status bar
	public void HideEnemyStatusBar () {
		enemyBarObject.SetActive(false);
	}

	//Make the end turn button interactable
	public void EnableEndTurnButton () {
		endTurnButton.interactable = true;
	}

	//Make the end turn button interactable
	public void DisableEndTurnButton () {
		endTurnButton.interactable = false;
	}

	//Enable the ability buttons
	public void EnableAbilityButtons () {
		abilityOneButton.interactable = true;
		abilityTwoButton.interactable = true;
		abilityThreeButton.interactable = true;
		abilityFourButton.interactable = true;
	}

	//Disable the ability buttons
	public void DisableAbilityButtons () {
		abilityOneButton.interactable = false;
		abilityTwoButton.interactable = false;
		abilityThreeButton.interactable = false;
		abilityFourButton.interactable = false;
	}

	//Enable the move action button
	public void EnableActionButton (int actionID) {
		switch (actionID) {
			case 0:
				moveButton.interactable = true;
				break;
			case 1:
				interactButton.interactable = true;
				break;
			case 2:
				attackButton.interactable = true;
				break;
			case 3:
				stopButton.interactable = true;
				break;
			default:
				return;
		}		
	}

	//Disable the move action button
	public void DisableActionButton (int actionID) {
		switch (actionID) {
			case 0:
				moveButton.interactable = false;
				break;
			case 1:
				interactButton.interactable = false;
				break;
			case 2:
				attackButton.interactable = false;
				break;
			case 3:
				stopButton.interactable = false;
				break;
			default:
				return;
		}
	}

	//Activate the UI
	public void EnableUI () {
		EnableEndTurnButton();
		EnableAbilityButtons();
        for (int id = 0; id < 4; id++) {
			EnableActionButton(id);
		}
	}

	//Deactivate the UI
	public void DisableUI () {
		DisableEndTurnButton();
		DisableAbilityButtons();
        for (int id = 0; id < 4; id++) {
			DisableActionButton(id);
		}
	}

	//Update the position and text of the AP cost text
	public void UpdateAPCostText (Vector3 newWorldPosition, int newValue) {
		actionPointsText.gameObject.SetActive(true);
		actionPointsText.text = newValue + "AP";
		actionPointsText.rectTransform.position = mainCamera.WorldToScreenPoint(newWorldPosition);		
	}

	//Hide the AP cost Text
	public void HideAPCostText () {
		actionPointsText.gameObject.SetActive(false);
	}

	//Changes the colour of the Ap text to black
	public void SetAPCostToValid () {
		actionPointsText.color = Color.black;
	}

	//Changes the colour of the Ap text to red
	public void SetAPCostToInvalid () {
		actionPointsText.color = Color.red;
	}

	//Update the status of the current quest
	public void UpdateQuestStatus () {
		questTitleText.text = QuestManager.instance.ActiveQuest.title;
		questDescriptionText.text = QuestManager.instance.ActiveQuest.description;

		//Find the last quest component in the chain and update the experience of the quest as a whole
		QuestComponent questComp = QuestManager.instance.ActiveQuest;
		do {
			if (questComp.dominoQuest == null) {
				questExperienceText.text = "Reward: " + questComp.experienceReward + " XP";
				return;
			} else {
				questComp = questComp.dominoQuest;
			}			
		} while (questComp != null);
	}
	
}
