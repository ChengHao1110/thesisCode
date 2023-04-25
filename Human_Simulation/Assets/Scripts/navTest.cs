using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class navTest : MonoBehaviour
{
    public GameObject sign;
    /*
    List<Vector3> targetList = new List<Vector3> {        
        new Vector3(-4, 0, -3),
        new Vector3(4, 0, -3),
        new Vector3(4, 0, 3),
        new Vector3(-4, 0, 3),
    };
    */
    List<Vector3> targetList = new List<Vector3> {
        new Vector3(4, 0, 3),
        new Vector3(-4, 0, -3),
        new Vector3(-4, 0, 3),
        new Vector3(4, 0, -3),
        
    };
    int targetId = 1;
    NavMeshAgent agent;
    Animator animator;
    public int frameRate = 200;
    float speed = 1f;
    Vector3 currentTarget;
    Vector3 finalTarget;

    void Start()
    {
        //Time.fixedDeltaTime = 1.0f / frameRate;
        //speed = 0.03333333f / Time.fixedDeltaTime;
        //Debug.Log(speed);
        Application.targetFrameRate = 240;
        foreach (Vector3 target in targetList)
        {
            Instantiate(sign, target, Quaternion.identity);
        }

        /* set human */
        //this.transform.position = targetList[0];
        agent = this.GetComponent<NavMeshAgent>();
        float agentSpeed = 60 * 0.5f * speed;
        agent.speed *= 0.5f * (agentSpeed / 100f);
        Debug.Log(agent.speed);
        agent.acceleration *= 0.25f * (agentSpeed / 100f);
        //agent.SetDestination(targetList[targetId]);
        agent.updatePosition = false;
        agent.updateRotation = false;

        animator = this.GetComponent<Animator>();
        animator.SetBool("walk", true);
        animator.SetFloat("speed", 0.5f);
        animator.SetFloat("walkSpeed", 1);
    }

    void Update()
    {
        /*
        if(agent.remainingDistance < 0.1f)
        {
            if (++targetId >= targetList.Count) targetId = 0;
            agent.SetDestination(targetList[targetId]);
            this.transform.LookAt(targetList[targetId]);
        }
        */
        //Debug.Log(agent.velocity.magnitude);
        
        //RunSimulation();
    }

    [ContextMenu("RunSimulation")]
    void RunSimulation()
    {
        Physics.autoSimulation = false;

        //To achieve deterministic physics results, you should pass a fixed step value to Physics.Simulate.
        //Usually, step should be a small positive number.
        //Using step values greater than 0.03 is likely to produce inaccurate results.
        /*
        if (agent.remainingDistance < 0.1f)
        {
            if (++targetId >= targetList.Count) targetId = 0;
            agent.SetDestination(targetList[targetId]);
            this.transform.LookAt(targetList[targetId]);
        }
        */
        //NavMeshPath
        NavMeshPath path = new NavMeshPath();

        Queue<Vector3> corner = new Queue<Vector3>();

        finalTarget = transform.position;

        for(int i = 0; i < 10000; i++) 
        {
            if (Vector3.Distance(transform.position, finalTarget) < 0.01f)
            {
                if (++targetId >= targetList.Count) targetId = 0;
                finalTarget = targetList[targetId];
                NavMesh.CalculatePath(transform.position, targetList[targetId], NavMesh.AllAreas, path);
                for (int k = 0; k < path.corners.Length - 1; k++)
                {
                    Debug.DrawLine(path.corners[k], path.corners[k + 1], Color.red);
                }
                for (int j = 1; j < path.corners.Length; j++)
                {
                    corner.Enqueue(path.corners[j]);
                }
                currentTarget = corner.Dequeue();
            }
            else
            {
                Debug.Log("11");
                Debug.Log(Vector3.Distance(transform.position, currentTarget));
                if (Vector3.Distance(transform.position, currentTarget) < 0.01f)
                {
                    Debug.Log("22");
                    currentTarget = corner.Dequeue();
                }
            }

            currentTarget.y = 0;
            Vector3 dir = (currentTarget - transform.position).normalized;
            transform.position += agent.speed * dir * 0.001f; 
            Physics.Simulate(0.001f);
        }
        
        //Checking when Pieces and Striker have stopped moving

        Physics.autoSimulation = true;
    }
}
