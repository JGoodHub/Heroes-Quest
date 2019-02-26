using System.Collections;
using UnityEngine;

public interface IGraphObject {

    Vertex CurrentVertex { get; set; }

    void CalculateCurrentVertex ();

}