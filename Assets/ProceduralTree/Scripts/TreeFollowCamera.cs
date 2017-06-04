using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TreeFollowCamera : MonoBehaviour {

    public float range = 5;
    public float rotSpeed = 50;
    public Vector3 offset = Vector3.zero;

    public ProceduralTree tree;

    private float angle = 0;

    // Use this for initialization
    //   void Start () {

    //}

    // Update is called once per frame
    void Update () {

        if (tree.SelectNode != null)
        {
            Vector3 pos = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * range;
            pos.y += tree.SelectNode.position.y;
            angle += rotSpeed * Time.deltaTime;
            //targetCamera.transform.position = selectNode.position + Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * range;
            transform.position = pos;
            transform.LookAt(tree.SelectNode.position, Vector3.up);
            pos += offset;
            if (pos.y < 0.5f) pos.y = 0.5f;
            transform.position = pos;
        }
    }
}
