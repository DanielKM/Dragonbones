using UnityEngine;
using System.Collections.Generic;
using RTSEngine.Game;
using RTSEngine.Event;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using RTSEngine.Movement;

public class Wander : MonoBehaviour, IEntityPostInitializable
{
    public float wanderRadius;
    public float wanderTimer;
 
    // private NavMeshAgent agent;
    private float timer;

    // Create movement variable
    public IMovementComponent movementComp = null;
    
    private static IMovementManager MvtMgr;
    protected IGameManager gameMgr { private set; get; }
    IEntity Entity;

    //then in the init method add the component
    public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
    {
        MvtMgr = gameMgr.transform.GetComponentInChildren<IMovementManager>();
        Entity = entity;
        timer = wanderTimer;
        movementComp = entity.MovementComponent;
        if(movementComp != null)
        {
            movementComp.MovementStart += DoSomeThingOnStartMoving;
            movementComp.MovementStop += DoSomethingOnMovementStop;
        }
    }
    
    public void DoSomeThingOnStartMoving(IMovementComponent movementComponent,MovementEventArgs eventArgs)
    {

    }

    public void DoSomethingOnMovementStop(IMovementComponent movementComponent,EventArgs eventArgs)
    {
        
    }
    
    public void Update()
    {
        // Here you can also check if the movement component is idle
        if (movementComp != null && movementComp.IsIdle)
        {
            timer += Time.deltaTime;
                if (timer >= wanderTimer) 
                {
                    Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);

                    MvtMgr.SetPathDestination(Entity, newPos, 0.0f, null, new MovementSource { playerCommand = false });
                    
                    timer = 0;
                }
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
        Vector3 randDirection = Random.insideUnitSphere * dist;
 
        randDirection += origin;
 
        NavMeshHit navHit;
 
        NavMesh.SamplePosition (randDirection, out navHit, dist, layermask);

        return navHit.position;
    }

    public void Disable()
    {
        movementComp.MovementStart -= DoSomeThingOnStartMoving;
        movementComp.MovementStop -= DoSomethingOnMovementStop;
    }
}