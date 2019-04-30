using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestObjective : MonoBehaviour {

    //-----STRUCTS-----

    [System.Serializable]
    public struct QuestTrigger {
        public string sourceQuestName;
        public string triggerQuestName;
    }

    //-----VARIABLES-----

    public QuestTrigger[] questTriggers;

    public string[] handInQuestNames;

    public GameObject marker;
	
	//-----METHODS-----
	
    /// <summary>
    /// Triggers the next quest once this objective is complete
    /// </summary>
    /// <returns></returns>
    public QuestComponent TriggerNextQuest() {
        foreach (QuestTrigger trigger in questTriggers) {
            if (trigger.sourceQuestName == QuestManager.instance.activeQuest.title) {
                QuestComponent nextQuest = QuestManager.instance.GetQuestUsingName(trigger.triggerQuestName);
                if (nextQuest == null) {
                    Debug.LogError("No matching quest with the same title could be found, check spelling of trigger quest");
                } else {
                    nextQuest.StartQuest();
                    return nextQuest;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Check if this is the finishing point for the current quest
    /// </summary>
    /// <returns></returns>
    public bool IsHandInPointForActiveQuest () {
        foreach (string questName in handInQuestNames) {
            if (questName == QuestManager.instance.activeQuest.title) {
                return true;
            }
        }

        return false;
    }	
}
