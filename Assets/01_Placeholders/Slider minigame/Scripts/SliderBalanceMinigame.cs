using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sliding pressure bar style minigame where the player keeps the handle inside a moving safe zone.
/// </summary>
public class SliderBalanceMinigame : MonoBehaviour
{
    [Header("Slider Elements")]
    [SerializeField] RectTransform sliderTrack;
    [SerializeField] RectTransform sliderHandle;
    [SerializeField] Slider unitySlider;
    [SerializeField] RectTransform safeZoneDisplay;
    [SerializeField] Image successProgressImage;
    [SerializeField] Image failureProgressImage;

    [Header("Handle Movement")]
    [Tooltip("Units per second the handle moves to the right while LMB is held.")]
    [SerializeField, Min(0f)] float pressSpeed = 0.5f;
    [Tooltip("Units per second the handle drifts back to the left when LMB is released.")]
    [SerializeField, Min(0f)] float releaseSpeed = 0.35f;

    [Header("Safe Zone")]
    [Range(0.01f, 1f)] public float safeZoneWidth = 0.15f;
    [SerializeField, Range(0f, 1f)] float safeZoneMidpoint = 0.5f;
    [SerializeField, Min(0f)] float safeZoneMoveSpeed = 0.65f;
    [SerializeField, Range(0f, 0.5f)] float safeZoneMovementAmplitude = 0.35f;
    [Tooltip("Set to true to use a ping-pong pattern, false for a sine pattern.")]
    [SerializeField] bool pingPongPattern = false;

    [Header("Progress")] 
    [SerializeField, Min(0f)] float successRate = 25f;
    [SerializeField, Min(0f)] float failureRate = 30f;
    [SerializeField, Min(1f)] float successTarget = 100f;
    [SerializeField, Min(1f)] float failureTarget = 100f;

    [Header("Behaviour")]
    [SerializeField] bool autoStart = true;
    [SerializeField] bool stopOnComplete = true;

    [Header("Events")]
    public UnityEvent onSuccess = new UnityEvent();
    public UnityEvent onFail = new UnityEvent();
    public UnityEvent<float> onSuccessProgress = new UnityEvent<float>();
    public UnityEvent<float> onFailureProgress = new UnityEvent<float>();
    public UnityEvent<bool> onZoneStateChanged = new UnityEvent<bool>();

    float handlePosition; // normalized 0-1
    float safeZoneCenter; // normalized 0-1
    float successProgress;
    float failureProgress;
    float safeZoneTimer;
    bool isRunning;
    bool isInsideZone;
    bool isFinished;

    void OnEnable()
    {
        if (autoStart)
            StartMinigame();
    }

    /// <summary>
    /// Starts the minigame from the beginning.
    /// </summary>
    public void StartMinigame()
    {
        handlePosition = 0f;
        safeZoneTimer = 0f;
        successProgress = 0f;
        failureProgress = 0f;
        safeZoneCenter = Mathf.Clamp(safeZoneMidpoint, GetHalfSafeZoneWidth(), 1f - GetHalfSafeZoneWidth());
        isFinished = false;
        isRunning = true;
        UpdateHandleVisual();
        UpdateSafeZoneVisual();
        UpdateProgressUI();
    }

    /// <summary>
    /// Stops the minigame without triggering win/lose logic.
    /// </summary>
    public void StopMinigame()
    {
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning || isFinished)
            return;

        float deltaTime = Time.deltaTime;
        UpdateSafeZone(deltaTime);
        UpdateHandle(deltaTime);
        UpdateProgress(deltaTime);
    }

    void UpdateHandle(float deltaTime)
    {
        bool pressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        float direction = pressed ? 1f : -1f;
        float speed = pressed ? pressSpeed : releaseSpeed;
        handlePosition += direction * speed * deltaTime;
        handlePosition = Mathf.Clamp01(handlePosition);
        UpdateHandleVisual();
    }

    void UpdateSafeZone(float deltaTime)
    {
        safeZoneTimer += deltaTime * safeZoneMoveSpeed;
        float halfWidth = GetHalfSafeZoneWidth();
        float minCenter = halfWidth;
        float maxCenter = 1f - halfWidth;

        if (pingPongPattern)
        {
            float travelRange = Mathf.Clamp(safeZoneMovementAmplitude, 0f, 0.5f);
            float pingValue = Mathf.PingPong(safeZoneTimer * (travelRange > 0f ? 1f : 0f), 1f);
            float offset = (pingValue - 0.5f) * 2f * travelRange;
            safeZoneCenter = Mathf.Clamp(safeZoneMidpoint + offset, minCenter, maxCenter);
        }
        else
        {
            float sineValue = Mathf.Sin(safeZoneTimer);
            float offset = sineValue * safeZoneMovementAmplitude;
            safeZoneCenter = Mathf.Clamp(safeZoneMidpoint + offset, minCenter, maxCenter);
        }

        UpdateSafeZoneVisual();
    }

    void UpdateProgress(float deltaTime)
    {
        float halfWidth = GetHalfSafeZoneWidth();
        float minBound = safeZoneCenter - halfWidth;
        float maxBound = safeZoneCenter + halfWidth;
        bool insideZone = handlePosition >= minBound && handlePosition <= maxBound;

        if (insideZone)
        {
            successProgress += successRate * deltaTime;
            successProgress = Mathf.Min(successProgress, successTarget);
        }
        else
        {
            failureProgress += failureRate * deltaTime;
            failureProgress = Mathf.Min(failureProgress, failureTarget);
        }

        if (insideZone != isInsideZone)
        {
            isInsideZone = insideZone;
            onZoneStateChanged?.Invoke(isInsideZone);
        }

        onSuccessProgress?.Invoke(successProgress / successTarget);
        onFailureProgress?.Invoke(failureProgress / failureTarget);
        UpdateProgressUI();

        if (successProgress >= successTarget)
            CompleteMinigame(true);
        else if (failureProgress >= failureTarget)
            CompleteMinigame(false);
    }

    void UpdateHandleVisual()
    {
        if (unitySlider != null)
        {
            float normalized = Mathf.Clamp01(handlePosition);
            unitySlider.minValue = 0f;
            unitySlider.maxValue = 1f;
            unitySlider.wholeNumbers = false;
            unitySlider.SetValueWithoutNotify(normalized);

            RectTransform handleRect = unitySlider.handleRect;
            RectTransform container = handleRect != null ? handleRect.parent as RectTransform : null;
            if (handleRect != null && container != null)
            {
                float travelWidth = Mathf.Max(0f, container.rect.width - handleRect.rect.width);
                float minX = -travelWidth * 0.5f;
                float maxX = travelWidth * 0.5f;
                Vector2 anchorPos = handleRect.anchoredPosition;
                anchorPos.x = Mathf.Lerp(minX, maxX, normalized);
                handleRect.anchoredPosition = anchorPos;
            }

            return;
        }

        if (sliderTrack == null || sliderHandle == null)
            return;

        float width = sliderTrack.rect.width;
        float travelWidth = Mathf.Max(0f, width - sliderHandle.rect.width);
        Vector2 anchorPos = sliderHandle.anchoredPosition;
        float minX = -travelWidth * 0.5f;
        float maxX = travelWidth * 0.5f;
        anchorPos.x = Mathf.Lerp(minX, maxX, handlePosition);
        sliderHandle.anchoredPosition = anchorPos;
    }

    void UpdateSafeZoneVisual()
    {
        if (safeZoneDisplay == null)
            return;

        float halfWidth = GetHalfSafeZoneWidth();
        float minBound = safeZoneCenter - halfWidth;
        float maxBound = safeZoneCenter + halfWidth;
        minBound = Mathf.Clamp01(minBound);
        maxBound = Mathf.Clamp01(maxBound);
        safeZoneDisplay.anchorMin = new Vector2(minBound, safeZoneDisplay.anchorMin.y);
        safeZoneDisplay.anchorMax = new Vector2(maxBound, safeZoneDisplay.anchorMax.y);
        safeZoneDisplay.offsetMin = Vector2.zero;
        safeZoneDisplay.offsetMax = Vector2.zero;
    }

    void UpdateProgressUI()
    {
        if (successProgressImage != null)
            successProgressImage.fillAmount = successTarget <= 0f ? 0f : successProgress / successTarget;

        if (failureProgressImage != null)
            failureProgressImage.fillAmount = failureTarget <= 0f ? 0f : failureProgress / failureTarget;
    }

    void CompleteMinigame(bool success)
    {
        if (isFinished)
            return;

        isFinished = true;
        if (stopOnComplete)
            isRunning = false;

        if (success)
            onSuccess?.Invoke();
        else
            onFail?.Invoke();
    }

    /// <summary>
    /// Returns the current normalized handle position.
    /// </summary>
    public float HandlePosition => handlePosition;

    /// <summary>
    /// Returns the normalized bounds of the safe zone.
    /// </summary>
    public Vector2 SafeZoneBounds
    {
        get
        {
            float halfWidth = GetHalfSafeZoneWidth();
            return new Vector2(safeZoneCenter - halfWidth, safeZoneCenter + halfWidth);
        }
    }

    /// <summary>
    /// Returns true if the minigame is still running.
    /// </summary>
    public bool IsRunning => isRunning && !isFinished;

    float GetHalfSafeZoneWidth()
    {
        return Mathf.Clamp(safeZoneWidth * 0.5f, 0.01f, 0.49f);
    }
}
