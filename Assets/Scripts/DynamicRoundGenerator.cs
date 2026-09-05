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
    private static WordDatabaseWrapper db;
    
    // Cache categories to avoid spelling typos
    public static string[] AllCategories = { "Dawn", "Nocturne", "Hearth", "Frost", "Canopy" };

    public static void Initialize(TextAsset dbJson)
    {
        if (db == null)
        {
            db = JsonUtility.FromJson<WordDatabaseWrapper>(dbJson.text);
            Debug.Log($"[DynamicRoundGenerator] Loaded {db.words.Length} words into memory.");
        }
    }

    public static SimpleWordChooser.WordRound GetNextWord(string catA, string catB, float difficulty, HashSet<string> usedWords)
    {
        if (db == null)
        {
            Debug.LogError("DynamicRoundGenerator not initialized!");
            return new SimpleWordChooser.WordRound();
        }

        // Randomly pick which side is correct for this round
        bool isLeftCorrect = Random.value > 0.5f;
        string targetCat = isLeftCorrect ? catA : catB;
        string oppositeCat = isLeftCorrect ? catB : catA;

        string lowerCatA = catA.ToLower();
        string lowerCatB = catB.ToLower();
        string rootA = lowerCatA.Substring(0, Mathf.Min(5, lowerCatA.Length));
        string rootB = lowerCatB.Substring(0, Mathf.Min(5, lowerCatB.Length));

        List<WordDbEntry> validPool = new List<WordDbEntry>();

        // 1. Filter the entire database for this specific category pair
        foreach (var w in db.words)
        {
            string lowerWord = w.word.ToLower();

            // Strict exclusion rules
            if (lowerWord.Contains(rootA) || lowerWord.Contains(rootB)) continue;
            if (IsTooSimilar(lowerWord, usedWords)) continue;

            float sTarget = w.GetScore(targetCat);
            float sOpposite = w.GetScore(oppositeCat);
            
            // It must actually belong to the target category (Target > Opposite)
            if (sTarget <= sOpposite) continue;

            float maxScore = Mathf.Max(w.dawn, w.nocturne, w.hearth, w.frost, w.canopy);
            
            // The word must be a primary match for the target category, and not a total junk word
            if (Mathf.Approximately(maxScore, sTarget) && (sTarget + sOpposite > 0.7f))
            {
                validPool.Add(w);
            }
        }

        if (validPool.Count == 0)
        {
            Debug.LogWarning("Ran out of valid words for this category pair! Returning fallback.");
            return new SimpleWordChooser.WordRound { word = "Error", leftCategory = catA, rightCategory = catB, isLeftCorrect = isLeftCorrect };
        }

        // 2. Find the max delta to establish the "Easiest" possible word
        float maxDelta = validPool.Max(w => w.GetScore(targetCat) - w.GetScore(oppositeCat));

        // 3. Map the difficulty (0 to 1) to a target delta. 
        // difficulty 0.0 = maxDelta (Easy)
        // difficulty 1.0 = 0.02 (Extreme)
        float targetDelta = Mathf.Lerp(maxDelta, 0.02f, difficulty);

        // 4. Find the closest matches to the target delta
        var closest = validPool.OrderBy(w => Mathf.Abs((w.GetScore(targetCat) - w.GetScore(oppositeCat)) - targetDelta))
                               .Take(15)
                               .ToList();

        // 5. Randomly pick from the top 5 closest to keep it thematic but varied
        int pickIndex = Random.Range(0, Mathf.Min(5, closest.Count));
        WordDbEntry chosen = closest[pickIndex];

        usedWords.Add(chosen.word.ToLower());

        return new SimpleWordChooser.WordRound
        {
            word = chosen.word,
            leftCategory = catA,
            rightCategory = catB,
            isLeftCorrect = isLeftCorrect
        };
    }

    private static bool IsTooSimilar(string newWord, HashSet<string> usedWords)
    {
        foreach (string used in usedWords)
        {
            if (newWord.Contains(used) || used.Contains(newWord)) return true;
            if (newWord.Length >= 4 && used.Length >= 4 && newWord.Substring(0, 4) == used.Substring(0, 4)) return true;
        }
        return false;
    }
}
