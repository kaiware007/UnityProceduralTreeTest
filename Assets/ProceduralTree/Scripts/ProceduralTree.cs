using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNodeClass
{
    public const int NODE_NEXT_MAX = 4;

    public int index = 0;
    public Vector3 position = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    public float speed = 0;
    public float lifeTime = 0;
    public float radius = 0;

    public TreeNodeClass back = null;
    public List<TreeNodeClass> nextList = new List<TreeNodeClass>(NODE_NEXT_MAX);
    public bool isMainNode = false;
    public int divideCount = 0;

    public bool AddNext(TreeNodeClass nextNode)
    {
        if (nextList.Count >= NODE_NEXT_MAX)
        {
            Debug.LogWarning("MAX NEXT NODE");
            return false;
        }

        // 追加したら成長止まる
        speed = 0;
        nextNode.position = this.position;
        nextNode.back = this;
        nextList.Add(nextNode);

        return true;
    }

    public bool Update(float dt)
    {
        position += rotation * Vector3.forward * speed * dt;
        if (lifeTime > 0) {
            lifeTime -= dt;
            if (lifeTime <= 0f)
            {
                // 伸び切った
                speed = 0;
                return false;
             }
        }

        return true;
    }
}

public class ProceduralTree : MonoBehaviour {

    #region define
    public struct TreeData
    {
        public Vector3 forward_position;    // 先端の座標
        public Vector3 back_position;       // 根本の座標
        public Vector3 next_position;       // 次のnodeの座標
        public float forward_radius;
        public float back_radius;
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

    public ComputeBuffer TreeDataBuffer { get { return treeBuffer; } }
    public int TreeDataIndex { get { return treeDataIndex; } }
    #endregion

    #region private
    private TreeNodeClass[] nodeArray = null;
    private Stack<TreeNodeClass> poolList = new Stack<TreeNodeClass>();
    private List<TreeNodeClass> activeList = new List<TreeNodeClass>();
    private TreeNodeClass selectNode = null;

    private List<TreeNodeClass> endList = new List<TreeNodeClass>();    // 成長が止まったツリー

    private Vector3 frontPosition = Vector3.zero;

    private int mainDivideCount = 0;    // メイン幹分裂回数

    private ComputeBuffer treeBuffer;
    private TreeData[] treeDataArray;
    private int treeDataIndex;
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
        treeDataArray = new TreeData[nodeMax];
        treeDataIndex = 0;
    }

    float GetGrowthSpeed()
    {
        return growthSpeed + Random.Range(growthSpeedRandomRange.x, growthSpeedRandomRange.y);
    }

    TreeNodeClass CreateNode(Vector3 pos, Quaternion rotation, float speed, float lt, float radius)
    {
        TreeNodeClass node = poolList.Pop();
        node.position = pos;
        node.rotation = rotation;
        node.speed = speed;
        node.lifeTime = lt;
        node.radius = radius;
        selectNode = node;
        activeList.Add(node);
        return node;
    }

    TreeNodeClass AddNodeSelect(Quaternion rotation, float speed, float lt)
    {
        if((selectNode != null)&&(poolList.Count > 0))
        {
            TreeNodeClass node = poolList.Pop();
            //node.position = pos;
            node.rotation = rotation;
            node.speed = speed;
            node.lifeTime = lt;

            if (!selectNode.AddNext(node))
            {
                // 追加できなかった場合はpoolに戻す
                poolList.Push(node);
                return null;
            }

            // 成功
            activeList.Add(node);
            return node;
        }

        return null;
    }

    TreeNodeClass AddNode(TreeNodeClass parent, Quaternion rotation, float speed, float lt, int divCount, float radius)
    {
        if ((selectNode != null) && (poolList.Count > 0))
        {
            TreeNodeClass node = poolList.Pop();
            //node.position = pos;
            node.rotation = rotation;
            node.speed = speed;
            node.lifeTime = lt;
            node.divideCount = divCount;
            node.radius = radius;

            if (!parent.AddNext(node))
            {
                // 追加できなかった場合はpoolに戻す
                poolList.Push(node);
                return null;
            }

            // 成功
            activeList.Add(node);
            return node;
        }

        return null;

    }

    void AddTreeData(TreeNodeClass node)
    {
        if (treeDataIndex < nodeMax)
        {
            //treeDataArray[treeDataIndex].forwardID = node.index;
            //treeDataArray[treeDataIndex].backID= (node.back != null) ? node.back.index: -1;
            treeDataArray[treeDataIndex].forward_position = node.position;    // 先端の座標
            treeDataArray[treeDataIndex].back_position = (node.back != null) ? node.back.position : Vector3.zero;       // 根本の座標
            treeDataArray[treeDataIndex].next_position = (node.nextList.Count > 0) ? node.nextList[0].position : Vector3.zero;       // 次のnodeの座標
            treeDataArray[treeDataIndex].forward_radius = node.radius;
            treeDataArray[treeDataIndex].back_radius = (node.back != null) ? node.back.radius : 0;
            treeDataIndex++;
        }
    }

    void UpdateNode()
    {
        float dt = Time.deltaTime;

        endList.Clear();

        treeDataIndex = 0;

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

            AddTreeData(activeList[i]);
        }

        treeBuffer.SetData(treeDataArray);

        // 分裂するかチェック
        for(int i = 0; i < endList.Count; i++)
        {
            if((endList[i].isMainNode)||(divisionPer >= Random.value))
            {
                // 分裂します
                Quaternion q;

                // メイン幹分裂
                if (endList[i].isMainNode)
                {
                    int num = Random.Range(1, 4);
                    float ang = Random.Range(0f, 180f);

                    // 脇道分裂
                    for (int j = 0; j < num; j++)
                    {
                        AddNode(endList[i], endList[i].rotation * Quaternion.AngleAxis(ang, Vector3.forward) * Quaternion.AngleAxis(Random.Range(-90, -45), Vector3.left), GetGrowthSpeed(), lifeTime, endList[i].divideCount - 1, defaultRadius);
                        ang += 120f + Random.Range(-10, 10);
                    }

                    if (mainDivideCount % mainBranchAdjustIntervalCount > 0)
                    {
                        // ランダム
                        Debug.Log("ランダム");
                        q = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Quaternion.Euler(Random.Range(mainBranchRandomAngle.x, mainBranchRandomAngle.y), 0, 0);
                    }
                    else
                    {
                        // 補正
                        Debug.Log("補正");
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
                        AddNode(endList[i], endList[i].rotation * Quaternion.AngleAxis(Random.Range(45, 60) * minus, Vector3.up), GetGrowthSpeed() / 2f, lifeTime, endList[i].divideCount - 1, defaultRadius);
                        minus *= -1;
                    }

                    // ランダム
                    q = endList[i].rotation * Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Quaternion.AngleAxis(Random.Range(-20, 20), Vector3.left);
                }

                selectNode = AddNode(endList[i], q, GetGrowthSpeed(), lifeTime, (endList[i].isMainNode) ? maxDivideCount : endList[i].divideCount - 1, defaultRadius);
                selectNode.isMainNode = endList[i].isMainNode;
            }
            //activeList.Remove(endList[i]);
        }
    }

	// Use this for initialization
	void Start () {
        Initialize();

        TreeNodeClass node = CreateNode(frontPosition, Quaternion.Euler(-90,0,0), 0, 0, defaultRadius);
        node.isMainNode = true; // メイン幹
        node = AddNode(node, Quaternion.Euler(-90, 0, 0), GetGrowthSpeed(), lifeTime, maxDivideCount, defaultRadius);
        node.isMainNode = true; // メイン幹
    }
	
	// Update is called once per frame
	void Update () {
        //// 上に伸びる
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    Quaternion q = selectNode.rotation * Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Quaternion.AngleAxis(Random.Range(-20, 20), Vector3.left);
        //    //Quaternion q = selectNode.rotation * Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Quaternion.AngleAxis(Random.Range(-1, 1), Vector3.left);
        //    //Quaternion q = selectNode.rotation * Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Quaternion.AngleAxis(70, Vector3.left);
        //    //Quaternion q = selectNode.rotation * Quaternion.AngleAxis(5, Vector3.right);
        //    selectNode = AddNodeSelect(q, GetGrowthSpeed(), lifeTime);
        //    //selectNode = AddNodeSelect(selectNode.rotation * q, growthSpeed, lifeTime);
        //    //selectNode = AddNodeSelect(Random.rotationUniform * selectNode.rotation, growthSpeed, lifeTime);
        //    //frontPosition += Random.onUnitSphere * 0.5f;
        //    //selectNode = AddNodeSelect(frontPosition);
        //}

        //// 分裂
        //if (Input.GetKeyDown(KeyCode.S))
        //{
        //    //TreeNodeClass next;
        //    //Quaternion q = Random.rotationUniform;
        //    //Quaternion q = Quaternion.Euler(-90, 0, 0);
        //    for (int i = 0; i < 3; i++)
        //    {
        //        AddNodeSelect(selectNode.rotation * Quaternion.AngleAxis((float)(i+1) * 120f, Vector3.forward) * Quaternion.AngleAxis(Random.Range(-90, -45), Vector3.left), growthSpeed, lifeTime);
        //    }
        //    Quaternion q = selectNode.rotation * Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Quaternion.AngleAxis(Random.Range(-20, 20), Vector3.left);
        //    selectNode = AddNodeSelect(q, GetGrowthSpeed(), lifeTime);
        //}

        UpdateNode();
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
    }
}
