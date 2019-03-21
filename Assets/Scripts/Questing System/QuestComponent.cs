using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestComponent : MonoBehaviour {

    //-----ENUMS-----

    public enum QuestType {INTERACT, ENCOUNTER};

    //-----VARIABLES-----

    public string title;
    [TextArea]
    public string description;

    public QuestType questType;

    public QuestComponent dominoQuest;
    public int experienceReward;

    //-----METHODS-----

    public void StartQuest () {
        QuestManager.instance.SetActiveQuest(this);
        UIManager.instance.UpdateQuestStatus();
    }

    public void CompleteQuest () {
        if (dominoQuest == null) {
            if (questType == QuestType.INTERACT) {
                PlayerManager.instance.SelectedHero.CharacterData.ApplyChangeToData(new StatChange(ResourceType.EXPERIENCE, experienceReward));
            } else if (questType == QuestType.ENCOUNTER) {
                foreach (CharacterController heroController in PlayerManager.instance.HeroSet) {
                    heroController.CharacterData.ApplyChangeToData(new StatChange(ResourceType.EXPERIENCE, experienceReward / PlayerManager.instance.HeroSet.Count));
                }
            }
        } else {
            dominoQuest.StartQuest();
        }

    }


}