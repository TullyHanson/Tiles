using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour {

    public Camera cam;
    public GameObject box;
    public Rigidbody2D rb;

    private float maxWidth;

	// Use this for initialization
	void Start () {
        if (cam == null)
            cam = Camera.main;
        Vector3 upperCorner = new Vector3(Screen.width, Screen.height, 0.0f);
        Vector3 targetWidth = cam.ScreenToWorldPoint(upperCorner);
        float boxWidth = box.GetComponent<Renderer>().bounds.extents.x;
        maxWidth = targetWidth.x - boxWidth;
        StartCoroutine(Spawn());

    }

    IEnumerator Spawn() {

        float[] myArray = {-1.5f,-0.5f, 0.5f, 1.5f};

        yield return new WaitForSeconds(2.0f);
        while (true){
            float xPosition = myArray[Random.Range(0, 4)];
            Vector3 spawnPosition = new Vector3(Mathf.Clamp(xPosition,xPosition,xPosition), transform.position.y, 0.0f);
            Quaternion spawnRotation = Quaternion.identity;
            Instantiate(box, spawnPosition, spawnRotation);
            yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
        }
    }
}
