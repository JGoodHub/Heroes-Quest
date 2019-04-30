using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable {

    //-----METHODS-----

    void Initialise();

    void HighlightObject();
    void UnhighlightObject();

    void TriggerInteraction();
    
}
