using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPH : MonoBehaviour
{
    private struct SPHParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 forcePhysic;
        public Vector3 forceHeading;

        public float density;
        public float pressure;
        public int parameterID;

        public GameObject particle;

        public void Init(Vector3 init_position, int init_parameterID, GameObject new_particle)
        {
            position = init_position;
            parameterID = init_parameterID;
            particle = new_particle;

            velocity = Vector3.zero;
            forcePhysic = Vector3.zero;
            forceHeading = Vector3.zero;
            density = 0.0f;
            pressure = 0.0f;
        }
    }


    [System.Serializable]
    private struct SPHParameters
    {
#pragma warning disable 0649 
        public float particleRadius;
        public float smoothingRadius;
        public float smoothingRadiusSq;
        public float restDensity;
        public float gravityMult;
        public float particleMass;
        public float particleViscosity;
        public float particleDrag;
#pragma warning restore 0649
    }

    private struct SPHCollider
    {
        public Vector3 position;
        public Vector3 right;
        public Vector3 up;
        public Vector2 scale;

        public void Init(Transform init_transform)
        {
            position = init_transform.position;
            right = init_transform.right;
            up = init_transform.up;
            scale = new Vector2(init_transform.lossyScale.x / 2f, init_transform.lossyScale.y / 2f);
        }
    }

    [Header("Import")]
    [SerializeField] private GameObject character0Prefab = null;

    [Header("Parameters")]
    [SerializeField] private int parameterID = 0;
    [SerializeField] private SPHParameters[] parameters = null;

    [Header("Properties")]
    [SerializeField] private int amount = 250;
    [SerializeField] private int rowSize = 16;

    private static Vector3 GRAVITY = new Vector3(0.0f, -9.81f, 0.0f);
    private const float GAS_CONST = 2000.0f;
    private const float DT = 0.0008f;
    private const float BOUND_DAMPING = -0.5f;

    private SPHParticle[] particles;

    private void Start()
    {
        InitSPH();
    }

    private void Update()
    {
        DensityAndPressure();
        Forces();
        Integrate();
        Colliders();
        ApplyPosition();
    }

    private void InitSPH()
    {
        particles = new SPHParticle[amount];

        for (int i = 0; i < amount; i++)
        {
            float jitter = (Random.value * 2f - 1f) * parameters[parameterID].particleRadius * 0.1f;
            float x = (i % rowSize) + Random.Range(-0.1f, 0.1f);
            float y = 2 + (float)((i / rowSize) / rowSize) * 1.1f;
            float z = ((i / rowSize) % rowSize) + Random.Range(-0.1f, 0.1f);

            GameObject new_particle = Instantiate(character0Prefab);
            new_particle.transform.localScale = Vector3.one * parameters[parameterID].particleRadius;
            new_particle.transform.position = new Vector3(x + jitter, y, z + jitter);
            new_particle.name = "New_Water_Particle" + i.ToString();

            particles[i].Init(new Vector3(x, y, z), parameterID, new_particle);
        }
    }

    private static bool Intersect(SPHCollider collider, Vector3 position, float radius, out Vector3 penetrationNormal, out Vector3 penetrationPosition, out float penetrationLength)
    {
        Vector3 colliderProjection = collider.position - position;

        penetrationNormal = Vector3.Cross(collider.right, collider.up);
        penetrationLength = Mathf.Abs(Vector3.Dot(colliderProjection, penetrationNormal)) - (radius / 2.0f);
        penetrationPosition = collider.position - colliderProjection;

        return penetrationLength < 0.0f && Mathf.Abs(Vector3.Dot(colliderProjection, collider.right)) < collider.scale.x && Mathf.Abs(Vector3.Dot(colliderProjection, collider.up)) < collider.scale.y;
    }

    private static Vector3 DampVelocity(SPHCollider collider, Vector3 velocity, Vector3 penetrationNormal, float drag)
    {
        Vector3 newVelocity = Vector3.Dot(velocity, penetrationNormal) * penetrationNormal * BOUND_DAMPING
                            + Vector3.Dot(velocity, collider.right) * collider.right * drag
                            + Vector3.Dot(velocity, collider.up) * collider.up * drag;
        newVelocity = Vector3.Dot(newVelocity, Vector3.forward) * Vector3.forward
                    + Vector3.Dot(newVelocity, Vector3.right) * Vector3.right
                    + Vector3.Dot(newVelocity, Vector3.up) * Vector3.up;
        return newVelocity;
    }

    private void Colliders()
    {
        GameObject[] active_colliders = GameObject.FindGameObjectsWithTag("SPHCollider");
        SPHCollider[] colliders = new SPHCollider[active_colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].Init(active_colliders[i].transform);
        }

        for (int i = 0; i < particles.Length; i++)
        {
            for (int j = 0; j < colliders.Length; j++)
            {
                Vector3 penetrationNormal;
                Vector3 penetrationPosition;
                float penetrationLength;
                if (Intersect(colliders[j], particles[i].position, parameters[particles[i].parameterID].particleRadius, out penetrationNormal, out penetrationPosition, out penetrationLength))
                {
                    particles[i].velocity = DampVelocity(colliders[j], particles[i].velocity, penetrationNormal, 1.0f - parameters[particles[i].parameterID].particleDrag);
                    particles[i].position = penetrationPosition - penetrationNormal * Mathf.Abs(penetrationLength);
                }
            }
        }
    }

    private void Integrate()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].velocity += DT * (particles[i].forcePhysic) / particles[i].density;
            particles[i].position += DT * (particles[i].velocity);
        }
    }

    private void DensityAndPressure()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].density = 0.0f;

            for (int j = 0; j < particles.Length; j++)
            {
                Vector3 diff_i_j = particles[j].position - particles[i].position;
                float diff_i_j_squared = diff_i_j.sqrMagnitude;

                if (diff_i_j_squared < parameters[particles[i].parameterID].smoothingRadiusSq)
                {
                    particles[i].density += parameters[particles[i].parameterID].particleMass * (315.0f / (64.0f * Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius, 9.0f))) * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadiusSq - diff_i_j_squared, 3.0f);
                }
            }

            particles[i].pressure = GAS_CONST * (particles[i].density - parameters[particles[i].parameterID].restDensity);
        }
    }

    private void Forces()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 forcePressure = Vector3.zero;
            Vector3 forceViscosity = Vector3.zero;

            for (int j = 0; j < particles.Length; j++)
            {
                if (i == j) continue;

                Vector3 diff_i_j = particles[j].position - particles[i].position;
                float diff_i_j_squared = diff_i_j.sqrMagnitude;
                float r = Mathf.Sqrt(diff_i_j_squared);

                if (r < parameters[particles[i].parameterID].smoothingRadius)
                {
                    forcePressure += -diff_i_j.normalized * parameters[particles[i].parameterID].particleMass * (particles[i].pressure + particles[j].pressure) / (2.0f * particles[j].density) * (-45.0f / (Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius, 6.0f))) * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius - r, 2.0f);
                    forceViscosity += parameters[particles[i].parameterID].particleViscosity * parameters[particles[i].parameterID].particleMass * (particles[j].velocity - particles[i].velocity) / particles[j].density * (45.0f / (Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius, 6.0f))) * (parameters[particles[i].parameterID].smoothingRadius - r);
                }
            }
            Vector3 forceGravity = GRAVITY * particles[i].density * parameters[particles[i].parameterID].gravityMult;
            particles[i].forcePhysic = forcePressure + forceViscosity + forceGravity;
        }
    }



    private void ApplyPosition()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].particle.transform.position = particles[i].position;
        }
    }
}
