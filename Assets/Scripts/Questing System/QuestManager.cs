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

    public QuestComponent activeQuest;

    //-----METHODS-----

    /// <summary>
    /// Sets up the quest collection and starts the demo quest
    /// </summary>
    public void Initialise () {
        QuestCollection.instance.Initialise();

        activeQuest = GetQuestUsingName(startingQuestName);
        activeQuest.StartQuest();
    }

    /// <summary>
    /// Find a quest by a given name
    /// </summary>
    /// <param name="questTitle">The quest to search for</param>
    /// <returns>The Quest Component associated with the name</returns>
    public QuestComponent GetQuestUsingName (string questTitle) {
        QuestComponent tempComponent = null;
        foreach (QuestComponent questComp in QuestCollection.quests) {
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
