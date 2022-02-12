/* 算path */
/* 
NavMeshPath newPath = new NavMeshPath();
person.Value.model.GetComponent<NavMeshAgent>().CalculatePath(person.Value.nextTarget_pos, newPath);
if (newPath.status == NavMeshPathStatus.PathComplete)
{
    Debug.Log("find " + newPath.corners.Length + " corners!");
    foreach(Vector3 cor in newPath.corners)
    {
        Instantiate(signPrefab, cor, Quaternion.identity);
    }
}
*/

/* Get corners of cube method 1 (not every case success) */
/*
 * 
                    MeshRenderer rend = newExhibit.range.GetComponent<MeshRenderer>();
                    Bounds rendBound = rend.bounds;
                    // Debug.Log(rend);
                    Debug.Log(exhibit.Key + ": min" + rendBound.min + ", max" + rendBound.max);
                    // Vector3 A = new Vector3(rendBound.min.x + rendBound.extents.x, 0, rendBound.min.z);
                    // Vector3 B = new Vector3(rendBound.min.x, 0, rendBound.max.z - rendBound.extents.z);
                    // Vector3 C = new Vector3(rendBound.max.x, 0, rendBound.min.z + rendBound.extents.z);
                    // Vector3 D = new Vector3(rendBound.max.x - rendBound.extents.x, 0, rendBound.max.z);
                    Vector3 A = new Vector3(rendBound.center.x - rendBound.extents.x, 0, rendBound.center.z - rendBound.center.z);
                    Vector3 B = new Vector3(rendBound.center.x + rendBound.extents.x, 0, rendBound.center.z - rendBound.extents.z);
                    Vector3 C = new Vector3(rendBound.center.x + rendBound.extents.x, 0, rendBound.center.z + rendBound.extents.z);
                    Vector3 D = new Vector3(rendBound.center.x - rendBound.extents.x, 0, rendBound.center.z + rendBound.extents.z);
                    GameObject sign4 = Instantiate(signPrefab, rendBound.center, Quaternion.identity);
                    sign4.name = exhibit.Key + "_center";
                    GameObject sign3 = Instantiate(signPrefab2, A, Quaternion.identity);
                    sign3.name = exhibit.Key + "_minXminZ";
                    GameObject sign2 = Instantiate(signPrefab2, B, Quaternion.identity);
                    sign2.name = exhibit.Key + "_minXmaxZ";
                    GameObject sign5 = Instantiate(signPrefab2, C, Quaternion.identity);
                    sign5.name = exhibit.Key + "_maxXminZ";
                    GameObject sign6 = Instantiate(signPrefab2, D, Quaternion.identity);
                    sign6.name = exhibit.Key + "_maxXmaxZ";
 */