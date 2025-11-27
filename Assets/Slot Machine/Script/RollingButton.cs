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

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) == true)
        {
            TryProcessClickFromRaycast();
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"[RollingButton] OnMouseDown on {name}: handlerAssigned={(ExternalPressHandler != null)}, canRoll={(smh != null && smh.canRoll())}, position={transform.position}");

        ProcessPress();
    }

    public void PlayRollAnimation()
    {
        RollAnimation();
    }

    private void TryProcessClickFromRaycast()
    {
        var mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit, float.MaxValue) == false)
            return;

        if (hit.collider == null)
            return;

        if (hit.collider.transform != transform && hit.collider.transform.IsChildOf(transform) == false)
            return;

        Debug.Log($"[RollingButton] Raycast click on {name}: handlerAssigned={(ExternalPressHandler != null)}, canRoll={(smh != null && smh.canRoll())}, position={hit.point}");

        ProcessPress();
    }

    private void ProcessPress()
    {
        if (ExternalPressHandler != null && ExternalPressHandler.Invoke() == true)
            return;

        if (smh != null && smh.canRoll())
            RollLocal();
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
