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
    public int dayLength = 86400;

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
        // gaiaGlobal.GaiaTimeOfDayValue.m_todHour = 18;
        // gaiaGlobal.SetTimeOfDayTimeScale(3);
        ChangeTime();
        // UpdateSun();
        CreateEvent();
    }

    // void ChangeTOD(){
        // gaiaGlobal.GaiaTimeOfDayValue.m_todHour = 18;
        // gaiaGlobal.GaiaTimeOfDayValue.m_todMinutes = 33;
        // gaiaGlobal.GaiaTimeOfDayValue
    // }

    public void ChangeTime() {
        int spellPower = speed;
        if(instability > 0) 
        {
            spellPower = instability * speed;
        }
        time += Time.deltaTime * spellPower;

        if(time > dayLength) 
        {
            days += 1;
            time = 0;
        }

        currentTime = TimeSpan.FromSeconds (time);
    }
    
    public void CreateEvent()
    {
        if(time >= nextEventTime && days == nextEventDay) {
            string chosenEvent = ChooseEvent();
            // GameEvents chosenEvent = GameEvents.MeteorShower;
            lastEventTime = time;
            lastEventDay = days;
            SetNextEventTime();
            InitiateEvent(chosenEvent);
        }
    }

    public void SetNextEventTime()
    {
        System.Random rnd = new System.Random();
        float selectedTime = (dayLength * eventDelayDays) + rnd.Next(0, dayLength);

        if(selectedTime > dayLength) 
        { 
            selectedTime = selectedTime/4; 
        }

        if(time >= nextEventTime) {
            nextEventDay = days + 1;
        }

        nextEventTime = selectedTime;
    }

    public string ChooseEvent()
    {
        string events = "Firestorm";
        
        return events;
    }

    public void InitiateEvent(string events)
    {
        Debug.Log(events);
    }

    IEnumerator Instability()
    {
        if(instability > 0) { instability -= 5; }
        if(instability < 0) { instability = 0; }
        yield return new WaitForSeconds(1);
        Debug.Log("Instability: " + instability);
        StartCoroutine(Instability());
    }
}
