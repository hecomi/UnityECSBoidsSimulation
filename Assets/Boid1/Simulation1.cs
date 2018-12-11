using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Boid1
{

public class Simulation1 : MonoBehaviour
{
    [SerializeField]
    int boidCount = 100;

    [SerializeField]
    GameObject boidPrefab;

    List<Boid1> boids_ = new List<Boid1>();
    public ReadOnlyCollection<Boid1> boids
    {
        get { return boids_.AsReadOnly(); }
    }

    void Start()
    {
        for (int i = 0; i < boidCount; ++i)
        {
            var go = Instantiate(boidPrefab, Random.insideUnitSphere, Random.rotation);
            var boid = go.GetComponent<Boid1>();
            boid.simulation = this;
            boids_.Add(boid);
        }
    }

    public Vector3 GetWallScale()
    {
        return transform.localScale * 0.5f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, transform.localScale);
    }
}

}
