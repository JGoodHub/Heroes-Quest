using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GraphObstacle))]
public class MovementController : MonoBehaviour {

    //-----VARIABLES-----

    //Variables for moving the character
    public float speedInMeters;
    private float yBounce = 5.5f;
    private float yFloor = 0f;
    private IEnumerator moveCharacterCoroutine;    

    //Is the character currently moving or able to move at all
    public bool lockedDown = false;
    private bool currentlyMoving;
    public bool CurrentlyMoving { get => currentlyMoving; }

    //Distance the character has moved this turn
    private float distanceMovedThisTurn = 0;

    //Reference to the character controller
    private CharacterController characterController;
    private GraphObstacle graphObstacle;
    public GraphObstacle GraphObstacle { get => graphObstacle; }

    //-----METHODS-----

    //Setup Method
    public virtual void Initialise () {
        characterController = GetComponent<CharacterController>();
        graphObstacle = GetComponent<GraphObstacle>();

    }

    //Resets the distance moved counter
    public void ResetDistanceMoved () {
        distanceMovedThisTurn = 0;
    }

    //Checks if the character can move this turn
    public bool CanMoveThatDistance (float distance) {
        return distance <= speedInMeters - distanceMovedThisTurn;
    }

    //The number of Ap this movement would cost
    public int APCostOfMovement (Path path) {
        float distancePerAP = speedInMeters / characterController.CharacterData.maxActionPoints;
        return Mathf.CeilToInt(path.GetPathLength() / distancePerAP);
    } 

    public Path CheckForEncounterAndTrimPath (Path path) {
        HashSet<Vertex> encounterTriggerVertices = EnemyAIManager.instance.GetAllEncounterTriggerVertices();
        int pathPtr = 0;
        bool matchFound = false;
        while (pathPtr < path.Vertices.Count && matchFound == false) {
            if (encounterTriggerVertices.Contains(path.Vertices[pathPtr])) {
                matchFound = true;
            } else {
                pathPtr++;
            }           
        }

        if (matchFound) {
            path.TrimPath(path.Vertices.Count - (pathPtr + 1));
        } 
        
        return path;
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
            characterController.CharacterData.ApplyChangeToData(new StatChange(ResourceType.ACTIONPOINTS, characterController.MovementController.APCostOfMovement(path) * -1));
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
        graphObstacle.UnblockCurrentVertex();

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
                if (Mathf.Abs(nextVertexWorldPosition.y - path.Vertices[pathPtr - 1].WorldPosition.y) > 0.1f) {

                } else {
                    float distanceToLastVertex = Vector3.Distance(transform.position, path.Vertices[pathPtr - 1].WorldPosition);
                    float distanceToNextVertex = Vector3.Distance(transform.position, nextVertexWorldPosition);
                    transform.position = new Vector3(transform.position.x, (Mathf.PingPong(distanceToLastVertex / (distanceToLastVertex + distanceToNextVertex), 0.5f) * yBounce) + yFloor, transform.position.z);
                }
            } else {
                //set the character current vertex to the last one it stepped on
                graphObstacle.SetCurrentVertex(path.Vertices[pathPtr]);                
                pathPtr++;       
                if (pathPtr < path.Vertices.Count) {
                    yFloor = path.Vertices[pathPtr].WorldPosition.y;       
                } 
            }
            
            yield return null;
        }

        //Clear flags and re-active buttons
        currentlyMoving = false;
        graphObstacle.BlockCurrentVertex();
        UIManager.instance.EnableUI();
        distanceMovedThisTurn += path.GetPathLength();
        PlayerManager.instance.ActionRunning = false;

        //Check if the character is in enemy territory and trigger their turn if so
        if (EnemyAIManager.instance.GetAllEncounterTriggerVertices().Contains(graphObstacle.CurrentVertex)) {
            EnemyAIManager.instance.GetSpecificEncounter(graphObstacle.CurrentVertex).encounterActive = true;
            
            GameManager.instance.EndPlayersTurn();
        }
    }

    public void LookAtVector (Vector3 lookAtTarget) {
        Vector3 newForward = lookAtTarget - transform.position;
        transform.forward = new Vector3(newForward.x, 0, newForward.z).normalized;
    }

}