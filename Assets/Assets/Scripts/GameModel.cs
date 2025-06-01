using System;
using System.Collections.Generic;

[Serializable]
public class GameModel
{
    public string id;
    public string idTeacher;
    public LocalizedString title;
    public LocalizedString description;
    public string @class;
    public FindComposition findcomposition;
    public WriteTheNumber WritetheFollowingNumberinLetters;
    public IdentifyUnits IdentifthUnitsTensHundredsandThousands;

    [Serializable]
    public class LocalizedString
    {
        public string ar;
        public string fr;
        public string en;
    }

    [Serializable]
    public class FindComposition
    {
        public int time;
        public int attemptsAllowed;
        public int number;
        public List<int> solution;
    }

    [Serializable]
    public class WriteTheNumber
    {
        public int time;
        public int attemptsAllowed;
        public int number;
        public List<string> solution;
    }

    [Serializable]
    public class IdentifyUnits
    {
        public int time;
        public int attemptsAllowed;
        public int number;
        public UnitsSolution solution;
    }

    [Serializable]
    public class UnitsSolution
    {
        public int units;
        public int tens;
        public int hundreds;
        public int thousands;
    }
}
