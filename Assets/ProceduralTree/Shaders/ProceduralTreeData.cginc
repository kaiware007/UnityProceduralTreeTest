#ifndef PROCEDURAL_TREE_DATA_INCLUDED
#define PROCEDURAL_TREE_DATA_INCLUDED

struct TreeData
{
	float3 position;		// 先端の座標
	float3 startPosition;	// 根本の座標
	int backID;			// 前のNodeのindex
	int nextID;			// 次のnodeのindex
	float radius;
	float growthLength;  // 伸びた距離(累計)
	float startLength;
};

#endif // PROCEDURAL_TREE_DATA_INCLUDED
