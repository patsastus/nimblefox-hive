using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WordDbEntry
{
    public string word;
    public float dawn;
    public float nocturne;
    public float hearth;
    public float frost;
    public float canopy;

    public float GetScore(string category)
    {
        switch (category.ToLower())
        {
            case "dawn": return dawn;
            case "nocturne": return nocturne;
            case "hearth": return hearth;
            case "frost": return frost;
            case "canopy": return canopy;
            default: return 0f;
        }
    }
}

[System.Serializable]
public class WordDatabaseWrapper
{
    public WordDbEntry[] words;
}

public static class DynamicRoundGenerator
{
    private static string[] AllCategories = { "Dawn", "Nocturne", "Hearth", "Frost", "Canopy" };

    public static SimpleWordChooser.WordRound[] GenerateRounds(TextAsset dbJson, int totalRounds)
    {
        WordDatabaseWrapper db = JsonUtility.FromJson<WordDatabaseWrapper>(dbJson.text);
        
        // Pick two random categories
        List<string> cats = new List<string>(AllCategories);
        string catA = cats[Random.Range(0, cats.Count)];
        cats.Remove(catA);
        string catB = cats[Random.Range(0, cats.Count)];

        // Filter out junk words (must have A or B as highest score, and combined > 0.7)
        List<WordDbEntry> validWords = new List<WordDbEntry>();
        string lowerCatA = catA.ToLower();
        string lowerCatB = catB.ToLower();
        string rootA = lowerCatA.Substring(0, Mathf.Min(5, lowerCatA.Length));
        string rootB = lowerCatB.Substring(0, Mathf.Min(5, lowerCatB.Length));

        foreach (var w in db.words)
        {
            string lowerWord = w.word.ToLower();

            // Never allow the category name, or words sharing its root (e.g. "nocturnal" for "Nocturne")
            if (lowerWord.Contains(rootA) || lowerWord.Contains(rootB)) continue;

            float sA = w.GetScore(catA);
            float sB = w.GetScore(catB);
            
            float maxScore = Mathf.Max(w.dawn, w.nocturne, w.hearth, w.frost, w.canopy);
            if ((Mathf.Approximately(maxScore, sA) || Mathf.Approximately(maxScore, sB)) && (sA + sB > 0.7f))
            {
                validWords.Add(w);
            }
        }

        // Separate by correct side and calculate delta (A - B)
        var aWords = validWords.Where(w => w.GetScore(catA) > w.GetScore(catB))
                               .OrderByDescending(w => w.GetScore(catA) - w.GetScore(catB)).ToList();
                               
        var bWords = validWords.Where(w => w.GetScore(catB) > w.GetScore(catA))
                               .OrderByDescending(w => w.GetScore(catB) - w.GetScore(catA)).ToList(); // Delta for B is (B - A)

        int numPerCat = Mathf.CeilToInt(totalRounds / 2f);
        
        List<SimpleWordChooser.WordRound> finalRounds = new List<SimpleWordChooser.WordRound>();
        HashSet<string> usedWords = new HashSet<string>();

        List<SimpleWordChooser.WordRound> aSequence = PickSmoothSequence(aWords, catA, catB, true, numPerCat, usedWords);
        List<SimpleWordChooser.WordRound> bSequence = PickSmoothSequence(bWords, catA, catB, false, numPerCat, usedWords);

        // Interleave them (Easy A, Easy B, Med A, Med B...)
        for (int i = 0; i < numPerCat; i++)
        {
            if (i < aSequence.Count) finalRounds.Add(aSequence[i]);
            if (i < bSequence.Count) finalRounds.Add(bSequence[i]);
        }

        Debug.Log($"[DynamicRoundGenerator] Generated {finalRounds.Count} rounds for {catA} vs {catB}");
        return finalRounds.ToArray();
    }

    private static List<SimpleWordChooser.WordRound> PickSmoothSequence(
        List<WordDbEntry> pool, string catA, string catB, bool isA, int count, HashSet<string> usedWords)
    {
        List<SimpleWordChooser.WordRound> sequence = new List<SimpleWordChooser.WordRound>();
        if (pool.Count == 0) return sequence;

        string targetCat = isA ? catA : catB;
        string oppositeCat = isA ? catB : catA;

        float maxDelta = pool[0].GetScore(targetCat) - pool[0].GetScore(oppositeCat);

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? 1f - ((float)i / (count - 1)) : 1f;
            float targetDelta = Mathf.Max(0.02f, maxDelta * t);

            // Find closest words to the target delta
            var available = pool.Where(w => !IsTooSimilar(w.word, usedWords)).ToList();
            if (available.Count == 0) break;

            var closest = available.OrderBy(w => Mathf.Abs((w.GetScore(targetCat) - w.GetScore(oppositeCat)) - targetDelta))
                                   .Take(15) // take top 15 closest
                                   .ToList();

            // Add randomness! Pick randomly from the top 15 closest delta words, 
            // but weighted slightly towards higher primary scores.
            // For simplicity, just pick a random one from the top 5 to keep it highly thematic but varied.
            int pickIndex = Random.Range(0, Mathf.Min(5, closest.Count));
            WordDbEntry chosen = closest[pickIndex];

            usedWords.Add(chosen.word.ToLower());

            sequence.Add(new SimpleWordChooser.WordRound
            {
                word = chosen.word,
                leftCategory = catA,
                rightCategory = catB,
                isLeftCorrect = isA
            });
        }

        return sequence;
    }

    private static bool IsTooSimilar(string newWord, HashSet<string> usedWords)
    {
        newWord = newWord.ToLower();
        foreach (string used in usedWords)
        {
            // Exact match or direct substring (sun vs sunshine)
            if (newWord.Contains(used) || used.Contains(newWord)) return true;

            // Shared 4-letter prefix (dark vs darkness)
            if (newWord.Length >= 4 && used.Length >= 4)
            {
                if (newWord.Substring(0, 4) == used.Substring(0, 4)) return true;
            }
        }
        return false;
    }
}
