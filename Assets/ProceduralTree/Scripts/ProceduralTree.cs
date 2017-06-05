using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNodeClass
{
    public const int NODE_NEXT_MAX = 4;

    public int index = 0;
    public Vector3 startPosition = Vector3.zero;
    public Vector3 position = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    public float speed = 0;
    public float lifeTime = 0;
    public float duration = 0;
    public float radius = 0;
    public float startRadius = 0;
    public float targetRadius = 0;
    public float delay = 0;
    public float deleteTime = 0;

    public TreeNodeClass back = null;
    public List<TreeNodeClass> nextList = new List<TreeNodeClass>(NODE_NEXT_MAX);
    public bool isMainNode = false;
    public int divideCount = 0;
    public float growthLength = 0;  // 伸びた距離
    public float startLength = 0;

    public bool AddNext(TreeNodeClass nextNode)
    {
        if (nextList.Count >= NODE_NEXT_MAX)
        {
            Debug.LogWarning("MAX NEXT NODE");
            return false;
        }

        // 追加したら成長止まる
        speed = 0;
        //nextNode.position = nextNode.startPosition = this.position;
        nextNode.back = this;
        nextList.Add(nextNode);

        return true;
    }

    public bool Update(float dt)
    {
        if(delay > 0)
        {
            delay -= dt;
            return true;
        }

        if (duration > 0) {
            duration = Mathf.Max(duration - dt, 0);
            radius = Mathf.Lerp(startRadius, targetRadius, 1f - duration / lifeTime);

            position += rotation * Vector3.forward * speed * dt;
            growthLength += speed * dt * (1f / radius);

            if (duration <= 0f)
            {
                // 伸び切った
                speed = 0;
                return false;
             }
        }

        return true;
    }

    public bool IsDelete(float dt)
    {
        deleteTime -= dt;
        if(deleteTime <=0)
        {
            return true;
        }
        return false;
    }
}

public class ProceduralTree : MonoBehaviour {

    #region define
    public struct TreeData
    {
        public Vector3 position;        // 先端の座標
        public Vector3 startPosition;   // 根本の座標
        public int backID;       // 前のNodeのindex
        public int nextID;       // 次のnodeのindex
        public float radius;
        public float startRadius;
        public float growthLength;  // 伸びた距離(累計)
        public float startLength;
    }
    #endregion

    #region public
    public int nodeMax = 64;
    public float growthSpeed = 1f;
    public float lifeTime = 5;
    public float divisionPer = 0.1f;
    public int maxDivideCount = 3;
    public float defaultRadius = 1;

    public Vector2 growthSpeedRandomRange = new Vector2(-0.5f, 0.5f);
    public int mainBranchAdjustIntervalCount = 3;                   // メイン節の分裂時の角度補正間隔
    public Vector2 mainBranchRandomAngle = new Vector2(-45, -90);   // メイン節の分裂時のランダム回転時の角度範囲

    public bool isDrawGizmos = false;
    public float branchDownScale = 0.75f;   // 枝分かれした時の縮小率

    //public Camera targetCamera;
    //public float range = 2;
    //public float rotSpeed = 10;
    //public float angle = 0;
    //public Vector3 offset = Vector3.zero;
    public float startHeight = 5;   // 開始時の高さ

    public float growthBranchDelay = 1f;

    public float defaultDeleteTime = 30;

    public ComputeBuffer TreeDataBuffer { get { return treeBuffer; } }
    public ComputeBuffer TreeActiveIndexBuffer { get { return treeActiveIndexBuffer; } }
    public int TreeDataIndex { get { return treeDataIndex; } }
    public int TreeActiveIndex { get { return treeActiveIndex; } }
    public TreeNodeClass SelectNode { get { return selectNode; } }
    #endregion

    #region private
    private TreeNodeClass[] nodeArray = null;
    private Stack<TreeNodeClass> poolList = new Stack<TreeNodeClass>();
    private List<TreeNodeClass> activeList = new List<TreeNodeClass>();
    private List<TreeNodeClass> deleteList = new List<TreeNodeClass>(); // 削除対象ツリー
    private TreeNodeClass selectNode = null;

    private List<TreeNodeClass> endList = new List<TreeNodeClass>();    // 成長が止まったツリー

    private Vector3 frontPosition = Vector3.zero;

    private int mainDivideCount = 0;    // メイン幹分裂回数

    private ComputeBuffer treeBuffer;
    private ComputeBuffer treeActiveIndexBuffer;
    private TreeData[] treeDataArray;
    private int[] treeActiveIndexArray;
    private int treeDataIndex;
    private int treeActiveIndex;
    #endregion

    void Initialize()
    {
        nodeArray = new TreeNodeClass[nodeMax];
        for(int i = 0; i < nodeMax; i++)
        {
            nodeArray[i] = new TreeNodeClass();
            nodeArray[i].index = i;
            nodeArray[i].nextList.Clear();

            poolList.Push(nodeArray[i]);
        }

        treeBuffer = new ComputeBuffer(nodeMax, System.Runtime.InteropServices.Marshal.SizeOf(typeof(TreeData)));
        treeActiveIndexBuffer = new ComputeBuffer(nodeMax, System.Runtime.InteropServices.Marshal.SizeOf(typeof(int)));
        treeDataArray = new TreeData[nodeMax];
        treeActiveIndexArray = new int[nodeMax];
        treeDataIndex = 0;
        treeActiveIndex = 0;
    }

    float GetGrowthSpeed()
    {
        return growthSpeed + Random.Range(growthSpeedRandomRange.x, growthSpeedRandomRange.y);
    }

    TreeNodeClass CreateNode(Vector3 pos, Quaternion rotation, float speed, float lt, int divCount, float startRadius, float targetRadius, TreeNodeClass parent=null, float delay = 0, float deleteTime=30)
    {
        TreeNodeClass node = poolList.Pop();
        if (node != null)
        {
            node.nextList.Clear();
            node.back = parent;
            node.startPosition = pos;
            node.position = pos;
            node.rotation = rotation;
            node.speed = speed;
            node.lifeTime = node.duration = lt;
            node.divideCount = divCount;
            node.radius = node.startRadius = startRadius;
            node.targetRadius = targetRadius;
            node.startLength = (parent != null) ? parent.startLength + parent.growthLength : 0;
            node.growthLength = 0;
            node.delay = delay;
            node.deleteTime = deleteTime;
            node.isMainNode = false;
            //selectNode = node;
            activeList.Add(node);
        }
        return node;
    }
    
    void AddTreeData(TreeNodeClass node)
    {
        if (treeDataIndex < nodeMax)
        {
            //treeDataArray[treeDataIndex].forwardID = node.index;
            //treeDataArray[treeDataIndex].backID= (node.back != null) ? node.back.index: -1;
            treeDataArray[treeDataIndex].position = node.position;    // 先端の座標
            treeDataArray[treeDataIndex].startPosition = node.startPosition;    // 根本の座標
            treeDataArray[treeDataIndex].backID = (node.back != null) ? node.back.index : -1;
            treeDataArray[treeDataIndex].nextID = (node.nextList.Count > 0) ? node.nextList[0].index : -1;
            //Debug.Log("AddTreeData[" + treeDataIndex + "] index " + node.index + " nextList.Count " + node.nextList.Count);
            //Debug.Log("AddTreeData[" + treeDataIndex + "] index " + node.index + " backID: " + treeDataArray[treeDataIndex].backID + " nextID: " + treeDataArray[treeDataIndex].nextID + " position " + node.position);
            treeDataArray[treeDataIndex].radius = node.radius;
            treeDataArray[treeDataIndex].startRadius = node.startRadius;
            treeDataArray[treeDataIndex].growthLength = node.growthLength;
            treeDataArray[treeDataIndex].startLength = node.startLength;
            treeDataIndex++;
        }
    }

    void AddActiveIndex(int index)
    {
        if(treeActiveIndex < nodeMax)
        {
            treeActiveIndexArray[treeActiveIndex] = index;
            //Debug.Log("AddActiveIndex[" + treeActiveIndex + "] " + index);
            treeActiveIndex++;
        }
    }

    void UpdateNode()
    {
        float dt = Time.deltaTime;

        endList.Clear();
        deleteList.Clear();

        treeDataIndex = 0;
        treeActiveIndex = 0;

        // 更新
        for (int i = 0; i < activeList.Count; i++)
        {
            if (!activeList[i].Update(dt))
            {
                if (activeList[i].divideCount > 0)
                {
                    endList.Add(activeList[i]);
                }
            }
            if (activeList[i].IsDelete(dt)){
                // 時間切れなので削除
                deleteList.Add(activeList[i]);
            } else {
                //AddTreeData(activeList[i]);
                AddActiveIndex(activeList[i].index);
            }
        }

        for(int i = 0; i < nodeMax; i++)
        {
            AddTreeData(nodeArray[i]);
        }

        treeBuffer.SetData(treeDataArray);
        treeActiveIndexBuffer.SetData(treeActiveIndexArray);

        // 分裂するかチェック
        for(int i = 0; i < endList.Count; i++)
        {
            if((endList[i].isMainNode) || (divisionPer > Random.value))
            {
                // 分裂します
                Quaternion q;
                //float branchGrowSpeedPower = (float)(endList[i].divideCount - 1) / maxDivideCount;
                float branchGrowSpeedPower = 1;
                float radius = endList[i].radius * branchDownScale;

                // メイン幹分裂
                if (endList[i].isMainNode)
                {
                    int num = 3;
                    float ang = Random.Range(0f, 180f);

                    // 脇道分裂
                    for (int j = 0; j < num; j++)
                    {
                        if (divisionPer > Random.value)
                        {

                            //AddNode(endList[i], endList[i].rotation * Quaternion.AngleAxis(ang, Vector3.forward) * Quaternion.AngleAxis(Random.Range(-90, -45), Vector3.left), GetGrowthSpeed(), lifeTime, endList[i].divideCount - 1, defaultRadius);
                            CreateNode(endList[i].position, endList[i].rotation * Quaternion.AngleAxis(ang, Vector3.forward) * Quaternion.AngleAxis(Random.Range(-90, -45), Vector3.left), GetGrowthSpeed(), lifeTime, endList[i].divideCount - 1, radius, radius * 0.75f, null, growthBranchDelay, defaultDeleteTime);
                            ang += 120f + Random.Range(-10, 10);
                        }
                    }

                    // メイン幹の角度
                    if (mainDivideCount % mainBranchAdjustIntervalCount > 0)
                    {
                        // ランダム
                        //Debug.Log("ランダム");
                        q = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Quaternion.Euler(Random.Range(mainBranchRandomAngle.x, mainBranchRandomAngle.y), 0, 0);
                    }
                    else
                    {
                        // 補正
                        //Debug.Log("補正");
                        Vector3 center = this.transform.position;
                        Vector3 target = new Vector3(center.x, endList[i].position.y + 2, center.z);    // 少し上の方を目標に
                        q = Quaternion.LookRotation(target - endList[i].position);    // 中心への向き

                    }
                    mainDivideCount++;
                }
                else
                {
                    // 脇道分裂
                    int num = 2;
                    float minus = 1;

                    for (int j = 0; j < num; j++)
                    {
                        if (divisionPer > Random.value)
                        {
                            CreateNode(endList[i].position, endList[i].rotation * Quaternion.AngleAxis(Random.Range(45, 60) * minus, Vector3.up), GetGrowthSpeed() * branchGrowSpeedPower, lifeTime, endList[i].divideCount - 1, endList[i].radius, radius, null, growthBranchDelay, defaultDeleteTime);
                            minus *= -1;
                        }
                    }

                    // ランダム
                    q = endList[i].rotation * Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Quaternion.AngleAxis(Random.Range(-20, 20), Vector3.left);
                }

                // メイン幹
                float startRadius = (endList[i].isMainNode) ? defaultRadius : endList[i].radius;
                float targetRadius = (endList[i].isMainNode) ? defaultRadius : startRadius * 0.5f;

                float growSpeed = GetGrowthSpeed();
                if (!endList[i].isMainNode)
                {
                    growSpeed *= branchGrowSpeedPower;
                }

                TreeNodeClass node = CreateNode(endList[i].position, q, growSpeed, lifeTime, (endList[i].isMainNode) ? maxDivideCount : endList[i].divideCount - 1, startRadius, targetRadius, endList[i], 0, defaultDeleteTime);
                node.isMainNode = endList[i].isMainNode;
                endList[i].AddNext(node);
                if (node.isMainNode)
                {
                    selectNode = node;
                }
            }
            //activeList.Remove(endList[i]);
        }

        // 削除
        for (int i = 0; i < deleteList.Count; i++)
        {
            poolList.Push(deleteList[i]);
            activeList.Remove(deleteList[i]);
        }
    }

    // Use this for initialization
    void Start () {
        Initialize();

        TreeNodeClass node1 = CreateNode(transform.position, Quaternion.Euler(-90,0,0), 0, 0, maxDivideCount, defaultRadius, defaultRadius,null, 0, defaultDeleteTime);
        node1.isMainNode = true; // メイン幹
        TreeNodeClass node2 = CreateNode(node1.position, Quaternion.Euler(-90, 0, 0), GetGrowthSpeed(), lifeTime, maxDivideCount, defaultRadius, defaultRadius, null, 0, defaultDeleteTime);
        //node = AddNode(node, Quaternion.Euler(-90, 0, 0), GetGrowthSpeed(), lifeTime, maxDivideCount, defaultRadius);
        node2.isMainNode = true; // メイン幹
        node1.AddNext(node2);
        node2.position += Vector3.up * startHeight;
        node2.growthLength += startHeight;
        selectNode = node2;
    }

    // Update is called once per frame
    void Update () {
        
        UpdateNode();

        //if(selectNode != null)
        //{
        //    Vector3 pos = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * range;
        //    pos.y += selectNode.position.y;
        //    angle += rotSpeed * Time.deltaTime;
        //    //targetCamera.transform.position = selectNode.position + Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * range;
        //    targetCamera.transform.position = pos;
        //    targetCamera.transform.LookAt(selectNode.position, Vector3.up);
        //    pos += offset;
        //    if (pos.y < 0.5f) pos.y = 0.5f;
        //    targetCamera.transform.position = pos;

        //}
    }

    private void OnDrawGizmos()
    {
        if (!isDrawGizmos) return;

        for(int i = 0; i < activeList.Count; i++)
        {
            if (activeList[i].isMainNode)
            {
                Gizmos.color = Color.white;
            }else
            {
                //Gizmos.color = Color.yellow;
                Gizmos.color = Color.HSVToRGB((float)activeList[i].divideCount / maxDivideCount, 1, 1);
            }
            Gizmos.DrawSphere(activeList[i].position, 0.1f);
            if(activeList[i].back != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(activeList[i].position, activeList[i].back.position);
//#if UNITY_EDITOR
//                UnityEditor.Handles.color = Color.white;
//                for (int j = 0; j < 5; j++)
//                {
//                    UnityEditor.Handles.DrawWireDisc(Vector3.Lerp(activeList[i].back.position, activeList[i].position, (float)j / 4f), activeList[i].rotation * Vector3.forward, activeList[i].radius);
//                }
//#endif
            }
        }
    }

    private void OnDestroy()
    {
        if(treeBuffer != null)
        {
            treeBuffer.Release();
            treeBuffer = null;
        }
        if(treeActiveIndexBuffer != null)
        {
            treeActiveIndexBuffer.Release();
            treeActiveIndexBuffer = null;
        }
    }
}
