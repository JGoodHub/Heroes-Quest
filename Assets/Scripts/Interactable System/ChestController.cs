using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GraphObstacle))]
public class ChestController : MonoBehaviour, IInteractable, IQuestable {

    //-----VARIABLES-----

    private Animator chestAnimator;

    [SerializeField]
    private QuestComponent linkedQuestComponent;
    public QuestComponent LinkedQuestComponent { get => linkedQuestComponent; set => linkedQuestComponent = value; }

    private GraphObstacle graphObstacleComponent;
    public GraphObstacle GraphObstacleComponent { get => graphObstacleComponent; set => graphObstacleComponent = value; }

    //-----METHODS-----

    public void Initialise () {
        chestAnimator = GetComponent<Animator>();
        graphObstacleComponent = GetComponent<GraphObstacle>();
    }

    //-----INTERFACES-----

    public void TriggerInteraction () {
        chestAnimator.SetTrigger("Open");
        CompleteQuest();        
    }

    public void Reset () {
        chestAnimator.SetTrigger("Close");
    }    

    public void CompleteQuest () {
        if (linkedQuestComponent != null) {
            linkedQuestComponent.CompleteQuest();
        } else {
            //Reward the player with gold or an item
        }
    }

}
