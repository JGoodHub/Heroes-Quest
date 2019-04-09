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
    /// 
    /// </summary>
    /// <param name="_title"></param>
    /// <param name="_description"></param>
    /// <param name="_xpReward"></param>
    /// <param name="_questType"></param>
    public QuestComponent (string _title, string _description, int _xpReward, QuestType _questType) {
        title = _title;
        description = _description;
        xpReward = _xpReward;
        questType = _questType;
    }


    /// <summary>
    /// 
    /// </summary>
    public void StartQuest () {
        QuestManager.instance.activeQuest = this;
        UIManager.instance.UpdateQuestStatus();
    }
    
    public void CompleteQuest () {
        foreach (CharacterController heroController in PlayerManager.instance.HeroSet) {
            heroController.CharacterData.ApplyChangeToData(new StatChange(ResourceType.EXPERIENCE, xpReward / PlayerManager.instance.HeroSet.Count));
        }
    }
    
}
 