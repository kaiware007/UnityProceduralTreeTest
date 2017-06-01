#ifndef PROCEDURAL_TREE_DATA_INCLUDED
#define PROCEDURAL_TREE_DATA_INCLUDED

struct TreeData
{
	float3 position;	// 先端の座標
	int backID;			// 根本の座標
	int nextID;			// 次のnodeの座標
	float radius;
};

#endif // PROCEDURAL_TREE_DATA_INCLUDED
