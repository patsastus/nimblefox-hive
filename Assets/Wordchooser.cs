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

    [Header("Data")]
    public TextAsset roundsJsonFile;

    [Header("UI / 3D Text References")]
    public TextMeshPro centerWordDisplay;
    public TextMeshPro leftCategoryDisplay;
    public TextMeshPro rightCategoryDisplay;

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

    [SerializeField] private SunriseLightingController sunriseLightingController;

    private int currentRoundIndex = 0;
    private Vector3 centerOrigin;
    private bool isResolving = false;
    public int score = 0;

    void Start()
    {
        if (roundsJsonFile != null)
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
            sunriseLightingController.Initialize(rounds != null ? rounds.Length : 0, score);
        }

        if (centerWordDisplay != null)
        {
            centerOrigin = centerWordDisplay.transform.position;
        }

        LoadRound(0);
    }

    void Update()
    {
        if (isResolving || currentRoundIndex >= rounds.Length) return;

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
        if (index >= rounds.Length)
        {
            centerWordDisplay.text = "FIRST LIGHT REACHED!";
            return;
        }

        WordRound r = rounds[index];
        centerWordDisplay.text = r.word;
        centerWordDisplay.transform.position = centerOrigin;
        centerWordDisplay.color = Color.white;

        if (leftCategoryDisplay) leftCategoryDisplay.text = r.leftCategory;
        if (rightCategoryDisplay) rightCategoryDisplay.text = r.rightCategory;
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
        
        // Trigger visual pulse on the target
        StartCoroutine(PulseTarget(target, isCorrect));

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
        Renderer rend = target.GetComponentInChildren<Renderer>();
        TMP_Text text = target.GetComponentInChildren<TMP_Text>();

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

            // Fade the color from Green/Red back to White
            if (rend != null && rend.material != null)
            {
                rend.material.color = Color.Lerp(pulseColor, Color.white, t);
            }
            if (text != null)
            {
                text.color = Color.Lerp(pulseColor, Color.white, t);
            }

            yield return null;
        }

        // Reset precisely to normal at the end
        target.localScale = originalScale;
        if (rend != null && rend.material != null) rend.material.color = Color.white;
        if (text != null) text.color = Color.white;
    }
}