using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class DifficultyPoolChecker : EditorWindow
{
    [MenuItem("NimbleFox/Check Difficulty Pools")]
    public static void ShowWindow()
    {
        GetWindow<DifficultyPoolChecker>("Difficulty Pool Checker");
    }

    private string output = "";

    void OnGUI()
    {
        if (GUILayout.Button("Run Analysis"))
        {
            RunAnalysis();
        }

        EditorGUILayout.Space();
        
        Vector2 scroll = Vector2.zero;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void RunAnalysis()
    {
        string dbPath = "Assets/Nimble Fox/Generated Content/word_database.json";
        if (!File.Exists(dbPath)) {
            output = "Could not find word_database.json";
            return;
        }

        WordDatabaseWrapper db = JsonUtility.FromJson<WordDatabaseWrapper>(File.ReadAllText(dbPath));
        string configPath = "Assets/StreamingAssets/config.json";
        
        SimpleWordChooser.GameConfig config = new SimpleWordChooser.GameConfig {
            startPrimaryScore = 0.8f,
            primaryScoreStep = 0.05f,
            startTargetDelta = 0.6f,
            targetDeltaStep = 0.1f
        };

        if (File.Exists(configPath)) {
            config = JsonUtility.FromJson<SimpleWordChooser.GameConfig>(File.ReadAllText(configPath));
        }

        output = "Analyzing word pools...\n\n";

        string[] cats = { "Dawn", "Nocturne", "Hearth", "Frost", "Canopy" };

        int totalWarnings = 0;

        for (int level = 0; level < 5; level++)
        {
            float minPrimaryScore = config.startPrimaryScore - (level * config.primaryScoreStep);
            float targetDelta = config.startTargetDelta - (level * config.targetDeltaStep);

            output += $"=== LEVEL {level + 1} (MinPrimary: {minPrimaryScore:F2}, TargetDelta: {targetDelta:F2}) ===\n";

            for (int i = 0; i < cats.Length; i++)
            {
                for (int j = i + 1; j < cats.Length; j++)
                {
                    string targetCat = cats[i];
                    string oppositeCat = cats[j];

                    int poolA = GetPoolSize(db, targetCat, oppositeCat, minPrimaryScore);
                    int poolB = GetPoolSize(db, oppositeCat, targetCat, minPrimaryScore);

                    if (poolA < 5) { output += $"[WARNING] "; totalWarnings++; }
                    output += $"{targetCat} vs {oppositeCat}: {targetCat} has {poolA} valid words.\n";

                    if (poolB < 5) { output += $"[WARNING] "; totalWarnings++; }
                    output += $"{oppositeCat} vs {targetCat}: {oppositeCat} has {poolB} valid words.\n";
                }
            }
            output += "\n";
        }
        
        output += $"Analysis Complete. {totalWarnings} warnings (pool size < 5).";
    }

    int GetPoolSize(WordDatabaseWrapper db, string targetCat, string oppositeCat, float minPrimaryScore)
    {
        int count = 0;
        string lowerCatA = targetCat.ToLower();
        string lowerCatB = oppositeCat.ToLower();
        string rootA = lowerCatA.Substring(0, Mathf.Min(5, lowerCatA.Length));
        string rootB = lowerCatB.Substring(0, Mathf.Min(5, lowerCatB.Length));

        foreach (var w in db.words)
        {
            string lowerWord = w.word.ToLower();
            if (lowerWord.Contains(rootA) || lowerWord.Contains(rootB)) continue;

            float sTarget = w.GetScore(targetCat);
            float sOpposite = w.GetScore(oppositeCat);
            
            if (sTarget <= sOpposite) continue;

            float maxScore = Mathf.Max(w.dawn, w.nocturne, w.hearth, w.frost, w.canopy);
            
            if (Mathf.Approximately(maxScore, sTarget) && (sTarget >= minPrimaryScore))
            {
                count++;
            }
        }
        return count;
    }
}
