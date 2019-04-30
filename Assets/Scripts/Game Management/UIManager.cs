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
    

    //-----VARIABLES-----

	private Camera mainCamera;

	[Header("UI Canvas Root")]
	public Canvas uiRootCanvas;
    
	[Header("Selected Character UI Elements")]
	public Image healthBarFill;
	public Text healthBarText;
	public Image manaBarFill;
	public Text manaBarText;
	public GameObject actionPointsFill;
	public Image experienceBarFill;

	[Header("Enemy Status Bar UI Elements")]
	public GameObject enemyBarWindow;
	public Text enemyName;
	public Image enemyHealthBarFill;
	public Text enemyHealthBarText;

	[Header("Turn Management UI Elements")]
	public Button endTurnButton;
    public GameObject endTurnButtonObject;

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

    [Header("Display Message Elements")]
    public Text messageText;

	[Header("Tile Editor UI Elements")]
	public GameObject tileEditorWindow;

    //-----METHODS-----

    /// <summary>
    /// Pre hide the appropriate UI elements
    /// </summary>
	public void Initialise () {
		mainCamera = Camera.main;
		HideEnemyStatusBar();
		HideFeetCostText();
        HideSpeechChat();
	}

    /// <summary>
    /// Show the UI
    /// </summary>
	public void ShowUI () {
		uiRootCanvas.enabled = true;
	}

    /// <summary>
    /// Hide the UI
    /// </summary>
	public void HideUI () {
		uiRootCanvas.enabled = false;
	}

    /// <summary>
    /// Set the UI to editor mode
    /// </summary>
	public void SetToTileEditorMode () {
		DisablePlayerControls();
		tileEditorWindow.SetActive(true);
	}

    /// <summary>
    /// Set the UI to game mode
    /// </summary>
	public void SetToGameMode () {
		tileEditorWindow.SetActive(false);
	}

    /// <summary>
    /// Update the health, mana and action points to that of the selected hero
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

        if (heroData.actionAvailable) {
            EnableActionPoint();
        } else {
            DisableActionPoint();
        }
        
		//Update the experience bar
		experienceBarFill.fillAmount = (float) heroData.experience / heroData.LevelToExperience(heroData.level + 1);
	}

    /// <summary>
    /// Show and update the enemies status bar
    /// </summary>
    /// <param name="enemyController">The enemy to display</param>
    public void ShowStatusBarForEnemy (CharacterController enemyController) {
		//Get the enemy to display
		CharacterData enemyData = enemyController.CharacterData;

		//Active the UI element
		enemyBarWindow.SetActive(true);

		//Update to the new enemies data
		enemyName.text = enemyData.characterName;
        float healthPercentage = (float) enemyData.GetResourceOfType(ResourceType.HEALTH) / enemyData.maxHealth;      
        
        if (healthPercentage > 0f) {
            enemyHealthBarText.text = "Brutalised";
        }

        if (healthPercentage >= 0.25f) {
            enemyHealthBarText.text = "Blooided";
        }

        if (healthPercentage >= 0.5f) {
            enemyHealthBarText.text = "Injured";
        }

        if (healthPercentage >= 0.75f) {
            enemyHealthBarText.text = "Hurt Feelings";
        }

        if (healthPercentage >= 1f) {
            enemyHealthBarText.text = "Unharmed";
        }

    }

    /// <summary>
    /// Hide the enemies status bar
    /// </summary>
    public void HideEnemyStatusBar () {
		enemyBarWindow.SetActive(false);
	}

    /// <summary>
    /// Enable the action point
    /// </summary>
    public void EnableActionPoint () {
        actionPointsFill.SetActive(true);
    }

    /// <summary>
    /// Disable the action point
    /// </summary>
    public void DisableActionPoint () {
        actionPointsFill.SetActive(false);
    }

    /// <summary>
    /// Enable the end turn button
    /// </summary>
	public void EnableEndTurnButton () {
        endTurnButton.interactable = true;
    }

    /// <summary>
    /// Disable the end turn button
    /// </summary>
    public void DisableEndTurnButton () {
        endTurnButton.interactable = false;
    }

    /// <summary>
    /// Show the end turn button
    /// </summary>
    public void ShowEndTurnButton () {
        endTurnButtonObject.SetActive(true);
    }

    /// <summary>
    /// Hide the end turn button
    /// </summary>
    public void HideEndTurnButton() {
        endTurnButtonObject.SetActive(false);
    }

    /// <summary>
    /// Enable the player controls
    /// </summary>
    public void EnablePlayerControls () {
        foreach (Button button in abilityButtons) {
            button.interactable = true;
        }

        foreach (Button button in actionButtons) {
            button.interactable = true;
        }
	}

    /// <summary>
    /// Disable the player controls
    /// </summary>
	public void DisablePlayerControls () {
        foreach (Button button in abilityButtons) {
            button.interactable = false;
        }

        foreach (Button button in actionButtons) {
            button.interactable = false;
        }
    }

    /// <summary>
    /// Update the position and text of the feet cost text
    /// </summary>
    /// <param name="newWorldPosition">The world position to hover over</param>
    /// <param name="newValue">The new vale of the feet text</param>
    public void UpdateFeetCostText (Vector3 newWorldPosition, int newValue) {
		actionPointsText.gameObject.SetActive(true);
        actionPointsText.rectTransform.position = mainCamera.WorldToScreenPoint(newWorldPosition);

        if (newValue >= 1000) {
            actionPointsText.text = (newValue - 1000) + "/∞ ft";
        } else {
            actionPointsText.text = newValue + "/" + PlayerManager.instance.SelectedHero.MovementController.speedInFeet + " ft";
        }
    }

    /// <summary>
    /// Hid the feet text
    /// </summary>
	public void HideFeetCostText () {
		actionPointsText.gameObject.SetActive(false);
	}

    /// <summary>
    /// Change the colour of the feet text to black, valid
    /// </summary>
	public void SetSpeedCostToValid () {
		actionPointsText.color = Color.black;
	}

    /// <summary>
    /// Change the colour of the feet text to red, invalid
    /// </summary>
	public void SetSpeedCostToInvalid () {
		actionPointsText.color = Color.red;
	}

    /// <summary>
    /// Update the status of the current quest
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

    IEnumerator speechCoroutine = null;

    /// <summary>
    /// Show the NPC speech chat element
    /// </summary>
    /// <param name="characterName">The name of the NPC</param>
    /// <param name="speechText">The NPC's text</param>
    public void ShowSpeechChat (string characterName, string speechText) {
        speechInteractionWindow.SetActive(true);
        speechCoroutine = SpeechChatCoroutine(characterName, speechText);

        StartCoroutine(speechCoroutine);
    }

    /// <summary>
    /// Write the speech text to the speech window
    /// </summary>
    /// <param name="characterName">NPC name</param>
    /// <param name="speechText">NPC's text</param>
    /// <returns></returns>
    IEnumerator SpeechChatCoroutine (string characterName, string speechText) {
        int charPtr = 0;
        speechInteractionText.text = characterName + ":\n\"";

        bool inDotSequence = false;
        while (charPtr < speechText.Length) {
            DisablePlayerControls();
            speechInteractionText.text += speechText[charPtr];
            charPtr++;

            if (charPtr < speechText.Length - 1) {
                if (speechText[charPtr] == '.' && speechText[charPtr + 1] == '.' || inDotSequence) {
                    inDotSequence = true;
                    yield return new WaitForSeconds(1f);
                } else {
                    yield return new WaitForSeconds(0.04f);
                }

                if (speechText[charPtr] != '.') {
                    inDotSequence = false;
                }
            }            
        }

        speechInteractionText.text += "\"";
        speechCoroutine = null;
    }

    /// <summary>
    /// Hide the speech window
    /// </summary>
    public void HideSpeechChat() {
        speechInteractionWindow.SetActive(false);
        if (speechCoroutine != null) {
            StopCoroutine(speechCoroutine);
            speechCoroutine = null;
        }

        EnablePlayerControls();
    }

    /// <summary>
    /// Display a message on the screen
    /// </summary>
    /// <param name="message">The message to be displayed</param>
    /// <param name="textColour">The colour of the text</param>
    /// <param name="messageDuration">How long to display it for</param>
    public void DisplayMessage (string message, Color textColour, float messageDuration) {
        StartCoroutine(DisplayMessageCoroutine(message, textColour, messageDuration));
    }

    /// <summary>
    /// Hid the message after several seconds
    /// </summary>
    /// <param name="message">The message to be displayed</param>
    /// <param name="textColour">The colour of the text</param>
    /// <param name="messageDuration">How long to display it for</param>
    /// <returns></returns>
    IEnumerator DisplayMessageCoroutine (string message, Color textColour, float messageDuration) {
        messageText.text = message;
        messageText.color = textColour;
        yield return new WaitForSeconds(messageDuration);
        messageText.text = "";
    }

}
