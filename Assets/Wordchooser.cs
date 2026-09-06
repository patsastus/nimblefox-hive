using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SimpleWordChooser : MonoBehaviour
{
    [System.Serializable]
    public struct WordRound
    {
        public string word;
        public string leftCategory;
        public string rightCategory;
        public bool isLeftCorrect;
    }

    [Header("UI / 3D Text References")]
    public TextMeshPro centerWordDisplay;
    public TextMeshPro leftCategoryDisplay;
    public TextMeshPro rightCategoryDisplay;
    [Tooltip("Optional dedicated text object for the endgame screen")]
    public TextMeshPro splashScreenDisplay;

    [Header("Endgame Screens & Music")]
    public GameObject victoryScreenPanel;
    public GameObject defeatScreenPanel;
    public AudioClip victoryMusic;
    public AudioClip defeatMusic;

    [Header("Positions")]
    public Transform leftTarget;
    public Transform rightTarget;
    public float flySpeed = 15f;

    [Header("V2 Game Rules")]
    [Tooltip("How many correct answers does it take to make the sun fully rise?")]
    public int requiredScoreToWin = 12;
    [Tooltip("Requires word_database.json")]
    public TextAsset fullDatabaseJson;
    
    [Header("V2 Difficulty Settings")]
    public float difficultyIncreasePerWin = 0.15f;
    public float difficultyDecreasePerLoss = 0.20f;
    public int wrongAnswersForShift = 2;

    [SerializeField] private SunriseLightingController sunriseLightingController;

    [Header("Audio")]
    
    public AudioClip swipeSound;
    public AudioClip successSound;
    public AudioClip failureSound;
    public AudioClip categoryShiftSound;

    private int score = 0;
    private int roundsPlayed = 0;
    private float currentDifficulty = 0f;
    private int consecutiveWrongAnswers = 0;
    private int maxDifficultyReached = 0; // scale of 0 to 10 for UI purposes

    private string currentCatA;
    private string currentCatB;
    private HashSet<string> usedWords = new HashSet<string>();
    private WordRound currentRound;
    private Vector3 centerOrigin;
    private bool isResolving = false;
    private bool gameEnded = false;

    [System.Serializable]
    public class GameConfig
    {
        public int requiredScoreToWin;
        public float difficultyIncreasePerWin;
        public float difficultyDecreasePerLoss;
        public int wrongAnswersForShift;
        public float flySpeed;
    }

    IEnumerator Start()
    {
        string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(configPath);
        yield return www.SendWebRequest();
        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success) {
            ApplyConfig(www.downloadHandler.text);
        }
#else
        if (System.IO.File.Exists(configPath)) {
            ApplyConfig(System.IO.File.ReadAllText(configPath));
        }
        yield return null;
#endif

        if (fullDatabaseJson != null)
        {
            DynamicRoundGenerator.Initialize(fullDatabaseJson);
        }
        else
        {
            Debug.LogError("No Database JSON assigned! Game will not work in V2.");
        }

        if (sunriseLightingController == null) TryGetComponent(out sunriseLightingController);
        if (sunriseLightingController != null) sunriseLightingController.Initialize(requiredScoreToWin, score);

        if (centerWordDisplay != null)
        {
            centerOrigin = centerWordDisplay.transform.position;
            centerWordDisplay.textWrappingMode = TextWrappingModes.NoWrap;
        }
        
        if (leftCategoryDisplay != null) leftCategoryDisplay.textWrappingMode = TextWrappingModes.NoWrap;
        if (rightCategoryDisplay != null) rightCategoryDisplay.textWrappingMode = TextWrappingModes.NoWrap;
        if (splashScreenDisplay != null) splashScreenDisplay.gameObject.SetActive(false);

        ShiftCategories(false);
        LoadNextRound();
    }

    private void ApplyConfig(string json)
    {
        try {
            GameConfig config = JsonUtility.FromJson<GameConfig>(json);
            requiredScoreToWin = config.requiredScoreToWin;
            difficultyIncreasePerWin = config.difficultyIncreasePerWin;
            difficultyDecreasePerLoss = config.difficultyDecreasePerLoss;
            wrongAnswersForShift = config.wrongAnswersForShift;
            flySpeed = config.flySpeed;
            Debug.Log("Loaded external config.json successfully!");
        } catch (System.Exception e) {
            Debug.LogWarning("Failed to parse config.json: " + e.Message);
        }
    }

    void ShiftCategories(bool playSound = true)
    {
        usedWords.Clear();
        List<string> cats = new List<string>(DynamicRoundGenerator.AllCategories);
        currentCatA = cats[Random.Range(0, cats.Count)];
        cats.Remove(currentCatA);
        currentCatB = cats[Random.Range(0, cats.Count)];

        if (playSound && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(categoryShiftSound);

        if (splashScreenDisplay != null)
        {
            // Optional: flash a warning. For now we just seamlessly change them.
            Debug.Log($"[V2] CATEGORY SHIFT! Now playing {currentCatA} vs {currentCatB}");
        }
    }

    void Update()
    {
        if (isResolving || gameEnded) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(ResolveChoice(true));
        }
        else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(ResolveChoice(false));
        }
    }

    void LoadNextRound()
    {
        currentRound = DynamicRoundGenerator.GetNextWord(currentCatA, currentCatB, currentDifficulty, usedWords);

        centerWordDisplay.text = currentRound.word;
        centerWordDisplay.transform.position = centerOrigin;
        centerWordDisplay.color = Color.white;

        if (leftCategoryDisplay) leftCategoryDisplay.text = currentCatA;
        if (rightCategoryDisplay) rightCategoryDisplay.text = currentCatB;
    }

    void EndGame(bool won)
    {
        gameEnded = true;

        if (leftCategoryDisplay) leftCategoryDisplay.gameObject.SetActive(false);
        if (rightCategoryDisplay) rightCategoryDisplay.gameObject.SetActive(false);
        if (centerWordDisplay) centerWordDisplay.gameObject.SetActive(false);
        
        if (won)
        {
            if (victoryScreenPanel != null) victoryScreenPanel.SetActive(true);
            if (AudioManager.Instance != null && victoryMusic != null) AudioManager.Instance.SwitchBGM(victoryMusic);
        }
        else
        {
            if (defeatScreenPanel != null) defeatScreenPanel.SetActive(true);
            if (AudioManager.Instance != null && defeatMusic != null) AudioManager.Instance.SwitchBGM(defeatMusic);
        }

        if (splashScreenDisplay != null)
        {
            splashScreenDisplay.gameObject.SetActive(true);
            if (won)
            {
                splashScreenDisplay.text = $"FIRST LIGHT REACHED!\n\nHighest Difficulty: {maxDifficultyReached}/10\nRounds Survived: {roundsPlayed}";
                splashScreenDisplay.color = Color.yellow;
            }
            else
            {
                splashScreenDisplay.text = $"CONSUMED BY DARKNESS...\n\nHighest Difficulty: {maxDifficultyReached}/10\nRounds Survived: {roundsPlayed}";
                splashScreenDisplay.color = Color.red;
            }
        }
        else
        {
            centerWordDisplay.gameObject.SetActive(true);
            centerWordDisplay.transform.position = centerOrigin + Vector3.up * 1.5f;
            centerWordDisplay.text = won ? "FIRST LIGHT REACHED!" : "GAME OVER";
            centerWordDisplay.color = won ? Color.yellow : Color.red;
        }
    }

    IEnumerator ResolveChoice(bool choseLeft)
    {
        isResolving = true;
        Transform target = choseLeft ? leftTarget : rightTarget;
        Vector3 targetPos = target != null ? target.position : (centerOrigin + (choseLeft ? Vector3.left : Vector3.right) * 6f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(swipeSound);

        while (Vector3.Distance(centerWordDisplay.transform.position, targetPos) > 0.1f)
        {
            centerWordDisplay.transform.position = Vector3.MoveTowards(
                centerWordDisplay.transform.position, targetPos, flySpeed * Time.deltaTime);
            yield return null;
        }

        bool isCorrect = (choseLeft == currentRound.isLeftCorrect);
        
        StartCoroutine(PulseTarget(target, isCorrect));
        Transform textTarget = choseLeft ? leftCategoryDisplay.transform : rightCategoryDisplay.transform;
        if (textTarget != null && textTarget != target) StartCoroutine(PulseTarget(textTarget, isCorrect));

        roundsPlayed++;

        if (isCorrect)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(successSound);
            score++;
            currentDifficulty = Mathf.Clamp01(currentDifficulty + difficultyIncreasePerWin);
            consecutiveWrongAnswers = 0;
            centerWordDisplay.color = Color.yellow; 
            
            int diffScale = Mathf.RoundToInt(currentDifficulty * 10f);
            if (diffScale > maxDifficultyReached) maxDifficultyReached = diffScale;
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(failureSound);
            score--;
            currentDifficulty = Mathf.Clamp01(currentDifficulty - difficultyDecreasePerLoss);
            consecutiveWrongAnswers++;
            centerWordDisplay.color = Color.gray;
        }

        if (sunriseLightingController != null)
        {
            sunriseLightingController.UpdateSuccessCount(score);
        }

        yield return new WaitForSeconds(0.4f);

        if (score >= requiredScoreToWin)
        {
            EndGame(true);
        }
        else if (score < 0)
        {
            EndGame(false);
        }
        else
        {
            if (consecutiveWrongAnswers >= wrongAnswersForShift)
            {
                consecutiveWrongAnswers = 0;
                ShiftCategories();
            }
            LoadNextRound();
            isResolving = false;
        }
    }

    IEnumerator PulseTarget(Transform target, bool isCorrect)
    {
        if (target == null) yield break;

        Color pulseColor = isCorrect ? Color.green : Color.red;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        UnityEngine.UI.Graphic[] graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>();

        Vector3 originalScale = target.localScale;
        Vector3 punchScale = originalScale * 1.4f;
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scaleT = Mathf.Sin(t * Mathf.PI);
            target.localScale = Vector3.Lerp(originalScale, punchScale, scaleT);
            Color frameColor = Color.Lerp(pulseColor, Color.white, t);

            foreach (var r in renderers) { if (r.material.HasProperty("_Color") || r.material.HasProperty("_BaseColor")) r.material.color = frameColor; }
            foreach (var g in graphics) { g.color = frameColor; }
            foreach (var txt in texts) { txt.color = frameColor; }
            yield return null;
        }

        target.localScale = originalScale;
        foreach (var r in renderers) { if (r.material.HasProperty("_Color") || r.material.HasProperty("_BaseColor")) r.material.color = Color.white; }
        foreach (var g in graphics) { g.color = Color.white; }
        foreach (var txt in texts) { txt.color = Color.white; }
    }
}