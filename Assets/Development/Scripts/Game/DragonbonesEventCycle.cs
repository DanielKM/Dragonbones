using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gaia;
using Mirror;

public class DragonbonesEventCycle : MonoBehaviour
{

    [Header("References")]
    public GaiaGlobal gaiaGlobal;

    [Header("Instability Settings")]
    public int instability = 1;
    public int maxInstability = 100;

    [Header("Time Settings")]
    // Time
    public float time;
    public TimeSpan currentTime;
    public int days;
    public int speed;
    public int dayLength = 24;

    [Header("Event Settings")]
    public float lastEventTime = 0;
    public float nextEventTime = 0;
    public int lastEventDay = 0;
    public int nextEventDay = 0;
    public int eventFrequency = 1;
    public float eventDelayDays = 0.1f;

    void Start()
    {   
        SetNextEventTime();
        StartCoroutine(Instability());
    }

    // Update is called once per frame
    void Update()
    {
        ChangeTimeScale();
        CreateEvent();
    }

    public void ChangeTimeScale() 
    {
        Gaia.GaiaGlobal.Instance.GaiaTimeOfDayValue.m_todDayTimeScale = instability * 2;
    }
    
    public void CreateEvent()
    {
        if(time == nextEventTime) {
            string chosenEvent = ChooseEvent();
            // GameEvents chosenEvent = GameEvents.MeteorShower;
            lastEventTime = time;
            lastEventDay = days;
            SetNextEventTime();
            InitiateEvent(chosenEvent);
        }
    }

    private void InitiateEvent(string chosenEvent)
    {
        Debug.Log(chosenEvent);
    }

    public void SetNextEventTime()
    {
        System.Random rnd = new System.Random();
        int selectedHour = rnd.Next(0, dayLength);

        nextEventTime = selectedHour;
    }

    public string ChooseEvent()
    {
        return "Firestorm";
    }

    IEnumerator Instability()
    {
        if(instability > 0) { instability -= 5; }
        if(instability < 0) { instability = 0; }
        yield return new WaitForSeconds(1);
        StartCoroutine(Instability());
    }
}
