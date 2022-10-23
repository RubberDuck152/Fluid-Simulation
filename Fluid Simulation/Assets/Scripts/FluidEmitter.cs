using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidEmitter : MonoBehaviour
{
    public bool GenerateParticle;
    public float RespawnTime;
    public GameObject newFluidParticle;
    public GameObject FluidParticle;
    public int UpwardsForce;

    private void Start()
    {
        GenerateParticle = true;
    }

    void Update()
    {
        if (GenerateParticle == true)
        {
            InitParticle();
            GenerateParticle = false;
            StartCoroutine(Reload());
        }
    }

    void InitParticle()
    {
        FluidParticle = Instantiate(newFluidParticle);
        FluidParticle.transform.position = gameObject.transform.position;

        int RandomX = Random.Range(-90, 90);
        int RandomZ = Random.Range(-90, 90);

        FluidParticle.gameObject.GetComponent<Rigidbody>().AddForce(RandomX, UpwardsForce, RandomZ);
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(RespawnTime);
        GenerateParticle = true;
    }
}
