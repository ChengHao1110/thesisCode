using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class visitorHeadMotionClass : MonoBehaviour
{
    public GameObject head;
    public GameObject viewPoint;
    public List<ViewPointAttribute> viewPoints = new List<ViewPointAttribute>(); // in humanClass.cs
    public int viewPointIdx;
    public Vector3 targetPos = new Vector3();
    public float movingDistance = 0.0f, watchingTime = 0.0f;
    public bool isPointMoving = false, isWatching = false, hasTarget = false;

    public void ViewPointMoving()
    {
        if (!hasTarget)
        {
            int nextViewPointIdx = viewPointIdx + 1;
            if (nextViewPointIdx == viewPoints.Count) nextViewPointIdx = 0;
            targetPos = viewPoints[nextViewPointIdx].viewPoint.transform.position;
            isPointMoving = true;
            hasTarget = true;
            float movingTime = RandomGaussian(3f, 2f, 1f, 5f);
            movingDistance = Vector3.Distance(viewPoint.transform.position, targetPos) / movingTime / 150; //30fps 2 coeffecient
            Debug.Log(movingDistance);
        }

        if (isWatching)
        {
            watchingTime -= Time.deltaTime;
            if (watchingTime < 0) 
            { 
                isWatching = false;
                hasTarget = false;
            }
            return;
        }

        if (isPointMoving)
        {
            viewPoint.transform.position = Vector3.MoveTowards(viewPoint.transform.position, targetPos, movingDistance);
            if (Vector3.Distance(viewPoint.transform.position, targetPos) < 0.001f)
            {
                viewPointIdx++;
                if (viewPointIdx == viewPoints.Count) viewPointIdx = 0;
                watchingTime = RandomGaussian(2f, 3f, 0.5f, 8f);
                isPointMoving = false;
                isWatching = true;
            }
        }



        /*
        int nextViewPointIdx = viewPointIdx + 1;
        if (nextViewPointIdx == viewPoints.Count) nextViewPointIdx = 0;

        Vector3 currentViewPos = viewPoint.transform.position;
        Vector3 targetViewPos = viewPoints[nextViewPointIdx].viewPoint.transform.position;

        viewPoint.transform.position = Vector3.MoveTowards(currentViewPos, targetViewPos, 0.002f);
        if (Vector3.Distance(viewPoint.transform.position, targetViewPos) < 0.001f)
        {
            viewPointIdx++;
            if (viewPointIdx == viewPoints.Count) viewPointIdx = 0;
        }
        */
    }



    public static float NextGaussian()
    {
        float v1, v2, s;
        do
        {
            v1 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            v2 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0f);
        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

        return v1 * s;
    }

    public static float NextGaussian(float mean, float standard_deviation)
    {
        return mean + NextGaussian() * standard_deviation;
    }

    public static float RandomGaussian(float mean, float std, float minValue, float maxValue)
    {
        float x;
        do
        {
            x = NextGaussian(mean, std);
        }
        while (x < minValue || x > maxValue);
        return x;
    }
}
