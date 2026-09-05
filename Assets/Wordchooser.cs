using System.Collections;
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

    [System.Serializable]
    public class GameRoundsWrapper
    {
        public WordRound[] rounds;
    }

    // Data references moved below

    [Header("UI / 3D Text References")]
    public TextMeshPro centerWordDisplay;
    public TextMeshPro leftCategoryDisplay;
    public TextMeshPro rightCategoryDisplay;
    [Tooltip("Optional dedicated text object for the endgame screen")]
    public TextMeshPro splashScreenDisplay;

    [Header("Positions")]
    public Transform leftTarget;
    public Transform rightTarget;
    public float flySpeed = 15f;

    [Header("Rounds")]
    public WordRound[] rounds = new WordRound[]
    {
        new WordRound { word = "Dew", leftCategory = "Night", rightCategory = "Dawn", isLeftCorrect = false },
        new WordRound { word = "Shadow", leftCategory = "Night", rightCategory = "Dawn", isLeftCorrect = true },
        new WordRound { word = "Rooster", leftCategory = "Night", rightCategory = "Dawn", isLeftCorrect = false }
    };

    [Header("Game Rules")]
    [Tooltip("How many correct answers does it take to make the sun fully rise?")]
    public int requiredScoreToWin = 12;
    [Tooltip("If true, ignores roundsJsonFile and generates an endless unique game from fullDatabaseJson")]
    public bool useDynamicDatabase = false;

    [Header("Data")]
    public TextAsset roundsJsonFile;
    public TextAsset fullDatabaseJson;

    [SerializeField] private SunriseLightingController sunriseLightingController;

    private int currentRoundIndex = 0;
    private Vector3 centerOrigin;
    private bool isResolving = false;
    public int score = 0;

    void Start()
    {
        if (useDynamicDatabase && fullDatabaseJson != null)
        {
            // Generate enough rounds to win, plus a huge buffer for wrong guesses
            rounds = DynamicRoundGenerator.GenerateRounds(fullDatabaseJson, requiredScoreToWin + 10);
        }
        else if (roundsJsonFile != null)
        {
            GameRoundsWrapper data = JsonUtility.FromJson<GameRoundsWrapper>(roundsJsonFile.text);
            if (data != null && data.rounds != null && data.rounds.Length > 0)
            {
                rounds = data.rounds;
            }
        }

        if (sunriseLightingController == null)
        {
            TryGetComponent(out sunriseLightingController);
        }

        if (sunriseLightingController != null)
        {
            // Now the sunrise progress is mapped to requiredScoreToWin instead of total rounds!
            sunriseLightingController.Initialize(requiredScoreToWin, score);
        }

        if (centerWordDisplay != null)
        {
            centerOrigin = centerWordDisplay.transform.position;
            centerWordDisplay.enableWordWrapping = false;
        }
        
        if (leftCategoryDisplay != null) leftCategoryDisplay.enableWordWrapping = false;
        if (rightCategoryDisplay != null) rightCategoryDisplay.enableWordWrapping = false;

        // Ensure the splash screen is hidden during gameplay
        if (splashScreenDisplay != null) splashScreenDisplay.gameObject.SetActive(false);

        LoadRound(0);
    }

    void Update()
    {
        if (isResolving || currentRoundIndex >= rounds.Length || score >= requiredScoreToWin) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Player input: Left (A or Left Arrow), Right (D or Right Arrow)
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(ResolveChoice(true));
        }
        else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(ResolveChoice(false));
        }
    }

    void LoadRound(int index)
    {
        // End the game if they hit the sun threshold, OR if we run out of words
        if (score >= requiredScoreToWin || index >= rounds.Length)
        {
            EndGame();
            return;
        }

        WordRound r = rounds[index];
        centerWordDisplay.text = r.word;
        centerWordDisplay.transform.position = centerOrigin;
        centerWordDisplay.color = Color.white;

        if (leftCategoryDisplay) leftCategoryDisplay.text = r.leftCategory;
        if (rightCategoryDisplay) rightCategoryDisplay.text = r.rightCategory;
    }

    void EndGame()
    {
        // Hide all gameplay elements to clean up the screen
        if (leftCategoryDisplay) leftCategoryDisplay.gameObject.SetActive(false);
        if (rightCategoryDisplay) rightCategoryDisplay.gameObject.SetActive(false);
        
        if (splashScreenDisplay != null)
        {
            if (centerWordDisplay) centerWordDisplay.gameObject.SetActive(false);
            
            splashScreenDisplay.gameObject.SetActive(true);
            splashScreenDisplay.text = $"FIRST LIGHT REACHED!\n\nScore: {score} / {currentRoundIndex}";
            splashScreenDisplay.color = Color.yellow;
        }
        else
        {
            // Fallback if no dedicated splash screen is assigned
            centerWordDisplay.transform.position = centerOrigin + Vector3.up * 1.5f;
            centerWordDisplay.text = $"FIRST LIGHT REACHED!\n\nScore: {score} / {currentRoundIndex}";
            centerWordDisplay.color = Color.yellow;
        }
    }

    IEnumerator ResolveChoice(bool choseLeft)
    {
        isResolving = true;
        Transform target = choseLeft ? leftTarget : rightTarget;
        Vector3 targetPos = target != null ? target.position : (centerOrigin + (choseLeft ? Vector3.left : Vector3.right) * 6f);

        // Glide the word to the chosen side
        while (Vector3.Distance(centerWordDisplay.transform.position, targetPos) > 0.1f)
        {
            centerWordDisplay.transform.position = Vector3.MoveTowards(
                centerWordDisplay.transform.position, 
                targetPos, 
                flySpeed * Time.deltaTime
            );
            yield return null;
        }

        // Evaluate answer
        bool isCorrect = (choseLeft == rounds[currentRoundIndex].isLeftCorrect);
        
        // Trigger visual pulse on the target AND the Text display!
        StartCoroutine(PulseTarget(target, isCorrect));
        
        Transform textTarget = choseLeft ? leftCategoryDisplay.transform : rightCategoryDisplay.transform;
        if (textTarget != null && textTarget != target) 
        {
            StartCoroutine(PulseTarget(textTarget, isCorrect));
        }

        if (isCorrect)
        {
            score++;

            if (sunriseLightingController != null)
            {
                sunriseLightingController.UpdateSuccessCount(score);
            }

            centerWordDisplay.color = Color.yellow; // Visual hit feedback
            Debug.Log($"Correct! Score: {score}");
        }
        else
        {
            centerWordDisplay.color = Color.gray;
            Debug.Log($"Wrong! Score: {score}");
        }

        yield return new WaitForSeconds(0.4f);

        currentRoundIndex++;
        LoadRound(currentRoundIndex);
        isResolving = false;
    }

    IEnumerator PulseTarget(Transform target, bool isCorrect)
    {
        if (target == null) yield break;

        Color pulseColor = isCorrect ? Color.green : Color.red;
        
        // Grab EVERYTHING that can potentially be colored
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        UnityEngine.UI.Graphic[] graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>();

        if (renderers.Length == 0 && graphics.Length == 0 && texts.Length == 0)
        {
            Debug.LogWarning($"[PulseTarget] Could not find any visual components on {target.name} or its children to color!");
        }

        Vector3 originalScale = target.localScale;
        Vector3 punchScale = originalScale * 1.4f;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Pop the scale up and down using a Sine wave (0 -> 1 -> 0)
            float scaleT = Mathf.Sin(t * Mathf.PI);
            target.localScale = Vector3.Lerp(originalScale, punchScale, scaleT);

            Color frameColor = Color.Lerp(pulseColor, Color.white, t);

            // Fade the color on everything we found
            foreach (var r in renderers) 
            {
                if (r.material.HasProperty("_Color") || r.material.HasProperty("_BaseColor")) 
                    r.material.color = frameColor; 
            }
            foreach (var g in graphics) { g.color = frameColor; }
            foreach (var txt in texts) { txt.color = frameColor; }

            yield return null;
        }

        // Reset precisely to normal at the end
        target.localScale = originalScale;
        foreach (var r in renderers) 
        {
            if (r.material.HasProperty("_Color") || r.material.HasProperty("_BaseColor")) 
                r.material.color = Color.white; 
        }
        foreach (var g in graphics) { g.color = Color.white; }
        foreach (var txt in texts) { txt.color = Color.white; }
    }
}