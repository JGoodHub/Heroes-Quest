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

    public QuestComponent startingQuest;
    private QuestComponent activeQuest;
    public QuestComponent ActiveQuest { get => activeQuest; }

    //-----METHODS-----

    public void Initialise () {
        SetActiveQuest(startingQuest);
        activeQuest.StartQuest();
    }

    public void SetActiveQuest (QuestComponent newActiveQuest) {
        activeQuest = newActiveQuest;
    }

    


}
