using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class DynamicDifficultyStars : MonoBehaviour
{
    [Header("References")]
    public GameObject starPrefab; // The star GameObject to spawn
    public SimpleWordChooser wordChooser;
    
    [Header("Grid Settings")]
    public int maxStars = 5;
    public float starSize = 30f;
    public float spacing = 8f;
    
    [Header("Animation")]
    public float appearDuration = 0.5f;
    public float disappearDuration = 0.5f;
    
    [Header("Auto-Find")]
    public bool autoFindGameManager = true;
    public string gameManagerTag = "GameManager";
    
    private float currentDifficulty = 0f;
    private int currentStarCount = 0;
    private List<GameObject> starObjects = new List<GameObject>();
    private List<Coroutine> activeAnimations = new List<Coroutine>();
    private bool isInitialized = false;
    
    public Transform starsContainer; // Drag StarsContainer here

    void Start()
    {
        Debug.Log($"DifficultyStars: Start called. wordChooser = {(wordChooser != null ? "Found" : "NULL")}");
        Debug.Log($"DifficultyStars: starPrefab = {(starPrefab != null ? "Assigned" : "NULL")}");

        // Auto-find GameManager
        if (autoFindGameManager && wordChooser == null)
        {
            GameObject gm = GameObject.FindGameObjectWithTag(gameManagerTag);
            if (gm == null)
            {
                gm = GameObject.Find("GameManager");
            }
            
            if (gm != null)
            {
                wordChooser = gm.GetComponent<SimpleWordChooser>();
                if (wordChooser != null)
                {
                    Debug.Log("DifficultyStars: Found GameManager!");
                }
                else
                {
                    Debug.LogWarning("DifficultyStars: GameManager found but no SimpleWordChooser component!");
                }
            }
            else
            {
                Debug.LogWarning("DifficultyStars: GameManager not found! Make sure it's in the scene.");
            }
        }
        
        // Create the star grid
        CreateStarGrid();
        
        // Get initial difficulty
        UpdateDifficulty();
        isInitialized = true;
    }
    
    void CreateStarGrid()
    {
        // Clear existing stars
        foreach (GameObject star in starObjects)
        {
            if (star != null)
                Destroy(star);
        }
        starObjects.Clear();
        
        // Stop all animations
        foreach (Coroutine coroutine in activeAnimations)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeAnimations.Clear();
        
        // Use the starsContainer, or fallback to this transform
        Transform parent = starsContainer != null ? starsContainer : transform;

        // Create new stars
        for (int i = 0; i < maxStars; i++)
        {
            GameObject starObj;
            
            if (starPrefab != null)
            {
                starObj = Instantiate(starPrefab, parent);
            }
            else
            {
                // Create default star if no prefab
                starObj = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
                starObj.transform.SetParent(parent, false);
                
                // Set size
                RectTransform rect = starObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(starSize, starSize);
                
                // Set default color
                Image img = starObj.GetComponent<Image>();
                img.color = Color.yellow;
            }
            
            // Start with star hidden
            starObj.SetActive(false);
            starObjects.Add(starObj);
        }
        
        Debug.Log($"DifficultyStars: Created {starObjects.Count} stars");
    }
    
    void Update()
    {
        if (!isInitialized) return;
        if (wordChooser == null) return;
        
        UpdateDifficulty();
    }
    
    void UpdateDifficulty()
    {
        float newDifficulty = GetDifficulty();
        int newStarCount = DifficultyToStars(newDifficulty);
        
        // Only update if changed
        if (newStarCount != currentStarCount)
        {
            currentStarCount = newStarCount;
            UpdateStars(currentStarCount);
            Debug.Log($"DifficultyStars: Updated to {currentStarCount} stars (difficulty: {newDifficulty:F2})");
        }
    }
    
    float GetDifficulty()
    {
        try
        {
            // Try to get currentDifficulty (it might be private)
            FieldInfo field = wordChooser.GetType().GetField("currentDifficulty", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null)
            {
                return (float)field.GetValue(wordChooser);
            }
            
            // If not found, try to get it from the score
            FieldInfo scoreField = wordChooser.GetType().GetField("score", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (scoreField != null)
            {
                int score = (int)scoreField.GetValue(wordChooser);
                // Convert score to difficulty (0-1)
                FieldInfo requiredField = wordChooser.GetType().GetField("requiredScoreToWin");
                if (requiredField != null)
                {
                    int required = (int)requiredField.GetValue(wordChooser);
                    if (required > 0)
                    {
                        return Mathf.Clamp01((float)score / required);
                    }
                }
                return Mathf.Clamp01((float)score / 12f); // Default fallback
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"DifficultyStars: Error reading difficulty - {e.Message}");
        }
        
        return 0f;
    }
    
    int DifficultyToStars(float difficulty)
    {
        // Map 0-1 difficulty to 0-5 stars
        if (difficulty < 0.15f) return 1;
        if (difficulty < 0.35f) return 2;
        if (difficulty < 0.55f) return 3;
        if (difficulty < 0.75f) return 4;
        return 5;
    }
    
    void UpdateStars(int starCount)
    {
        starCount = Mathf.Clamp(starCount, 0, starObjects.Count);
        
        for (int i = 0; i < starObjects.Count; i++)
        {
            bool shouldBeVisible = i < starCount;
            GameObject star = starObjects[i];
            
            if (star == null) continue;
            
            // Stop any existing animation for this star
            if (i < activeAnimations.Count && activeAnimations[i] != null)
            {
                StopCoroutine(activeAnimations[i]);
                activeAnimations[i] = null;
            }
            
            // Start new animation
            Coroutine newCoroutine;
            if (shouldBeVisible)
            {
                newCoroutine = StartCoroutine(AnimateStarIn(star));
            }
            else
            {
                newCoroutine = StartCoroutine(AnimateStarOut(star));
            }
            
            // Store the coroutine
            while (activeAnimations.Count <= i)
            {
                activeAnimations.Add(null);
            }
            activeAnimations[i] = newCoroutine;
        }
    }
    
    IEnumerator AnimateStarIn(GameObject star)
    {
        // Make sure star is active
        star.SetActive(true);
        
        // Get image component
        Image img = star.GetComponent<Image>();
        RectTransform rect = star.GetComponent<RectTransform>();
        
        // Initial state: small and transparent
        Vector3 originalScale = rect != null ? rect.localScale : Vector3.one;
        Vector3 startScale = originalScale * 0.1f;
        Vector3 endScale = originalScale;
        
        if (rect != null)
            rect.localScale = startScale;
        
        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
        
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            
            // Ease out (quick start, slow end)
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Scale: pop effect
            if (rect != null)
            {
                float scaleT = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
                rect.localScale = originalScale * Mathf.Lerp(0.1f, scaleT, easedT);
            }
            
            // Fade in
            if (img != null)
            {
                Color c = img.color;
                c.a = easedT;
                img.color = c;
            }
            
            yield return null;
        }
        
        // Final state
        if (rect != null)
            rect.localScale = originalScale;
        
        if (img != null)
        {
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }
    }
    
    IEnumerator AnimateStarOut(GameObject star)
    {
        if (star == null) yield break;
        
        Image img = star.GetComponent<Image>();
        RectTransform rect = star.GetComponent<RectTransform>();
        
        Vector3 originalScale = rect != null ? rect.localScale : Vector3.one;
        
        float elapsed = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disappearDuration;
            
            // Ease in (slow start, quick end)
            float easedT = t * t;
            
            // Scale shrink
            if (rect != null)
            {
                rect.localScale = originalScale * (1f - easedT * 0.8f);
            }
            
            // Fade out
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f - easedT;
                img.color = c;
            }
            
            yield return null;
        }
        
        // Hide the star
        star.SetActive(false);
        
        // Reset scale
        if (rect != null)
            rect.localScale = originalScale;
        
        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }
    
    // Public method to manually set stars (for testing)
    public void SetStarsManually(int starCount)
    {
        currentStarCount = Mathf.Clamp(starCount, 0, maxStars);
        UpdateStars(currentStarCount);
        Debug.Log($"DifficultyStars: Manually set to {currentStarCount} stars");
    }
}