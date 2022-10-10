using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDespawner : MonoBehaviour
{
    public int DespawnTime;
    void Update()
    {
        StartCoroutine(DespawnTimer());
    }

    IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(DespawnTime);
        Destroy(gameObject);
    }
}
