using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IQuestable {

    //-----PROPERTIES-----

    QuestComponent LinkedQuestComponent { get; set; }

    //-----METHODS-----

    void CompleteQuest ();

}
