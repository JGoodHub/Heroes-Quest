using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestComponent : MonoBehaviour {

    //-----VARIABLES-----

    public string title;
    [TextArea]
    public string description;

    public QuestObjective objective;
    public QuestComponent dominoQuest;
    public string experienceReward;

    //-----METHODS-----

    public void StartQuest () {
        QuestManager.instance.SetActiveQuest(this);
        UIManager.instance.UpdateQuestStatus();
    }

    public void CompleteQuest () {

    }


}