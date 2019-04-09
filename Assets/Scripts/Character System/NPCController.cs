using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : CharacterController, IInteractable {

    //-----VARIABLES-----
	
    
	
	//-----METHODS-----
	
    
    
    //-----INTERFACES-----

    /// <summary>
    /// 
    /// </summary>
    public override void Initialise () {
        base.Initialise();
    }

    /// <summary>
    /// 
    /// </summary>
    public void TriggerInteraction() {
        QuestObjective questObjectiveScript = GetComponent<QuestObjective>();
        if (questObjectiveScript != null) {
            if (QuestManager.instance.activeQuest.questType == QuestComponent.QuestType.HAND_IN) {
                if (questObjectiveScript.IsHandInPointForActiveQuest() == true) {
                    QuestManager.instance.activeQuest.CompleteQuest();
                }
            } else {
                QuestComponent questComp = questObjectiveScript.TriggerNextQuest();
                if (questComp != null) {
                    questComp.StartQuest();
                }
            }
        }

        UIManager.instance.ShowSpeechChat(CharacterData.characterName, QuestManager.instance.activeQuest.interactionText);
    }

    //-----GIZMOS-----
    //public bool drawGizmos;
    void OnDrawGizmos() {
	
	}
	
}
