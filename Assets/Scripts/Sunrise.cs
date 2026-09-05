using UnityEngine;

[ExecuteAlways]
public class Sunrise : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Assign your Directional Light here")]
    public Light sunLight;

    [Header("Dawn Progression")]
    [Range(0f, 1f)]
    [Tooltip("0 = Midnight, 0.5 = First Light, 1 = Full Dawn")]
    public float progress = 0f;

    [Header("Manual Controls (Play Mode)")]
    public float scrubSpeed = 0.3f;

    [Header("Sun Pitch (Rotation X)")]
    public float sunPitchNight = -15f;
    public float sunPitchDawn = 24f;

    // Palettes: Midnight -> Horizon Break -> Full Morning
    // Made more cinematic with deeper blacks and fierier oranges
    private readonly Color sunColorNight = new Color(0.02f, 0.03f, 0.10f);
    private readonly Color sunColorDawn = new Color(1.0f, 0.25f, 0.05f);
    private readonly Color sunColorDay = new Color(1.0f, 0.95f, 0.84f);

    private readonly Color fogColorNight = new Color(0.01f, 0.02f, 0.08f);
    private readonly Color fogColorDawn = new Color(0.6f, 0.2f, 0.3f);
    private readonly Color fogColorDay = new Color(0.49f, 0.69f, 0.84f);

    private void OnValidate()
    {
        // Updates scene view in real-time when moving the slider in the Inspector
        ApplyAtmosphere(progress);
    }

    private void ApplyAtmosphere(float t)
    {
        // Calculate the curves first
        float dramaCurve = (t < 0.5f) ? Mathf.Pow(t / 0.5f, 1.5f) : 1f;
        float softCurve = (t >= 0.5f) ? Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f) : 0f;

        // 1. Sun Rotation (Now syncs with the dramatic fade)
        float currentPitch = (t < 0.5f) 
            ? Mathf.Lerp(sunPitchNight, Mathf.Lerp(sunPitchNight, sunPitchDawn, 0.5f), dramaCurve)
            : Mathf.Lerp(Mathf.Lerp(sunPitchNight, sunPitchDawn, 0.5f), sunPitchDawn, softCurve);

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(currentPitch, sunFacingY, 0f);

            // 2. Sun Color & Intensity
            if (t < 0.5f)
            {
                sunLight.color = Color.Lerp(sunColorNight, sunColorDawn, dramaCurve);
                sunLight.intensity = Mathf.Lerp(0.05f, 1.1f, dramaCurve);
            }
            else
            {
                sunLight.color = Color.Lerp(sunColorDawn, sunColorDay, softCurve);
                sunLight.intensity = Mathf.Lerp(1.1f, 1.3f, softCurve);
            }
        }

        // 3. Unity Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        if (t < 0.5f)
        {
            RenderSettings.fogColor = Color.Lerp(fogColorNight, fogColorDawn, dramaCurve);
            // Reduced fog so it doesn't completely block the sun disk
            RenderSettings.fogDensity = Mathf.Lerp(0.045f, 0.025f, dramaCurve); 
        }
        else
        {
            RenderSettings.fogColor = Color.Lerp(fogColorDawn, fogColorDay, softCurve);
            RenderSettings.fogDensity = Mathf.Lerp(0.025f, 0.01f, softCurve);
        }
    }
[Header("Sun Orientation")]
[Range(0f, 360f)]
[Tooltip("0 = North (+Z), 90 = East (+X), 180 = South (-Z, toward camera), 270 = West (-X)")]
public float sunFacingY = 180f;

[Tooltip("Speed to spin the sun around the horizon")]
public float rotationSpeed = 45f;

void Update()
{
    if (Application.isPlaying)
    {
        // Legacy input removed to prevent crashes with the New Input System.
        // During Play mode, Wordchooser handles everything.
    }

    ApplyAtmosphere(progress);
    }
}
