using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestBook : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static QuestBook instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}
    
    //-----VARIABLES-----

    HashSet<QuestComponent> quests = new HashSet<QuestComponent>();

    //-----METHODS-----

    public void Initialise () {
        
    }



}
