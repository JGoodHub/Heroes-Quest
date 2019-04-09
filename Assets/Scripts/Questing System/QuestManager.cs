using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour {
    
    //-----SINGLETON SETUP-----

	public static QuestManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    public string startingQuestName;

    [HideInInspector]
    public QuestComponent activeQuest;

    //-----METHODS-----

    public void Initialise () {
        QuestBook.instance.Initialise();

        activeQuest = GetQuestUsingName(startingQuestName);
        activeQuest.StartQuest();
    }

    public QuestComponent GetQuestUsingName (string questTitle) {
        QuestComponent tempComponent = null;
        foreach (QuestComponent questComp in QuestBook.Quests) {
            if (questComp.title.ToLower() == questTitle.ToLower()) {
                if (tempComponent != null) {
                    Debug.LogError("MULTIPLE QUESTS WITH THE SAME TITLE DETECTED");
                } else {
                    tempComponent = questComp;
                }                
            }
        }

        if (tempComponent == null) {
            Debug.LogError("No quest with the name " + questTitle + " found");
        }

        return tempComponent;
    }
    


}
