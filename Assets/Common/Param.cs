using UnityEngine;

namespace Boid
{

[CreateAssetMenu(menuName = "Boid1/Param")]
public class Param : ScriptableObject
{
    public float initSpeed = 2f;
    public float minSpeed = 2f;
    public float maxSpeed = 5f;
    public float neighborDistance = 1f;
    public float neighborFov = 90f;
    public float separationWeight = 5f;
    public float wallScale = 5f;
    public float wallDistance = 3f;
    public float wallWeight = 1f;
    public float alignmentWeight = 2f;
    public float cohesionWeight = 3f;
}

}
