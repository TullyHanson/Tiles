using UnityEngine;
using System.Collections;

public class WorldSpawn : MonoBehaviour
{

    public GameObject block1;

    public int worldWidth = 4;
    public int worldHeight = 10;

    public float spawnSpeed = 3.0f;

    void Start()
    {
        StartCoroutine(CreateWorld());
    }

    IEnumerator CreateWorld()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            yield return new WaitForSeconds(spawnSpeed);

            for (int y = 0; y < worldHeight; y++)
            {
                yield return new WaitForSeconds(spawnSpeed);

                GameObject block = Instantiate(block1, Vector3.zero, block1.transform.rotation) as GameObject;
                block.transform.parent = transform;
                block.transform.localPosition = new Vector3(x, y, 0);
            }
        }
    }
}
