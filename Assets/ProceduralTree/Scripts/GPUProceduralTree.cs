using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GPUProceduralTree : MonoBehaviour {


    const int THREAD_NUM_X = 8;

    #region public
    public int nodeMax = 32;
    public ComputeShader cs;
    #endregion

    #region private
    private int nodeNum;
    private ComputeBuffer nodeBuffer;
    private ComputeBuffer nodeActiveBuffer;
    private ComputeBuffer nodePoolBuffer;
    private ComputeBuffer nodeActiveCountBuffer;  // nodeActiveBuffer
    private ComputeBuffer nodePoolCountBuffer;    // nodePoolBuffer
    protected int[] activeCounts = null;
    #endregion

    void Initialize()
    {
        nodeNum = Mathf.CeilToInt((float)nodeMax / THREAD_NUM_X) * THREAD_NUM_X;

        nodeBuffer = new ComputeBuffer(nodeNum, Marshal.SizeOf(typeof(TreeNode)), ComputeBufferType.Default);
        nodeActiveBuffer = new ComputeBuffer(nodeNum, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
        nodePoolBuffer = new ComputeBuffer(nodeNum, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
        nodeActiveCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
        nodePoolCountBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
        activeCounts = new int[] { 0, 1, 0, 0 };
        nodePoolCountBuffer.SetData(activeCounts);

    }

    void ReleaseBuffer()
    {
        new[] { nodeBuffer, nodeActiveBuffer, nodePoolBuffer, nodeActiveCountBuffer, nodePoolCountBuffer }.ToList().ForEach(i => {
            if (i != null)
            {
                i.Release();
                i = null;
            }
        });

        //if (nodeBuffer != null)
        //{
        //    nodeBuffer.Release();
        //}
    }

    // Use this for initialization
    void Start () {
        Initialize();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnDestroy()
    {
        ReleaseBuffer();
    }
}
