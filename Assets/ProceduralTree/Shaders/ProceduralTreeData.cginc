#ifndef PROCEDURAL_TREE_DATA_INCLUDED
#define PROCEDURAL_TREE_DATA_INCLUDED

struct TreeData
{
	float3 forward_pos;    // 先端の座標
	float3 back_pos;       // 根本の座標
	float3 next_pos;       // 次のnodeの座標
	float forward_radius;
	float back_radius;
};

#endif // PROCEDURAL_TREE_DATA_INCLUDED
