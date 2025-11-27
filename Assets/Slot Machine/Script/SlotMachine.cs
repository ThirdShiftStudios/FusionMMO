using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachine : MonoBehaviour
{
    public int numberOfObject = 4;
    public Renderer[] slotList;
    private float[] forcePoint;
    private int[] score;
    private bool spin = false;
    private Vector3 saveRotation;

    public bool canShack = true;

    public Renderer lightBox;
    public Texture t0;
    public Texture t1;
    public Texture t2;

    public AudioSource audioSource;
    public AudioClip machineSound;
    public AudioClip jackpot;
    public AudioClip fail;

    public delegate void OnRollComplete(int[] score, int matchScore);
    public OnRollComplete onRollComplete;

    void Start ()
    {
        forcePoint = new float[slotList.Length];
        score = new int[numberOfObject];
        for (int i = 0; i < slotList.Length; i++)
        {
            forcePoint[i] = 100;
        }

        saveRotation = transform.rotation.eulerAngles;

        for (int i = 0; i < slotList.Length; i++)
        {
            slotList[i].material.SetTextureScale("_MainTex", new Vector2(1, 1.0f / (float)numberOfObject));
        }
    }
	
    public void Roll ()
    {
        if (!spin)
        {
            for (int i = 0; i < numberOfObject; i++)
            {
                score[i] = 0;
            }

            for (int i = 0; i < slotList.Length; i++)
            {
                forcePoint[i] = UnityEngine.Random.Range(30 * (i + 1), 40 * (i + 1));
                int r = (int)UnityEngine.Random.Range(0, numberOfObject);

                score[r]++;

                float rand = (float)r / (float)numberOfObject;
                slotList[i].material.SetFloat("_SpinIndex", rand);
            }

            if (lightBox)
                lightBox.material.SetTexture("_MainTex", t1);

            spin = true;
        }
    }

    public bool canRoll ()
    {
        if (spin)
        {
            return false;
        }
        return true;
    }

	void Update ()
    {
		if (spin)
        {
            for (int i = 0; i < slotList.Length; i++)
            {
                forcePoint[i] += (0 - forcePoint[i]) / 25;
                slotList[i].material.SetFloat("_Force", forcePoint[i]);
            }
            if (forcePoint[slotList.Length - 1] <= 0.005f)
            {
                int totalScore = 0;
                foreach (int s in score)
                {
                    if (s > totalScore)
                        totalScore = s;
                }
                if (totalScore == 3)
                {
                    if (lightBox)
                        lightBox.material.SetTexture("_MainTex", t2);
                    audioSource.PlayOneShot(jackpot);
                }
                else
                {
                    if (lightBox)
                        lightBox.material.SetTexture("_MainTex", t0);
                    audioSource.PlayOneShot(fail);
                }
                spin = false;

                onRollComplete(score, totalScore);
            }
            Shack(forcePoint[slotList.Length - 1] / (40 * slotList.Length));
        }
        else
        {
            transform.rotation = Quaternion.Euler(saveRotation);
        }
	}

    private float tick = 0;
    private void Shack(float power)
    {
        if (canShack)
        {
            Vector3 newRand = new Vector3();
            newRand.x = UnityEngine.Random.Range(-1, 1) * power + saveRotation.x;
            newRand.y = UnityEngine.Random.Range(-1, 1) * power + saveRotation.y;
            newRand.z = UnityEngine.Random.Range(-1, 1) * power + saveRotation.z;

            transform.rotation = Quaternion.Euler(newRand);
        }
        if (tick >= 5)
        {
            audioSource.PlayOneShot(machineSound);
            tick = 0;
        }
        tick += (power * 8) + 0.1f;
    }
}
