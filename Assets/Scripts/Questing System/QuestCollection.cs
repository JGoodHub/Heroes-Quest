using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestCollection : MonoBehaviour {
	
	//-----SINGLETON-----
	
	public static QuestCollection instance = null;
	
	void Awake() {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    public static HashSet<QuestComponent> quests;

	//-----METHODS-----
	
    /// <summary>
    /// Deceleration for the demo quest
    /// </summary>
	public void Initialise () {
        quests = new HashSet<QuestComponent>();

        #region Demo Quest Declaration
        //Stage 1
        QuestComponent meetTheTownsFolk = new QuestComponent(
            "Meet the Towns Folk",
            "Farmer Joe has another job for you, probably lost his sheep again.",
            0, QuestComponent.QuestType.INTERACT);
        quests.Add(meetTheTownsFolk);

        //Stage 2
        QuestComponent findJoesLostTreasure = new QuestComponent(
            "Find the Lost Treasure",
            "Search for Joes lost treasure, but be cautious, there be demons in these parts",
            100, QuestComponent.QuestType.ENCOUNTER);
        findJoesLostTreasure.interactionText = "Good to see you again, wish it was under better circumstances but those demon bastards stole all my gold. I need you to go get it back.";
        quests.Add(findJoesLostTreasure);

        //Stage 3
        QuestComponent grabTheCoinsAndGo = new QuestComponent(
            "Grab the Coins and Go",
            "Lets grab the treasure and get out of here before more show up",
            200, QuestComponent.QuestType.INTERACT);
        quests.Add(grabTheCoinsAndGo);

        //Stage 4
        QuestComponent warnFarmerJoe = new QuestComponent(
            "Warn Farmer Joe",
            "We should head back to town and warn Joe about the demons",
            300, QuestComponent.QuestType.HAND_IN);
        warnFarmerJoe.interactionText = "Well thanks for finding me gold anyway. Don't worry I'll find a group of adventurers dumb enough to take on demons for me. So ... I have another job for you.";
        quests.Add(warnFarmerJoe);

        //Chain linking
        meetTheTownsFolk.followUpQuests.Add(findJoesLostTreasure);
        findJoesLostTreasure.followUpQuests.Add(grabTheCoinsAndGo);
        grabTheCoinsAndGo.followUpQuests.Add(warnFarmerJoe);
        #endregion
    }

}
