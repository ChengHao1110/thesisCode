using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class offMeshLinkNavigation : MonoBehaviour
{
    string agentName;
    NavMeshAgent agent;
    Animator animator;

    IEnumerator Start()
    {
        agentName = this.transform.name;
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
        animator = GetComponent<Animator>();
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {                
                yield return StartCoroutine(NormalSpeed(agent));
                agent.CompleteOffMeshLink();
                // don't stop at the end of the path
                dynamicSystem.instance.people[agentName].walkStopState = "walk";
                dynamicSystem.instance.people[agentName].ifMoveNavMeshAgent(true);
            }

            yield return null;
        }
    }    

    IEnumerator NormalSpeed(NavMeshAgent agent)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
                
        while (agent.transform.position != endPos)
        {
            animator.SetBool("walk", true);
            animator.SetFloat("walkSpeed", 1);
            animator.SetFloat("speed", 1);
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            this.transform.LookAt(endPos);

            yield return null;
        }
    }
}
