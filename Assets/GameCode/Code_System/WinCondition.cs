using System.Collections.Generic;
using UnityEngine;

public abstract class WinCondition
{
    public abstract GameMode GameMode { get; }
    public abstract bool IsRoundOver(int totalPlayers, float elapsedTime, float timeLimit);
    public abstract PlayerRoundResult[] GenerateResults(
        List<int> finishOrder, List<int> eliminatedOrder,
        int totalPlayers, string[] playerNames);

    protected static PlayerRoundResult CreateResult(int actorNumber, string nickName,
        int rank, bool finished, bool eliminated, float finishTime)
    {
        return new PlayerRoundResult
        {
            actorNumber = actorNumber,
            nickName = nickName,
            rank = rank,
            score = PlayerRoundResult.CalculateRankScore(rank, totalPlayers: 12),
            finished = finished,
            eliminated = eliminated,
            finishTime = finishTime
        };
    }
}

public class RaceWinCondition : WinCondition
{
    public override GameMode GameMode => GameMode.Race;

    public override bool IsRoundOver(int totalPlayers, float elapsedTime, float timeLimit)
    {
        // All finished or time up
        return elapsedTime >= timeLimit;
    }

    public override PlayerRoundResult[] GenerateResults(
        List<int> finishOrder, List<int> eliminatedOrder,
        int totalPlayers, string[] playerNames)
    {
        var results = new List<PlayerRoundResult>();
        int rank = 1;

        // Finished players get ranked by finish order
        foreach (int actorNr in finishOrder)
        {
            string name = GetPlayerName(actorNr, playerNames);
            results.Add(CreateResult(actorNr, name, rank, true, false, rank * 10f));
            rank++;
        }

        // Unfinished players (didn't cross finish, not eliminated)
        // ranked after all finishers
        int remaining = totalPlayers - finishOrder.Count;
        for (int i = 1; i <= remaining; i++)
        {
            int dummyActor = i;
            results.Add(CreateResult(dummyActor, $"Player_{dummyActor}", rank, false, false, 999f));
            rank++;
        }

        return results.ToArray();
    }

    private string GetPlayerName(int actorNumber, string[] playerNames)
    {
        if (actorNumber >= 0 && actorNumber < playerNames.Length &&
            !string.IsNullOrWhiteSpace(playerNames[actorNumber]))
            return playerNames[actorNumber];
        return $"Player_{actorNumber}";
    }
}

public class SurvivalWinCondition : WinCondition
{
    public override GameMode GameMode => GameMode.Survival;

    public override bool IsRoundOver(int totalPlayers, float elapsedTime, float timeLimit)
    {
        if (elapsedTime >= timeLimit)
            return true;

        // Round ends when only 1 remains (or 0 if all eliminated)
        return totalPlayers - EliminatedCount <= 1;
    }

    private int EliminatedCount;

    public override PlayerRoundResult[] GenerateResults(
        List<int> finishOrder, List<int> eliminatedOrder,
        int totalPlayers, string[] playerNames)
    {
        EliminatedCount = eliminatedOrder.Count;
        var results = new List<PlayerRoundResult>();

        // Survivors (not eliminated) — ranked first by elimination time (last eliminated = best)
        // Actually for survivors, they're all equal first until time runs out
        List<int> survivors = GetSurvivors(finishOrder, eliminatedOrder, totalPlayers);
        int rank = 1;

        foreach (int actorNr in survivors)
        {
            string name = GetPlayerName(actorNr, playerNames);
            results.Add(CreateResult(actorNr, name, rank, true, false, 0f));
            rank++;
        }

        // Eliminated players — ranked by elimination order (last eliminated = higher rank)
        for (int i = eliminatedOrder.Count - 1; i >= 0; i--)
        {
            int actorNr = eliminatedOrder[i];
            string name = GetPlayerName(actorNr, playerNames);
            float elimTime = (eliminatedOrder.Count - i) * 10f;
            results.Add(CreateResult(actorNr, name, rank, false, true, elimTime));
            rank++;
        }

        return results.ToArray();
    }

    private List<int> GetSurvivors(List<int> finishOrder, List<int> eliminatedOrder,
        int totalPlayers)
    {
        var survivors = new List<int>();
        var eliminated = new HashSet<int>(eliminatedOrder);

        // Use actor numbers 1..totalPlayers as reference
        for (int i = 1; i <= totalPlayers; i++)
        {
            if (!eliminated.Contains(i))
                survivors.Add(i);
        }

        return survivors;
    }

    private string GetPlayerName(int actorNumber, string[] playerNames)
    {
        if (actorNumber >= 0 && actorNumber < playerNames.Length &&
            !string.IsNullOrWhiteSpace(playerNames[actorNumber]))
            return playerNames[actorNumber];
        return $"Player_{actorNumber}";
    }
}
