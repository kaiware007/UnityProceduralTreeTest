using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LTree
{
    private List<LTree> branches;
    private GameObject contents;
    private GameObject appearance;
    private LTree lParent;


    public float length_decay = 0.8f;
    public float radius_decay = 0.7f;
    public float angle_deviation = 0.3f;
    public float minimum_branches = 1;
    public float maximum_branches = 3;
    public float minimum_radius = 0.1f;

    void createChildren()
    {
        float new_radius = appearance.transform.localScale.x * radius_decay;
        float new_length = appearance.transform.localScale.y * length_decay;
        if (new_radius < minimum_radius) return;
        branches = new List<LTree>();

        GameObject progenitor = new GameObject();
        progenitor.name = "Root for children";
        progenitor.transform.parent = contents.transform;
        progenitor.transform.localPosition = new Vector3(0, 0, 0);
        progenitor.transform.localEulerAngles = new Vector3(0, 0, 0);
        Debug.Log("the offset is " + appearance.transform.localPosition.y);
        progenitor.transform.Translate(0, 2 * appearance.transform.localPosition.y, 0);
        int num_children = (int)(Random.value * (maximum_branches - minimum_branches) + minimum_branches);
        for (int i = 0; i < num_children; i++)
        {
            LTree child = new LTree();
            branches.Add(child);
            child.construct(progenitor, new_length, new_radius);
        }

    }

    public void pivot()
    {

        contents.transform.Rotate(0, .1f, 0);
    }

    public void do_rotate(float amt)
    {
        contents.transform.Rotate(0, 0, amt);
        if (branches == null) return;
        for (int i = 0; i < branches.Count; i++)
        {
            branches[i].do_rotate(amt);
        }
    }

    public void reset(float l, float r)
    {
        GameObject.Destroy(contents);
        branches = new List<LTree>();
        construct(null, l, r);
    }

    public void construct(GameObject parentTree, float length, float radius)
    {
        contents = new GameObject();
        appearance = GameObject.CreatePrimitive(LTreeController.limbType);
        if (parentTree != null) contents.transform.parent = parentTree.transform;
        contents.transform.localPosition = new Vector3(0, 0, 0);
        contents.transform.localEulerAngles = new Vector3(0, 0, 0);
        appearance.transform.parent = contents.transform;
        appearance.transform.localPosition = new Vector3(0, 0, 0);
        appearance.transform.localEulerAngles = new Vector3(0, 0, 0);
        contents.name = "Contents";
        contents.transform.Rotate(Random.value * 100 - 50, 0, Random.value * 100 - 50);
        appearance.name = "Appearance";
        Vector3 scaleVector = new Vector3(radius, length, radius);
        appearance.transform.localScale = scaleVector;
        appearance.transform.Translate(0, 0.5f * LTreeController.yScale * length, 0);
        createChildren();
    }


}


public class LTreeController : MonoBehaviour
{

    /*
	the LTree is an L-System-based tree object that creates a tree structure from simple rules.
	*/
    private int t = 0;
    public float initial_length = 1;
    public float initial_radius = 0.1f;
    private LTree rootNode;
    public static PrimitiveType limbType = PrimitiveType.Cube;
    public static float yScale = 1f;

    void Start()
    {
        rootNode = new LTree();
        rootNode.construct(null, initial_length, initial_radius);

    }

    void OnGUI()
    {
        t++;
        rootNode.do_rotate(0.1f * Mathf.Cos(0.03f * t));
        rootNode.pivot();
        if (GUI.Button(new Rect(10, 70, 100, 20), "reset"))
        {
            rootNode.reset(initial_length, initial_radius);
        }
        if (GUI.Button(new Rect(10, 90, 100, 20), "Cubes"))
        {
            limbType = PrimitiveType.Cube;
            yScale = 1;
            rootNode.reset(initial_length, initial_radius);
        }
        if (GUI.Button(new Rect(10, 110, 100, 20), "Cylinders"))
        {
            limbType = PrimitiveType.Cylinder;
            yScale = 2;
            rootNode.reset(initial_length, initial_radius);
        }
        if (GUI.Button(new Rect(10, 130, 100, 20), "Capsules"))
        {
            limbType = PrimitiveType.Capsule;
            yScale = 2;
            rootNode.reset(initial_length, initial_radius);
        }
        //------

        GUI.Label(new Rect(10, 150, 200, 20), "Start length:" + (Mathf.Round(initial_length * 100) / 100));

        if (GUI.Button(new Rect(10, 170, 50, 20), "-"))
        {
            initial_length *= 0.9f;
            rootNode.reset(initial_length, initial_radius);
        }
        if (GUI.Button(new Rect(60, 170, 50, 20), "+"))
        {
            initial_length /= 0.9f;
            rootNode.reset(initial_length, initial_radius);
        }
        //------

        GUI.Label(new Rect(10, 190, 200, 20), "Start radius:" + (Mathf.Round(initial_radius * 100) / 100));

        if (GUI.Button(new Rect(10, 210, 50, 20), "-"))
        {
            initial_radius *= 0.9f;
            rootNode.reset(initial_length, initial_radius);
        }
        if (GUI.Button(new Rect(60, 210, 50, 20), "+"))
        {
            initial_radius /= 0.9f;
            rootNode.reset(initial_length, initial_radius);
        }
    }


    // Update is called once per frame
    void Update()
    {

    }
}
