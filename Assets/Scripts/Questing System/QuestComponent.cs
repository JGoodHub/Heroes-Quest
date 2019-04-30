using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestComponent {

    //-----ENUMS-----

    public enum QuestType {INTERACT, ENCOUNTER, HAND_IN};

    //-----VARIABLES-----

    public string title;
    public string description;
    public string interactionText;
    public int xpReward;
    public QuestType questType;
    
    public HashSet<QuestComponent> followUpQuests = new HashSet<QuestComponent>();

    //-----METHODS-----

    /// <summary>
    /// Sets up the component
    /// </summary>
    /// <param name="title">Title of the objective</param>
    /// <param name="description">Description of the objective</param>
    /// <param name="xpReward">The reward for completing the objective</param>
    /// <param name="questType">The type of objective</param>
    public QuestComponent (string title, string description, int xpReward, QuestType questType) {
        this.title = title;
        this.description = description;
        this.xpReward = xpReward;
        this.questType = questType;
    }


    /// <summary>
    /// Starts the objective for this component
    /// </summary>
    public void StartQuest () {
        QuestManager.instance.activeQuest = this;
        UIManager.instance.UpdateQuestStatus();
        UIManager.instance.DisplayMessage("----- Quest Complete, Objective Updated -----", Color.yellow, 5f);
    }
    
    /// <summary>
    /// Competes the quest component
    /// </summary>
    public void CompleteQuest () {
        foreach (CharacterController heroController in PlayerManager.instance.HeroSet) {
            heroController.CharacterData.ApplyChangeToData(new StatChange(ResourceType.EXPERIENCE, xpReward / PlayerManager.instance.HeroSet.Count));
        }
    }
    
}
 