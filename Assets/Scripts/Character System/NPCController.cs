using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : CharacterController, IInteractable {

    //-----INTERFACES-----

    /// <summary>
    /// Setup the base class
    /// </summary>
    public override void Initialise () {
        base.Initialise();
    }

    /// <summary>
    /// Trigger the interact with the NPC, competing or handing in quests as necessary
    /// </summary>
    public void TriggerInteraction() {
        QuestObjective questObjectiveScript = GetComponent<QuestObjective>();
        if (questObjectiveScript != null) {
            if (QuestManager.instance.activeQuest.questType == QuestComponent.QuestType.HAND_IN) {
                if (questObjectiveScript.IsHandInPointForActiveQuest() == true) {
                    QuestManager.instance.activeQuest.CompleteQuest();
                    QuestManager.instance.activeQuest = null;
                    UIManager.instance.UpdateQuestStatus();
                    UIManager.instance.DisplayMessage("----- QUEST COMPLETED -----", Color.yellow, 5f);
                }
            } else {
                QuestComponent questComp = questObjectiveScript.TriggerNextQuest();                
            }
        }

        //Show the speech chat window for the new quest
        UIManager.instance.ShowSpeechChat(CharacterData.characterName, QuestManager.instance.activeQuest.interactionText);
    }

    /// <summary>
    /// Highlight the NPC
    /// </summary>
    public void HighlightObject() {
        base.Highlight();
    }

    /// <summary>
    /// Highlight the NPC
    /// </summary>
    public void UnhighlightObject() {
        base.Unhighlight();
    }

	
}
