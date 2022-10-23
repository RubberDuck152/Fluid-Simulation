using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public float Radius;
    public float Mass;
    public float RestDensity;
    public float Viscosity;
    public float Drag;

    private float SmoothingRadius = 1.0f;
    private float GravityMultiplier = 2000.0f;
    private float Gas = 2000.0f;
    private float DeltaTime = 0.0008f;
    private float Damping = -0.5f;

    public int NumberOfParticles;
    public int NumberPerRow;

    public bool WallsUp;

    public List<GameObject> Walls = new List<GameObject>();
    public GameObject Prefab;

    private Vector3 Gravity = new Vector3(0.0f, -9.81f, 0.0f);

    private SPHV2[] Particles;
    private Collider[] Colliders;
    private bool Clearing;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        InitFluid();
        InitForces();
        InitMovement();
        InitCollisions();
    }

    private void InitFluid()
    {
        Particles = new SPHV2[NumberOfParticles];

        for (int i = 0; i < NumberOfParticles; i++)
        {
            float x = (i % NumberPerRow) + Random.Range(-0.1f, 0.1f);
            float y = (2 * Radius) + (float)((i / NumberPerRow) / NumberPerRow) * 1.1f;
            float z = ((i / NumberPerRow) % NumberPerRow) + Random.Range(-0.1f, 0.1f);

            GameObject ActiveParticle = Instantiate(Prefab);
            SPHV2 CurrentParticle = ActiveParticle.AddComponent<SPHV2>();
            Particles[i] = CurrentParticle;

            ActiveParticle.transform.localScale = Vector3.one * Radius;
            ActiveParticle.transform.position = new Vector3(x, y, z);

            CurrentParticle.Particle = ActiveParticle;
            CurrentParticle.Position = ActiveParticle.transform.position;
        }
    }

    private void InitForces()
    {
        for(int i = 0; i < Particles.Length; i++)
        {
            if (Clearing)
            {
                return;
            }

            for (int j = 0; j < Particles.Length; j++)
            {
                Vector3 Direction = Particles[j].Position - Particles[i].Position;
                float Distance = Direction.magnitude;

                Particles[i].Density = InitDensity(Particles[i], Distance);
                Particles[i].Pressure = Gas * (Particles[i].Density - RestDensity);
            }
        }
    }


    private float InitDensity(SPHV2 CurrentParticle, float Distance)
    {
        if (Distance < SmoothingRadius)
        {
            return CurrentParticle.Density += Mass * (315.0f / (64.0f * Mathf.PI * Mathf.Pow(SmoothingRadius, 9.0f))) * Mathf.Pow(SmoothingRadius - Distance, 3.0f);
        }

        return CurrentParticle.Density;
    }

    private void InitMovement()
    {
        for (int i = 0; i < Particles.Length; i++)
        {
            if (Clearing)
            {
                return;
            }

            Vector3 ForcePressure = Vector3.zero;
            Vector3 ForceViscosity = Vector3.zero;

            for (int j = 0; j < Particles.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Vector3 Direction = Particles[j].Position - Particles[i].Position;
                float Distance = Direction.magnitude;

                ForcePressure += InitPressure(Particles[i], Particles[j], Direction, Distance);
                ForceViscosity += InitViscosity(Particles[i], Particles[j], Distance);
            }

            Vector3 ForceGravity = Gravity * Particles[i].Density * GravityMultiplier;

            Particles[i].Force = ForcePressure + ForceViscosity + ForceGravity;
            Particles[i].Velocity += DeltaTime * (Particles[i].Force) / Particles[i].Density;
            Particles[i].Position += DeltaTime * (Particles[i].Velocity);
            Particles[i].Particle.transform.position = Particles[i].Position;
        }
    }

    private Vector3 InitPressure(SPHV2 CurrentParticle, SPHV2 NextParticle, Vector3 Direction, float Distance)
    {
        if (Distance < SmoothingRadius)
        {
            return -1 * (Direction.normalized) * Mass * (CurrentParticle.Pressure + NextParticle.Pressure) / (2.0f * NextParticle.Density) *
                (-45/0f / (Mathf.PI * Mathf.Pow(SmoothingRadius, 6.0f))) * Mathf.Pow(SmoothingRadius - Distance, 2.0f);
        }

        return Vector3.zero;
    }

    private Vector3 InitViscosity(SPHV2 CurrentParticle, SPHV2 NextParticle, float Distance)
    {
        if (Distance < SmoothingRadius)
        {
            return Viscosity * Mass * (NextParticle.Velocity - CurrentParticle.Velocity) / NextParticle.Density * 
                (45.0f / (Mathf.PI * Mathf.Pow(SmoothingRadius, 6.0f))) * (SmoothingRadius - Distance);
        }

        return Vector3.zero;
    }

    private void InitCollisions()
    {
        for (int i = 0; i < Particles.Length; i++)
        {
            for (int j = 0; j < Colliders.Length; j++)
            {
                if (Clearing || Colliders.Length == 0)
                {
                    return;
                }

                Vector3 PenetrationNormal;
                Vector3 PenetrationPosition;
                float PenetrationLength;

                if (CollsionOccurred(Colliders[j], Particles[i].Position, Radius, out PenetrationNormal, out PenetrationPosition, out PenetrationLength))
                {
                    Particles[i].Velocity = AlterVelocity(Colliders[j], Particles[i].Velocity, PenetrationNormal, 1.0f - Drag);
                    Particles[i].Position = PenetrationPosition - PenetrationNormal * Mathf.Abs(PenetrationLength);
                }
            }
        }
    }

    private static bool CollsionOccurred(Collider collider, Vector3 Position, float radius, out Vector3 PenetrationNormal, out Vector3 PenetrationPosition, out float PenetrationLength)
    {
        Vector3 ColliderProjection = collider.Position - Position;

        PenetrationNormal = Vector3.Cross(collider.Right, collider.Up);
        PenetrationLength = Mathf.Abs(Vector3.Dot(ColliderProjection, PenetrationNormal)) - (radius / 2.0f);
        PenetrationPosition = collider.Position - ColliderProjection;

        return PenetrationLength < 0.0f && 
            Mathf.Abs(Vector3.Dot(ColliderProjection, collider.Right)) < collider.Scale.x && 
            Mathf.Abs(Vector3.Dot(ColliderProjection, collider.Up)) < collider.Scale.y;
    }

    private Vector3 AlterVelocity(Collider collider, Vector3 Velocity, Vector3 PentrationNormal, float drag)
    {
        Vector3 NewVelocity = Vector3.Dot(Velocity, PentrationNormal) * PentrationNormal * Damping + Vector3.Dot(Velocity, collider.Right) * collider.Right * drag + 
            Vector3.Dot(Velocity, collider.Up) * collider.Up * drag;
        return Vector3.Dot(NewVelocity, Vector3.forward) * Vector3.forward + Vector3.Dot(NewVelocity, Vector3.right) * Vector3.right + Vector3.Dot(NewVelocity, Vector3.up) * Vector3.up;
    }
}
