using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

namespace Boid.PureECS.Sample6
{

public class Bootstrap : MonoBehaviour 
{
    public static Bootstrap _Instance;

    public static Bootstrap Instance 
    { 
        get 
        { 
            return _Instance ?? (_Instance = FindObjectOfType<Bootstrap>());
        }
    }

    [SerializeField]
    Param param;

    public static Param Param
    {
        get { return Instance.param; }
    }

    [System.Serializable]
    public struct BoidInfo
    {
        public int count;
        public Vector3 scale;
        public Mesh mesh;
        public Material material;
    }

    [SerializeField]
    BoidInfo boidInfo = new BoidInfo 
    {
        count = 100,
        scale = new Vector3(0.1f, 0.1f, 0.3f),
    };

    public static BoidInfo Boid
    {
        get { return Instance.boidInfo; }
    }

    void OnDrawGizmos()
    {
        if (!param) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * param.wallScale);
    }
}

}