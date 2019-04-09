using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GraphObstacle))]
public class MovementController : MonoBehaviour {

    //-----VARIABLES-----

    //Variables for moving the character
    public float speedInFeet;
    private float yBounce = 6f;
    private float yFloor = 0f;
    private IEnumerator moveCharacterCoroutine;    

    //Is the character currently moving or able to move at all
    public bool lockedDown = false;
    private bool currentlyMoving;
    public bool CurrentlyMoving { get => currentlyMoving; }

    //Distance the character has moved this turn
    private float feetMovedThisTurn = 0;

    //Reference to the character controller
    private CharacterController characterController;
    private GraphObstacle graphObstacle;
    public GraphObstacle GraphObstacle { get => graphObstacle; }

    //-----METHODS-----

    /// <summary>
    /// Setup Method
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
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool CanMoveDistance (float distance) {
        return distance <= speedInFeet - feetMovedThisTurn;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Path CheckForEncounterAndTrimPath (Path path) {
        HashSet<Vertex> encounterTriggerVertices = EnemyAIManager.instance.GetAllEncounterVertices();
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
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="delay"></param>
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
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator MoveCharacterCoroutine (Path path, float delay) {
        //If the character is casting an ability don't let them move
        if (AbilityManager.instance.abilityRunning) {
            yield break;
        } else {
            currentlyMoving = true;
        }

        //Disable the users ability to move again until the current move is finished
        UIManager.instance.DisableGameUI();

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
                if (Mathf.Abs(nextVertexWorldPosition.y - path.Vertices[pathPtr - 1].WorldPosition.y) > 0.01f) {

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
        feetMovedThisTurn += path.Length;
        graphObstacle.BlockCurrentVertex();

        UIManager.instance.EnableGameUI();        
        PlayerManager.instance.ActionRunning = false;

        //Check if the character is in enemy territory and trigger their turn if so
        if (EnemyAIManager.instance.GetAllEncounterVertices().Contains(graphObstacle.CurrentVertex)) {
            EnemyAIManager.instance.GetEncounterThatVertexIsPartOf(graphObstacle.CurrentVertex).encounterActive = true;            
            GameManager.instance.EndPlayersTurn();
        } else {
            ResetDistanceMoved();
            UIManager.instance.DisableEndTurnButton();
            Debug.Log("MOVED WITHOUT COMBAT");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lookAtTarget"></param>
    public void LookAtVector (Vector3 lookAtTarget) {
        Vector3 newForward = lookAtTarget - transform.position;
        transform.forward = new Vector3(newForward.x, 0, newForward.z).normalized;
    }

}