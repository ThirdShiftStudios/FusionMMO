using System;
using UnityEngine;

public class RollingButton : MonoBehaviour
{
    public SlotMachine smh;

    public Func<bool> ExternalPressHandler { get; set; }

    private Animator _animator;

    private void Start()
    {
        smh.onRollComplete += RollComplete;
        _animator = GetComponent<Animator>();
    }

    private void RollComplete(int[] score, int matchScore)
    {
        string[] objectName = new string[4] { "Cherry", "Pineapple", "Banana", "Orange" };
        for (int i = 0; i < score.Length; i++)
        {
            print(objectName[i] + " Score : " + score[i]);
        }
        print(matchScore);
    }

    void OnMouseDown()
    {
        Debug.Log($"[RollingButton] OnMouseDown on {name}: handlerAssigned={(ExternalPressHandler != null)}, canRoll={(smh != null && smh.canRoll())}, position={transform.position}");

        if (ExternalPressHandler != null && ExternalPressHandler.Invoke() == true)
            return;

        if (smh != null && smh.canRoll())
            RollLocal();
    }

    public void PlayRollAnimation()
    {
        RollAnimation();
    }

    private void RollLocal()
    {
        smh.Roll();
        RollAnimation();
    }

    private void RollAnimation()
    {
        if (_animator != null)
            _animator.Play("Rolling");
    }
}
