using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollingButton : MonoBehaviour
{
    public SlotMachine smh;

    private void Start()
    {
        smh.onRollComplete += RollComplete;
    }

    private void RollComplete(int[] score, int matchScore)
    {
        string[] objectName = new string[4] { "Cherry", "Pineapple", "Banana", "Orange" };
        for (int i = 0; i <score.Length; i++)
        {
            print(objectName[i] + " Score : " + score[i]);
        }
        print(matchScore);
    }

    void OnMouseDown()
    {
        if (smh.canRoll())
        {
            smh.Roll();
            GetComponent<Animator>().Play("Rolling");
        }
    }
}
