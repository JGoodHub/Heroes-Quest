using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GraphObstacle))]
public class MovementController : MonoBehaviour {

    //-----VARIABLES-----

    //Variables for moving the character
    public float speedInFeet;
    private float hopingHeight = 6f;
    private float floorHeight;
    private IEnumerator moveCharacterCoroutine;    

    //Is the character currently moving or able to move at all
    [HideInInspector] public bool disableMovement = false;
    public bool infinityMovement = true;
    private bool currentlyMoving;
    public bool CurrentlyMoving { get => currentlyMoving; }

    //Distance the character has moved this turn
    private float feetMovedThisTurn;

    //Reference to the character controller
    private CharacterController characterController;
    private GraphObstacle graphObstacle;
    public GraphObstacle GraphObstacle { get => graphObstacle; }

    //-----METHODS-----

    /// <summary>
    /// Setup the component references
    /// </summary>
    public virtual void Initialise () {
        characterController = GetComponent<CharacterController>();
        graphObstacle = GetComponent<GraphObstacle>();
    }

    /// <summary>
    /// Resets the distance moved counter
    /// </summary>
    public void ResetDistanceMoved () {
        feetMovedThisTurn = 0;
    }

    /// <summary>
    /// Checks if the character can move this turn
    /// </summary>
    /// <param name="distance">The intended distance to move</param>
    /// <returns>Boolean of whether the character can move the distance</returns>
    public bool CanMoveDistance (float distance) {
        if (infinityMovement) {
            return true;
        } else {
            return distance <= speedInFeet - feetMovedThisTurn;
        }
    }

    /// <summary>
    /// Checks if a path intersects with an encounter and if so trims the path to end on the border
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>The path after being trimmed</returns>
    public Path CheckForEncounterAndTrimPath (Path path) {
        HashSet<Vertex> encounterTriggerVertices = EnemyAIManager.instance.GetAllEncounterVertices();
        int pathPtr = 0;
        bool matchFound = false;
        while (pathPtr < path.vertices.Count && matchFound == false) {
            if (encounterTriggerVertices.Contains(path.vertices[pathPtr])) {
                matchFound = true;
            } else {
                pathPtr++;
            }           
        }

        if (matchFound) {
            path.TrimPath(path.vertices.Count - (pathPtr + 1));
        } 
        
        return path;
    }

    /// <summary>
    /// Check if the character is able to move and call the movement coroutine, overriding any current path
    /// </summary>
    /// <param name="path">The path to move along</param>
    public void MoveCharacter (Path path) {
        if (!disableMovement) {
            //If the characters moving stop them
            if (currentlyMoving) {
                StopCoroutine(moveCharacterCoroutine);
            }

            //Start the character moving on their new path
            moveCharacterCoroutine = MoveCharacterCoroutine(path);
            StartCoroutine(moveCharacterCoroutine);
        }
    }

    /// <summary>
    /// Coroutine for moving the character along the path
    /// </summary>
    /// <param name="path">The path to move along</param>
    /// <param name="delay">The delay in second be starting the movement</param>
    /// <returns></returns>
    private IEnumerator MoveCharacterCoroutine (Path path) {
        //If the character is casting an ability don't let them move
        if (AbilityManager.instance.abilityRunning) {
            yield break;
        } else {
            currentlyMoving = true;
        }

        //Unblock the starting vertex
        graphObstacle.UnblockCurrentVertex();

        //Iterate over each vertex in the path
        int pathPtr = 1;
        while (pathPtr < path.vertices.Count) {
            //Get the next vertex's world position 
            Vector3 nextVertexWorldPosition = path.vertices[pathPtr].worldPosition;
            if (Vector3.Distance(transform.position, nextVertexWorldPosition) > 0.1f) {
                LookAtVector(nextVertexWorldPosition);

                //Movement consists of two stages
                //Stage One - Horizontal movement                
                transform.position = Vector3.MoveTowards(transform.position, nextVertexWorldPosition, 20 * Time.deltaTime);

                //Stage Two - Vertical movement
                if (Mathf.Abs(nextVertexWorldPosition.y - path.vertices[pathPtr - 1].worldPosition.y) > 0.01f) {

                } else {
                    float distanceToLastVertex = Vector3.Distance(transform.position, path.vertices[pathPtr - 1].worldPosition);
                    float distanceToNextVertex = Vector3.Distance(transform.position, nextVertexWorldPosition);
                    transform.position = new Vector3(transform.position.x, (Mathf.PingPong(distanceToLastVertex / (distanceToLastVertex + distanceToNextVertex), 0.5f) * hopingHeight) + floorHeight, transform.position.z);
                }
            } else {
                //set the character current vertex to the last one it stepped on
                graphObstacle.SetCurrentVertex(path.vertices[pathPtr]);                
                pathPtr++;       
                if (pathPtr < path.vertices.Count) {
                    floorHeight = path.vertices[pathPtr].worldPosition.y;       
                } 
            }
            
            yield return null;
        }

        //Clear flags and re-active buttons
        currentlyMoving = false;
        feetMovedThisTurn += path.length;
        graphObstacle.BlockCurrentVertex();

        PlayerManager.instance.ActionRunning = false;

    }

    /// <summary>
    /// Rotates the object around the Y axis to look at the target vector
    /// </summary>
    /// <param name="lookAtTarget">The position in world space to look at</param>
    public void LookAtVector (Vector3 lookAtTarget) {
        Vector3 newForward = lookAtTarget - transform.position;
        transform.forward = new Vector3(newForward.x, 0, newForward.z).normalized;
    }

}