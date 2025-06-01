using System;
using System.Collections.Generic;

[Serializable]
public class AnswerModel
{
    public string gameId;
    public string studentId;
    public string date;
    public int totalScore;
    public Answers answers;

    [Serializable]
    public class Answers
    {
        public FindComposition findcomposition;
        public WriteTheNumber writetheNumber;
        public IdentifyUnits identifyUnits;
    }

    [Serializable]
    public class FindComposition
    {
        public int number;
        public List<int> studentAnswer;
        public int attemptsUsed;
        public bool isCorrect;
        public int score;
    }

    [Serializable]
    public class WriteTheNumber
    {
        public int number;
        public string studentAnswer;
        public bool isCorrect;
        public int attemptsUsed;
        public int score;
    }

    [Serializable]
    public class IdentifyUnits
    {
        public int number;
        public UnitsAnswer studentAnswer;
        public bool isCorrect;
        public int attemptsUsed;
        public int score;
    }

    [Serializable]
    public class UnitsAnswer
    {
        public int units;
        public int tens;
        public int hundreds;
        public int thousands;
    }
}
