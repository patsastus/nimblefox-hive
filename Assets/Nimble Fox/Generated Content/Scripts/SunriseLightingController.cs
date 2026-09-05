using UnityEngine;
using UnityEngine.Rendering;

public class SunriseLightingController : MonoBehaviour
{
    [Header("Driven Scene References")]
    [SerializeField] private Light directionalSun;
    [SerializeField] private Camera environmentCamera;

    [Header("Sun Progression")]
    [SerializeField] private Vector3 preDawnSunRotation = new Vector3(-10f, -30f, 0f);
    [SerializeField] private Vector3 morningSunRotation = new Vector3(35f, -30f, 0f);
    [SerializeField] private float preDawnDirectionalIntensity = 0.005f;
    [SerializeField] private float morningDirectionalIntensity = 1f;
    [SerializeField] private Color preDawnDirectionalColor = new Color(0.18f, 0.24f, 0.38f);
    [SerializeField] private Color morningDirectionalColor = new Color(1f, 0.72f, 0.4f);

    [Header("Ambient Environment")]
    [SerializeField] private Color preDawnAmbientSkyColor = new Color(0.003f, 0.006f, 0.018f);
    [SerializeField] private Color morningAmbientSkyColor = new Color(0.52f, 0.68f, 0.9f);
    [SerializeField] private Color preDawnAmbientEquatorColor = new Color(0.008f, 0.012f, 0.03f);
    [SerializeField] private Color morningAmbientEquatorColor = new Color(0.62f, 0.56f, 0.48f);
    [SerializeField] private Color preDawnAmbientGroundColor = new Color(0.002f, 0.003f, 0.008f);
    [SerializeField] private Color morningAmbientGroundColor = new Color(0.22f, 0.2f, 0.17f);
    [SerializeField] private float preDawnAmbientIntensity = 0.03f;
    [SerializeField] private float morningAmbientIntensity = 1f;

    [Header("Camera Sky")]
    [SerializeField] private Color preDawnCameraSkyColor = new Color(0.001f, 0.004f, 0.015f);
    [SerializeField] private Color morningCameraSkyColor = new Color(0.35f, 0.63f, 0.9f);

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1.5f;

    private int totalSuccessSteps;
    private float currentProgress;
    private float targetProgress;

    private float transitionStartProgress;
    private float transitionElapsed;

    private void Awake()
    {
        ResolveReferences();

        currentProgress = 0f;
        targetProgress = 0f;
        transitionStartProgress = 0f;
        transitionElapsed = 0f;

        ApplyLighting(0f);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentProgress, targetProgress))
        {
            return;
        }

        if (transitionDuration <= 0f)
        {
            currentProgress = targetProgress;
            ApplyLighting(currentProgress);
            return;
        }

        transitionElapsed += Time.deltaTime;

        float linearProgress = Mathf.Clamp01(transitionElapsed / transitionDuration);
        float smoothedProgress = linearProgress * linearProgress * (3f - 2f * linearProgress);

        currentProgress = Mathf.Lerp(transitionStartProgress, targetProgress, smoothedProgress);

        if (linearProgress >= 1f)
        {
            currentProgress = targetProgress;
        }

        ApplyLighting(currentProgress);
    }

    public void Initialize(int availableSuccessSteps, int currentSuccessCount)
    {
        ResolveReferences();

        totalSuccessSteps = Mathf.Max(0, availableSuccessSteps);

        if (totalSuccessSteps == 0)
        {
            targetProgress = 0f;
        }
        else
        {
            int clampedSuccessCount = Mathf.Clamp(currentSuccessCount, 0, totalSuccessSteps);
            targetProgress = (float)clampedSuccessCount / totalSuccessSteps;
        }

        currentProgress = targetProgress;
        transitionStartProgress = currentProgress;
        transitionElapsed = transitionDuration;

        ApplyLighting(currentProgress);
    }

    public void UpdateSuccessCount(int successfulChoiceCount)
    {
        float newTargetProgress;

        if (totalSuccessSteps <= 0)
        {
            newTargetProgress = 0f;
        }
        else
        {
            int clampedSuccessCount = Mathf.Clamp(successfulChoiceCount, 0, totalSuccessSteps);
            newTargetProgress = (float)clampedSuccessCount / totalSuccessSteps;
        }

        if (Mathf.Approximately(targetProgress, newTargetProgress))
        {
            return;
        }

        targetProgress = Mathf.Clamp01(newTargetProgress);
        transitionStartProgress = currentProgress;
        transitionElapsed = 0f;

        if (transitionDuration <= 0f)
        {
            currentProgress = targetProgress;
            ApplyLighting(currentProgress);
        }
    }

    private void ResolveReferences()
    {
        if (directionalSun == null)
        {
            directionalSun = RenderSettings.sun;
        }

        if (environmentCamera == null)
        {
            environmentCamera = Camera.main;
        }

        if (directionalSun != null)
        {
            RenderSettings.sun = directionalSun;
        }
    }

    private void ApplyLighting(float normalizedProgress)
    {
        ResolveReferences();

        float progress = Mathf.Clamp01(normalizedProgress);

        // Injecting the dramatic cinematic curves we built earlier
        float dramaCurve = (progress < 0.5f) ? Mathf.Pow(progress / 0.5f, 1.5f) : 1f;
        float softCurve = (progress >= 0.5f) ? Mathf.SmoothStep(0f, 1f, (progress - 0.5f) / 0.5f) : 0f;
        float visualProgress = (progress < 0.5f) ? (dramaCurve * 0.5f) : (0.5f + softCurve * 0.5f);

        if (directionalSun != null)
        {
            directionalSun.transform.rotation = Quaternion.Slerp(
                Quaternion.Euler(preDawnSunRotation),
                Quaternion.Euler(morningSunRotation),
                visualProgress);

            directionalSun.intensity = Mathf.Lerp(
                preDawnDirectionalIntensity,
                morningDirectionalIntensity,
                visualProgress);

            directionalSun.color = Color.Lerp(
                preDawnDirectionalColor,
                morningDirectionalColor,
                visualProgress);
        }

        // We let Unity's procedural skybox handle the ambient lighting automatically!
        // Forcing ambient colors every frame causes the Global Illumination to flicker (the "brown flash").
        /*
        RenderSettings.ambientSkyColor = Color.Lerp(
            preDawnAmbientSkyColor,
            morningAmbientSkyColor,
            visualProgress);

        RenderSettings.ambientEquatorColor = Color.Lerp(
            preDawnAmbientEquatorColor,
            morningAmbientEquatorColor,
            visualProgress);

        RenderSettings.ambientGroundColor = Color.Lerp(
            preDawnAmbientGroundColor,
            morningAmbientGroundColor,
            visualProgress);

        RenderSettings.ambientIntensity = Mathf.Lerp(
            preDawnAmbientIntensity,
            morningAmbientIntensity,
            visualProgress);
        */

        // Removed the code that forces CameraClearFlags.SolidColor so the procedural Skybox (and sun) remains visible!
        if (environmentCamera != null && environmentCamera.clearFlags == CameraClearFlags.SolidColor)
        {
            environmentCamera.backgroundColor = Color.Lerp(
                preDawnCameraSkyColor,
                morningCameraSkyColor,
                visualProgress);
        }
    }
}