using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour, IInteractable {

    //-----VARIABLES-----

    private Animator chestAnimator;

    public GameObject highlight;


    //-----INTERFACES-----

    /// <summary>
    /// Setup the component references
    /// </summary>
    public void Initialise() {
        chestAnimator = GetComponent<Animator>();
    }

    /// <summary>
    /// Highlight the object
    /// </summary>
    public void HighlightObject() {
        highlight.SetActive(true);
    }

    /// <summary>
    /// Unhighlight the object
    /// </summary>
    public void UnhighlightObject() {
        highlight.SetActive(false);
    }

    /// <summary>
    /// Trigger the quest interaction with the chest
    /// </summary>
    public void TriggerInteraction() {
        chestAnimator.SetTrigger("Open");

        QuestObjective questObjectiveScript = GetComponent<QuestObjective>();
        if (questObjectiveScript != null) {
            QuestComponent questComp = questObjectiveScript.TriggerNextQuest();
        }
    }

}
