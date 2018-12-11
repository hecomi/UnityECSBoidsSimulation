using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Boids/Param")]
public class BoidParam : ScriptableObject
{
    public float initSpeed = 2f;
    public float minSpeed = 2f;
    public float maxSpeed = 5f;
}

public class Boid : MonoBehaviour
{
    [SerializeField]
    BoidParam param;

    public Vector3 pos;
    public Vector3 velocity;
    public Vector3 accel = Vector3.zero;
    public List<Boid> neighbors = new List<Boid>();

    void Start()
    {
        pos = transform.position;
        velocity = transform.forward * param.initSpeed;
    }

    void Update()
    {
        UpdateMove();
    }

    void UpdateMove()
    {
        var dt = Time.deltaTime;

        velocity += accel * dt;
        var dir = velocity.normalized;
        var speed = velocity.magnitude;
        velocity = Mathf.Clamp(speed, param.minSpeed, param.maxSpeed) * dir;
        pos += velocity * dt;

        var rot = Quaternion.LookRotation(velocity);
        transform.SetPositionAndRotation(pos, rot);

        accel = Vector3.zero;
    }
}
