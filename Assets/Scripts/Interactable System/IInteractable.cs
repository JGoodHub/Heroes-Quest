using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable {

    Vertex CurrentVertex { get; set; }

    void Initialise ();
    void TriggerInteraction ();
    void CalculateCurrentVertex ();
    void Reset ();
    
}
