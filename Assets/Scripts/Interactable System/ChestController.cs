using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour, IInteractable {

    //-----VARIABLES-----

    private Animator chestAnimator;

    public QuestObjective questObjective;

    private Vertex currentVertex;
    public Vertex CurrentVertex { get => currentVertex; set => currentVertex = value; }

    //-----METHODS-----


    //-----INTERFACES-----

    public void Initialise () {
        chestAnimator = GetComponent<Animator>();
        CalculateCurrentVertex();
    }

    public void TriggerInteraction () {
        chestAnimator.SetTrigger("Open");

        if (questObjective != null) {
            questObjective.TriggerEndOfQuestObjective();
        }
    }

    public void Reset () {
        chestAnimator.SetTrigger("Close");
    }

    public void CalculateCurrentVertex () {
        currentVertex = PathfindingManager.instance.Graph.GetClosestVertexToCoordinates(PathfindingManager.TranslateWorldSpaceToGraphCoordinates(transform.position));
        currentVertex.blocked = true;
    }
}
