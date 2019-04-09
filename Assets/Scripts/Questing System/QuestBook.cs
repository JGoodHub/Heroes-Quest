using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestBook : MonoBehaviour {
	
	//-----SINGLETON-----
	
	public static QuestBook instance = null;
	
	void Awake() {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    private static HashSet<QuestComponent> quests;
    public static HashSet<QuestComponent> Quests { get => quests; }

	//-----METHODS-----
	
    /// <summary>
    /// 
    /// </summary>
	public void Initialise () {
        quests = new HashSet<QuestComponent>();

        #region Demo Quest Declaration
        //Stage 1
        QuestComponent meetTheTownsFolk = new QuestComponent(
            "Meet the Towns Folk",
            "I hear farmer Joe has another job for you, probably lost his damn sheep again",
            0, QuestComponent.QuestType.INTERACT);
        quests.Add(meetTheTownsFolk);

        //Stage 2
        QuestComponent findJoesLostTreasure = new QuestComponent(
            "Find Joes Lost Treasure",
            "Go search for Joes long lost treasure, who knows he might even give you a coin or two for it. Watch out though, strange creatures have been spotted in the area so keep your wits about you",
            100, QuestComponent.QuestType.ENCOUNTER);
        findJoesLostTreasure.interactionText = "Good to see you again, wish it was under better circumstances but those demon bastards stole all my gold. I need you to go get it back.";
        quests.Add(findJoesLostTreasure);

        //Stage 3
        QuestComponent grabTheCoinsAndGo = new QuestComponent(
            "Grab the Coins and Go",
            "Well Joe didn't tell use he was in deep with the Xanathar guild, I'm sure he won't notice if a few coins are missing",
            200, QuestComponent.QuestType.INTERACT);
        quests.Add(grabTheCoinsAndGo);

        //Stage 4
        QuestComponent warnFarmerJoe = new QuestComponent(
            "Warn Farmer Joe",
            "Well now we know why the Xanathar guild stole his gold, we should head back to town and warn Joe",
            300, QuestComponent.QuestType.HAND_IN);
        quests.Add(warnFarmerJoe);

        //Chain linking
        meetTheTownsFolk.followUpQuests.Add(findJoesLostTreasure);
        findJoesLostTreasure.followUpQuests.Add(grabTheCoinsAndGo);
        grabTheCoinsAndGo.followUpQuests.Add(warnFarmerJoe);
        #endregion
    }

    //-----GIZMOS-----
    //public bool drawGizmos;
    void OnDrawGizmos() {
	
	}
	
}
