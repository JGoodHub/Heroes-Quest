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

    //public string[] triggerQuestNames;
    //private int triggerState = 0;

    public QuestTrigger[] questTriggers;

    public string[] handInQuestNames;

    public GameObject marker;
	
	//-----METHODS-----
	
    /// <summary>
    /// 
    /// </summary>
	public void Initialise () {
		
	}

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public QuestComponent TriggerNextQuest() {
        /*
        if (triggerState < triggerQuestNames.Length) {
            QuestComponent linkedComp = QuestManager.instance.SearchForQuest(triggerQuestNames[triggerState]);
            if (linkedComp != null && QuestManager.instance.activeQuest.outQuests.Contains(linkedComp)) {
                triggerState++;
                return linkedComp;
            } else {
                if (linkedComp == null) {
                    Debug.LogError("No matching quest with the same title could be found, check spelling of trigger quest");
                } else {
                    Debug.Log("Player trying to activate next quest before completeing its previous one");
                }
                return null;
            }
        } else {
            return null;
        }
        */

        foreach (QuestTrigger trigger in questTriggers) {
            if (trigger.sourceQuestName == QuestManager.instance.activeQuest.title) {
                QuestComponent nextQuest = QuestManager.instance.GetQuestUsingName(trigger.triggerQuestName);
                if (nextQuest == null) {
                    Debug.LogError("No matching quest with the same title could be found, check spelling of trigger quest");
                } else {
                    return nextQuest;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 
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

	
	//-----GIZMOS-----
	//public bool drawGizmos;
	void OnDrawGizmos() {
	
	}
	
}
