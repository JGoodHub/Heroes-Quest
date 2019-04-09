using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour, IInteractable {

    //-----VARIABLES-----

    private Animator chestAnimator;

    //-----METHODS-----

    /// <summary>
    /// 
    /// </summary>
    public void Initialise () {
        chestAnimator = GetComponent<Animator>();
    }

    //-----INTERFACES-----

    /// <summary>
    /// 
    /// </summary>
    public void TriggerInteraction () {
        chestAnimator.SetTrigger("Open");

        QuestObjective questObject = GetComponent<QuestObjective>();
        if (questObject != null) {
            questObject.TriggerNextQuest().StartQuest();
        }
    }

}
