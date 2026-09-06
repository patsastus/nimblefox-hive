using UnityEngine;
using UnityEngine.UI;
using System.Reflection; // Add this for reflection

public class LightProgressBar : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    public Image fillImage;
    public SimpleWordChooser wordChooser;

    [Header("Colors")]
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f);
    public Color dawnColor = new Color(0.8f, 0.4f, 0.1f);
    public Color sunriseColor = new Color(1f, 0.8f, 0.2f);

    private int totalRounds = 12; // Default, will be updated from GameManager
    private int currentScore = 0;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Find slider
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        // Find fill image
        if (fillImage == null)
        {
            Transform fillArea = transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    fillImage = fill.GetComponent<Image>();
                }
            }
        }

        // Find GameManager
        if (wordChooser == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm != null)
            {
                wordChooser = gm.GetComponent<SimpleWordChooser>();
            }
        }

        // Get required score to win from GameManager
        if (wordChooser != null && slider != null)
        {
            totalRounds = wordChooser.requiredScoreToWin;
            slider.minValue = 0;
            slider.maxValue = totalRounds;
            slider.value = 0;
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        if (wordChooser != null)
        {
            int newScore = wordChooser.GetScore();
            if (newScore != currentScore)
            {
                currentScore = newScore;
                UpdateProgress(currentScore);
            }
        }
    }

    void UpdateProgress(int score)
    {
        // Update slider
        if (slider != null)
        {
            slider.value = score;
        }

        // Update fill color based on progress
        if (fillImage != null)
        {
            float progress = totalRounds > 0 ? (float)score / totalRounds : 0f;
            progress = Mathf.Clamp01(progress); // Keep between 0 and 1
            
            if (progress >= 1f)
            {
                fillImage.color = sunriseColor;
            }
            else if (progress >= 0.6f)
            {
                float t = (progress - 0.6f) / 0.4f;
                fillImage.color = Color.Lerp(dawnColor, sunriseColor, t);
            }
            else if (progress >= 0.2f)
            {
                float t = (progress - 0.2f) / 0.4f;
                fillImage.color = Color.Lerp(nightColor, dawnColor, t);
            }
            else
            {
                fillImage.color = nightColor;
            }
        }
    }

    // Public method for manual updates (optional)
    public void SetProgress(int score, int total)
    {
        totalRounds = Mathf.Max(1, total);
        if (slider != null)
        {
            slider.maxValue = totalRounds;
        }
        currentScore = score;
        UpdateProgress(score);
    }
}