using UnityEngine;
using System.Collections.Generic;

public class Simulation : MonoBehaviour
{
    [SerializeField]
    int boidCount = 1000;

    [SerializeField]
    GameObject boidPrefab;

    [SerializeField, Range(0f, 10f)]
    float neighborDistance = 2f;

    [SerializeField, Range(0f, 180f)]
    float neighborFov = 60f;

    [SerializeField, Range(0f, 10f)]
    float separationWeight = 5f;

    [SerializeField, Range(0.1f, 5f)]
    float wallDistance = 1f;

    [SerializeField, Range(1f, 10f)]
    float wallWeight = 5f;

    [SerializeField, Range(0f, 10f)]
    float alignmentWeight = 2f;

    [SerializeField, Range(0f, 10f)]
    float cohesionWeight = 2f;

    List<Boid> boids = new List<Boid>();

    void Start()
    {
        for (int i = 0; i < boidCount; ++i)
        {
            var go = Instantiate(boidPrefab, Random.insideUnitSphere, Random.rotation);
            var boid = go.GetComponent<Boid>();
            boids.Add(boid);
        }
    }

    void Update()
    {
        UpdateNeighbors();
        UpdateSeparation();
        UpdateWalls();
        UpdateAlignment();
        UpdateCohesion();
    }

    void UpdateNeighbors()
    {
        var neighborProd = Mathf.Cos(neighborFov * Mathf.Rad2Deg);

        foreach (var boid in boids)
        {
            boid.neighbors.Clear();

            foreach (var other in boids)
            {
                if (other == boid) continue;

                var to = other.pos - boid.pos;
                var dist = to.magnitude;
                if (dist < neighborDistance)
                {
                    var dir = to.normalized;
                    var fwd = boid.velocity.normalized;
                    var prod = Vector3.Dot(fwd, dir);
                    if (prod > neighborProd)
                    {
                        boid.neighbors.Add(other);
                    }
                }
            }
        }
    }

    void UpdateSeparation()
    {
        foreach (var boid in boids)
        {
            if (boid.neighbors.Count == 0) continue;

            Vector3 accel = Vector3.zero;
            foreach (var neighbor in boid.neighbors)
            {
                accel += (boid.pos - neighbor.pos).normalized;
            }
            accel /= boid.neighbors.Count;

            boid.accel += accel * separationWeight;
        }
    }

    void UpdateWalls()
    {
        var center = transform.position;
        var area = transform.localScale * 0.5f;
        var wall1 = center - area;
        var wall2 = center + area;

        foreach (var boid in boids)
        {
            boid.accel +=
                GetAccelAgainstWall(wall1.x - boid.pos.x, Vector3.right) +
                GetAccelAgainstWall(wall1.y - boid.pos.y, Vector3.up) +
                GetAccelAgainstWall(wall1.z - boid.pos.z, Vector3.forward) +
                GetAccelAgainstWall(wall2.x - boid.pos.x, Vector3.left) +
                GetAccelAgainstWall(wall2.y - boid.pos.y, Vector3.down) +
                GetAccelAgainstWall(wall2.z - boid.pos.z, Vector3.back);
        }
    }

    Vector3 GetAccelAgainstWall(float distance, Vector3 dir)
    {
        if (distance < wallDistance)
        {
            return dir * (wallWeight / Mathf.Abs(distance / wallDistance));
        }
        return Vector3.zero;
    }

    void UpdateAlignment()
    {
        foreach (var boid in boids)
        {
            if (boid.neighbors.Count == 0) continue;

            var velocity = Vector3.zero;
            foreach (var neighbor in boid.neighbors)
            {
                velocity += neighbor.velocity;
            }
            velocity /= boid.neighbors.Count;

            boid.accel += (velocity - boid.velocity) * alignmentWeight;
        }
    }

    void UpdateCohesion()
    {
        foreach (var boid in boids)
        {
            if (boid.neighbors.Count == 0) continue;

            var pos = Vector3.zero;
            foreach (var neighbor in boid.neighbors)
            {
                pos += neighbor.pos;
            }
            pos /= boid.neighbors.Count;

            boid.accel += (pos - boid.pos) * cohesionWeight;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
