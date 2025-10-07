using UnityEngine;

public class AnimatorFrameStepper : MonoBehaviour
{
    [Tooltip("Animator to toggle on/off")]
    public Animator targetAnimator;

    [Tooltip("Number of frames to wait before showing the next animation frame")]
    public int frameInterval = 5;

    private int frameCount;

    void Start()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        targetAnimator.enabled = false;
    }

    void Update()
    {
        frameCount++;

        // Enable animator for one frame, then disable again
        if (frameCount % frameInterval == 0)
        {
            targetAnimator.enabled = true;
        }
        else if (targetAnimator.enabled)
        {
            targetAnimator.enabled = false;
        }
    }
}