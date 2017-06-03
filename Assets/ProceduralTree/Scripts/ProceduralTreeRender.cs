using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTreeRender : MonoBehaviour {

    public const int MESH_VERTEX_MAX = 65534;

    public Material mat;
    //[Range(3,16)]
    //public int divideCount = 4; // 面の分割数

    private ProceduralTree tree;

	// Use this for initialization
	void Start () {
        tree = GetComponent<ProceduralTree>();
    }

    void OnRenderObject()
    {
        mat.SetPass(0);
        mat.SetBuffer("_TreeBuffer", tree.TreeDataBuffer);
        mat.SetBuffer("_IndexBuffer", tree.TreeActiveIndexBuffer);
        mat.SetInt("_TreeCount", tree.TreeDataIndex);
        mat.SetInt("_IndexCount", tree.TreeActiveIndex);
        //mat.SetInt("_DivideCount", divideCount);

        Graphics.DrawProcedural(MeshTopology.Points, tree.TreeDataIndex, 0);
    }
}
