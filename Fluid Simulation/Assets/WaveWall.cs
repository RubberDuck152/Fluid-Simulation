using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveWall : MonoBehaviour
{
    public GameObject Position1;
    public GameObject Position2;
    public GameObject Wall;
    public float MoveSpeed;
    public bool active = true;

    private void Update()
    {
        if (active == true)
        {
            StopCoroutine(MoveToSea(Position1.transform.position));
            StartCoroutine(MoveToShore(Position2.transform.position));
        }

        if (active == false)
        {
            StopCoroutine(MoveToShore(Position2.transform.position));
            StartCoroutine(MoveToSea(Position1.transform.position));
        }
    }

    IEnumerator MoveToShore(Vector3 goalPos)
    {
        float dist = Vector3.Distance(Wall.transform.position, goalPos);

        if (dist > 0.0001f)
        {
            Wall.transform.position = Vector3.Lerp(Wall.transform.position, goalPos, MoveSpeed * Time.deltaTime);
        }

        yield return new WaitForSeconds(2);

        active = !active;
        StopCoroutine(MoveToShore(Position2.transform.position));
    }

    IEnumerator MoveToSea(Vector3 goalPos)
    {
        float dist = Vector3.Distance(Wall.transform.position, goalPos);

        if (dist > 0.0001f)
        {
            Wall.transform.position = Vector3.Lerp(Wall.transform.position, goalPos, MoveSpeed * Time.deltaTime);
        }

        yield return new WaitForSeconds(2);

        active = !active;
        StopCoroutine(MoveToSea(Position1.transform.position));
    }
}
