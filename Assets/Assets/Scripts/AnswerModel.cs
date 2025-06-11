using System;
using System.Collections.Generic;

[Serializable]
public class AnswerModel
{
    public string gameId;
    public string studentId;
    public string date;
    public int totalScore;
    public string idTeacher;
    public bool isSent;
    public Answers answers;
    public Statistics statistics;

    [Serializable]
    public class Answers
    {
        public FindCompositionAnswers findcomposition;
        public WriteNumberAnswers WritetheFollowingNumberinLetters;
        public IdentifyUnitsAnswers IdentifthUnitsTensHundredsandThousands;
    }

    [Serializable]
    public class FindCompositionAnswers
    {
        public DifficultyAnswer easy;
        public DifficultyAnswer medium;
        public DifficultyAnswer hard;

        [Serializable]
        public class DifficultyAnswer
        {
            public List<int> studentAnswer;
            public int attemptsUsed;
            public bool isCorrect;
            public int score;
            public int time; // ADDED: Time field to match Firebase structure
        }
    }

    [Serializable]
    public class WriteNumberAnswers
    {
        public DifficultyAnswer easy;
        public DifficultyAnswer medium;
        public DifficultyAnswer hard;

        [Serializable]
        public class DifficultyAnswer
        {
            public List<string> studentAnswer;
            public bool isCorrect;
            public int attemptsUsed;
            public int score;
            public int time; // ADDED: Time field
        }
    }

    [Serializable]
    public class IdentifyUnitsAnswers
    {
        public DifficultyAnswer easy;
        public DifficultyAnswer medium;
        public DifficultyAnswer hard;

        [Serializable]
        public class DifficultyAnswer
        {
            public UnitsAnswer studentAnswer;
            public bool isCorrect;
            public int attemptsUsed;
            public int score;
            public int time; // ADDED: Time field
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

    [Serializable]
    public class Statistics
    {
        public int totalTimeSpent;
        public int totalAttemptsUsed;
        public int correctAnswersCount;
        public int incorrectAnswersCount;
    }
}
