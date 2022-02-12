using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class navTest : MonoBehaviour
{
    public GameObject sign;
    List<Vector3> targetList = new List<Vector3> {        
        new Vector3(-4, 0, -3),
        new Vector3(4, 0, -3),
        new Vector3(4, 0, 3),
        new Vector3(-4, 0, 3),
    };
    int targetId = 1;
    NavMeshAgent agent;
    Animator animator;

    void Start()
    {
        foreach(Vector3 target in targetList)
        {
            Instantiate(sign, target, Quaternion.identity);
        }

        /* set human */
        this.transform.position = targetList[0];
        agent = this.GetComponent<NavMeshAgent>();
        float agentSpeed = 60 * 0.5f;
        agent.speed *= 0.5f * (agentSpeed / 100f);
        agent.acceleration *= 0.25f * (agentSpeed / 100f);
        agent.SetDestination(targetList[targetId]);

        animator = this.GetComponent<Animator>();
        animator.SetBool("walk", true);
        animator.SetFloat("speed", 0.5f);
        animator.SetFloat("walkSpeed", 1);
    }

    void Update()
    {
        if(agent.remainingDistance < 0.1f)
        {
            if (++targetId >= targetList.Count) targetId = 0;
            agent.SetDestination(targetList[targetId]);
            this.transform.LookAt(targetList[targetId]);
        }
        Debug.Log(agent.velocity.magnitude);
    }
}
