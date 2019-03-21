using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable {

    //-----PROPERTIES-----

    GraphObstacle GraphObstacleComponent {get; set;}

    //

    void Initialise ();
    void TriggerInteraction ();
    void Reset ();
    
}
