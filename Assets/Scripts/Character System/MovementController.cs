using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

    //-----VARIABLES-----

    //The current vertex the character is on
    private Vertex currentVertex;
    public Vertex CurrentVertex { get => currentVertex; }

    //Variables for moving the character
    public float speedInMeters;
    private float yBounce = 5.5f;
    private IEnumerator moveCharacterCoroutine;    

    //Is the character currently moving or able to move at all
    private bool lockedDown = false;
    private bool currentlyMoving;
    public bool CurrentlyMoving { get => currentlyMoving; }

    //Distance the character has moved this turn
    private float distanceMovedThisTurn = 0;

    //Reference to the character controller
    private CharacterController characterController;

    //-----METHODS-----

    //Setup Method
    public virtual void Initialise () {
        currentVertex = PathfindingManager.instance.Graph.GetClosestVertexToCoordinates(PathfindingManager.TranslateWorldSpaceToGraphCoordinates(transform.position));
        currentVertex.blocked = true;     

        characterController = GetComponent<CharacterController>();
    }

    //Lock the character to prevent it from moving
    public void LockCharacter () {
        lockedDown = true;
    }

    //Unlock the character to allow it to move
    public void UnlockCharacter () {
        lockedDown = false;
    }

    //Resets the distance moved counter
    public void ResetDistanceMoved () {
        distanceMovedThisTurn = 0;
    }

    //Checks if the character can move this turn
    public bool CanCharacterMove (float distance) {
        return distance <= speedInMeters - distanceMovedThisTurn;
    }

    //The number of Ap this movement would cost
    public int APCostOfMovement (Path path) {
        float distancePerAP = speedInMeters / characterController.CharacterData.maxActionPoints;
        return Mathf.CeilToInt(path.GetPathLength() / distancePerAP);
    }    

    //Check if the character is able to move and call the corresponding coroutine, overriding any current path
    public void MoveCharacter (Path path, float delay) {
        if (!lockedDown) {
            //If the characters moving stop them
            if (currentlyMoving) {
                StopCoroutine(moveCharacterCoroutine);
            }

            //Start the character moving on their new path
            moveCharacterCoroutine = MoveCharacterCoroutine(path, delay);
            StartCoroutine(moveCharacterCoroutine);
        }
    }

    //Coroutine for moving the character
    private IEnumerator MoveCharacterCoroutine (Path path, float delay) {
        //If the character is casting an ability don't let them move
        if (AbilityManager.instance.abilityRunning) {
            yield break;
        } else {
            currentlyMoving = true;
        }

        //Disable the users ability to move again until the current move is finished
        UIManager.instance.DisableUI();

        //Unblock the starting vertex
        currentVertex.blocked = false;

        //Wait for the specified delay
        yield return new WaitForSeconds(delay);

        //Iterate over each vertex in the path
        int pathPtr = 1;
        while (pathPtr < path.Vertices.Count) {
            //Get the next vertex's world position 
            Vector3 nextVertexWorldPosition = path.Vertices[pathPtr].WorldPosition;
            if (Vector3.Distance(transform.position, nextVertexWorldPosition) > 0.1f) {
                LookAtVector(nextVertexWorldPosition);

                //Movement consists of two stages
                //Stage One - Horizontal movement                
                transform.position = Vector3.MoveTowards(transform.position, nextVertexWorldPosition, 20 * Time.deltaTime);

                //Stage Two - Vertical movement
                float distanceToLastVertex = Vector3.Distance(transform.position, path.Vertices[pathPtr - 1].WorldPosition);
                float distanceToNextVertex = Vector3.Distance(transform.position, nextVertexWorldPosition);
                transform.position = new Vector3(transform.position.x, Mathf.PingPong(distanceToLastVertex / (distanceToLastVertex + distanceToNextVertex), 0.5f) * yBounce, transform.position.z);
            } else {
                //set the character current vertex to the last one it stepped on
                currentVertex = path.Vertices[pathPtr];
                pathPtr++;                
            }
            
            yield return null;
        }

        //Clear flags and re-active buttons
        currentlyMoving = false;
        currentVertex.blocked = true;
        UIManager.instance.EnableUI();
        distanceMovedThisTurn += path.GetPathLength();
    }

    public void LookAtVector (Vector3 lookAtTarget) {
        Vector3 newForward = lookAtTarget - transform.position;
        transform.forward = new Vector3(newForward.x, 0, newForward.z).normalized;
    }

}