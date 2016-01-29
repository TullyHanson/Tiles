using UnityEngine;
using System.Collections;

public class BoxController : MonoBehaviour {

    public Camera cam;
    public Rigidbody2D rb;

    // Use this for initialization
    void Start () {
        if (cam == null)
            cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
    }


    // Update is called once per physics timestep
    void FixedUpdate () {
        Vector3 rawPosition = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 targetPosition = new Vector3(rawPosition.x, rawPosition.y, 0.0f);
        //rb.MovePosition(targetPosition);


        //for (var i = 0; i < Input.touchCount; ++i)
        //{
        //    Touch touch = Input.GetTouch(i);
        //    if (touch.phase == TouchPhase.Began)
        //    {
        //        if (Mathf.Abs(touch.position.y - transform.position.y) <= 0.5)
        //        {
        //            if (Mathf.Abs(touch.position.x - transform.position.x) <= 0.5)
        //            {
        //                Object.Destroy(this.gameObject);
        //            }
        //        }
        //    }
        //}


        if (Mathf.Abs(rawPosition.y - transform.position.y) <= 0.5)
        {
            if (Mathf.Abs(rawPosition.x - transform.position.x) <= 0.5)
            {
                Object.Destroy(this.gameObject);
            }
        }


    }
}
