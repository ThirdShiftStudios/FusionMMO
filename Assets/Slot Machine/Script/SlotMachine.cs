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
    public AudioClip machineSoundLoop;
    public AudioClip jackpot;
    public AudioClip regularWinClip;
    public AudioClip fail;

    public GameObject jackpotEffect;
    public GameObject regularWinEffect;
    public float jackpotHideTime = 2f;
    public float regularWinHideTime = 2f;

    public delegate void OnRollComplete(int[] score, int matchScore);
    public OnRollComplete onRollComplete;

    private Coroutine jackpotHideCoroutine;
    private Coroutine regularWinHideCoroutine;

    void Awake()
    {
        HideWinEffects();
    }

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
            HideWinEffects();

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

            if (machineSoundLoop)
            {
                PlayMachineLoop();
            }

            tick = 0;

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
                bool isJackpot = totalScore == 3;
                bool isRegularWin = totalScore >= 2 && !isJackpot;

                StopMachineLoop();

                if (isJackpot)
                {
                    if (lightBox)
                        lightBox.material.SetTexture("_MainTex", t2);
                    if (audioSource && jackpot)
                        audioSource.PlayOneShot(jackpot);

                    ShowEffect(ref jackpotHideCoroutine, jackpotEffect, jackpotHideTime);
                }
                else if (isRegularWin)
                {
                    if (lightBox)
                        lightBox.material.SetTexture("_MainTex", t0);

                    if (audioSource && regularWinClip)
                        audioSource.PlayOneShot(regularWinClip);

                    ShowEffect(ref regularWinHideCoroutine, regularWinEffect, regularWinHideTime);
                }
                else
                {
                    if (lightBox)
                        lightBox.material.SetTexture("_MainTex", t0);
                    if (audioSource && fail)
                        audioSource.PlayOneShot(fail);

                    HideWinEffects();
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
        if (!machineSoundLoop)
        {
            if (tick >= 5)
            {
                if (audioSource && machineSound)
                    audioSource.PlayOneShot(machineSound);
                tick = 0;
            }
            tick += (power * 8) + 0.1f;
        }
    }

    private void ShowEffect(ref Coroutine routine, GameObject effect, float hideTime)
    {
        if (!effect)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        effect.SetActive(true);
        routine = StartCoroutine(HideEffectAfterDelay(effect, hideTime));
    }

    private IEnumerator HideEffectAfterDelay(GameObject effect, float hideTime)
    {
        yield return new WaitForSeconds(hideTime);
        if (effect)
            effect.SetActive(false);
    }

    private void HideWinEffects()
    {
        if (jackpotHideCoroutine != null)
        {
            StopCoroutine(jackpotHideCoroutine);
            jackpotHideCoroutine = null;
        }

        if (regularWinHideCoroutine != null)
        {
            StopCoroutine(regularWinHideCoroutine);
            regularWinHideCoroutine = null;
        }

        if (jackpotEffect && jackpotEffect.activeSelf)
            jackpotEffect.SetActive(false);

        if (regularWinEffect && regularWinEffect.activeSelf)
            regularWinEffect.SetActive(false);
    }

    private void PlayMachineLoop()
    {
        if (!audioSource || !machineSoundLoop)
            return;

        audioSource.clip = machineSoundLoop;
        audioSource.loop = true;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    private void StopMachineLoop()
    {
        if (!audioSource)
            return;

        if (audioSource.isPlaying && audioSource.clip == machineSoundLoop)
            audioSource.Stop();

        audioSource.loop = false;

        if (audioSource.clip == machineSoundLoop)
            audioSource.clip = null;
    }
}
