using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTreeMeshRender : MonoBehaviour {

    [Range(0,1)]
    public float metallic = 0;
    [Range(0, 1)]
    public float smoothness = 0;

    public Material mat;

    private ProceduralTree tree;
    private Mesh mesh;
    private Material mat_;

    void InitializeMesh()
    {
        Bounds bounds = new Bounds(transform.position, new Vector3(5, 1000, 5));

        Vector3[] vertices = new Vector3[tree.nodeMax];
        int[] indices = new int[tree.nodeMax];
        for(int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.bounds = bounds;
        mat_ = new Material(mat);
    }

    // Use this for initialization
    void Start () {
        tree = GetComponent<ProceduralTree>();

        InitializeMesh();
    }

    private void LateUpdate()
    {
        mat_.SetPass(0);
        mat_.SetBuffer("_TreeBuffer", tree.TreeDataBuffer);
        mat_.SetBuffer("_IndexBuffer", tree.TreeActiveIndexBuffer);
        mat_.SetInt("_TreeCount", tree.TreeDataIndex);
        mat_.SetInt("_IndexCount", tree.TreeActiveIndex);
        mat_.SetFloat("_Metallic", metallic);
        mat_.SetFloat("_Glossiness", smoothness);

        Graphics.DrawMesh(mesh, Matrix4x4.identity, mat_, 0);
    }
}
