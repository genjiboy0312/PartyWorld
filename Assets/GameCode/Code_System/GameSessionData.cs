using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    Race,
    Survival,
    Score,
    Team
}

[Serializable]
public class RoundConfig
{
    public int roundNumber;
    public GameMode gameMode = GameMode.Race;
    public string mapSceneName;
    public float timeLimitSeconds = 180f;
    public bool isFinalRound;
    public int playersToEliminate;
    public int playersToAdvance;
}

[Serializable]
public class PlayerRoundResult
{
    public int actorNumber;
    public string nickName;
    public int rank = -1;
    public int score;
    public bool eliminated;
    public bool finished;
    public float finishTime;

    public static int CalculateRankScore(int rank, int totalPlayers)
    {
        if (rank <= 0 || rank > totalPlayers)
            return 0;

        // Fall Guys style: 1st=10, 2nd=8, 3rd=6, 4th=4, 5th=2, rest=1
        int[] scores = { 10, 8, 6, 4, 2 };
        if (rank <= scores.Length)
            return scores[rank - 1];

        return 1;
    }
}
