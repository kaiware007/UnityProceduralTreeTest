Shader "Custom/ProceduralTree"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_uvScale("UV Scale", Range(0,1)) = 0.01 
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			//Cull Off
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			// make fog work
			//#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "ProceduralTreeData.cginc"
			#include "Libs/Quaternion.cginc"

			struct v2g {
				float4 forward_pos : SV_POSITION;
				float4 back_pos : TEXCOORD0;
				float4 next_pos : TEXCOORD1;
				float3 forward_dir : TEXCOORD2;
				float3 back_dir : TEXCOORD3;
				float forward_radius : TEXCOORD4;
				float back_radius : TEXCOORD5;
				float2 growthLength : TEXCOORD6;
			};

			struct g2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
				//float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			StructuredBuffer<TreeData> _TreeBuffer;
			StructuredBuffer<int> _IndexBuffer;
			int _TreeCount;
			int _IndexCount;
			//int _DivideCount;
			float _uvScale;

			v2g vert (uint id : SV_VertexID)
			{
				//TreeData o = _TreeBuffer[id];

				v2g o = (v2g)0;
				
				int idx = _IndexBuffer[id];
				float3 back_pos2 = float3(0, 0, 0);
				//float3 next_pos2 = float3(0, 0, 0);

				o.forward_pos = float4(_TreeBuffer[idx].position,1);
				o.back_pos = float4(_TreeBuffer[idx].startPosition, 1);
				if (_TreeBuffer[idx].backID >= 0) {
					back_pos2 = float4(_TreeBuffer[_TreeBuffer[idx].backID].startPosition, 1);
				}
				else {
					back_pos2 = _TreeBuffer[idx].startPosition;
				}

				o.next_pos = (_TreeBuffer[idx].nextID >= 0) ? float4(_TreeBuffer[_TreeBuffer[idx].nextID].position,1) : o.forward_pos;
				o.forward_radius = _TreeBuffer[idx].radius;
				o.back_radius = (_TreeBuffer[idx].backID >= 0) ? _TreeBuffer[_TreeBuffer[idx].backID].radius : o.forward_radius;

				// forward_dir
				float3 forward_dirBack = o.forward_pos.xyz - o.back_pos.xyz;
				float3 forward_dirNext = o.next_pos.xyz - o.forward_pos.xyz;

				o.forward_dir = forward_dirBack + forward_dirNext;

				// back_dir
				float3 back_dirBack = o.back_pos.xyz - back_pos2;
				float3 back_dirNext = o.forward_pos.xyz - o.back_pos.xyz;

				o.back_dir = back_dirBack + back_dirNext;

				o.growthLength = float2(_TreeBuffer[idx].startLength, _TreeBuffer[idx].growthLength);

				//o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				
				return o;
			}

			// ジオメトリシェーダ
			[maxvertexcount(36)]
			void geom(point v2g input[1], inout TriangleStream<g2f> outStream) 
			//void geom(point v2g input[1], inout LineStream<g2f> outStream)
			{
				g2f o = (g2f)0;

				float4 topVertex[4];

				for(int i = 0; i < 4; i++)
				{

					float angle = (float)i / 4.0 * PI;
					float angle2 = (float)(i+1) / 4.0 * PI;

					float4 fq = fromToRotation(float3(0, 0, 1), input[0].forward_dir);
					float4 bq = fromToRotation(float3(0, 0, 1), input[0].back_dir);
					float3 fnorm = rotateWithQuaternion(float3(0, 1, 0), fq);
					float3 bnorm = rotateWithQuaternion(float3(0, 1, 0), bq);
					float3 forward_right = rotateAngleAxis(fnorm, input[0].forward_dir, angle);
					float3 forward_right2 = rotateAngleAxis(fnorm, input[0].forward_dir, angle2);
					float3 back_right = rotateAngleAxis(bnorm, input[0].back_dir, angle);
					float3 back_right2 = rotateAngleAxis(bnorm, input[0].back_dir, angle2);

					float4 lt = UnityObjectToClipPos(input[0].forward_pos + float4(forward_right * input[0].forward_radius * 0.5, 0));	// lt
					float4 rt = UnityObjectToClipPos(input[0].forward_pos + float4(forward_right2 * input[0].forward_radius * 0.5, 0));	// rt
					float4 lb = UnityObjectToClipPos(input[0].back_pos + float4(back_right * input[0].back_radius * 0.5, 0));	// lb
					float4 rb = UnityObjectToClipPos(input[0].back_pos + float4(back_right2 * input[0].back_radius  * 0.5, 0));	// rb

					topVertex[i] = lt;

					// 0
					o.position = lt;
					o.uv = float2(0, (input[0].growthLength.x + input[0].growthLength.y) * _uvScale);
					outStream.Append(o);

					// 1
					o.position = lb;
					o.uv = float2(0, (input[0].growthLength.x) * _uvScale);
					outStream.Append(o);

					// 2
					o.position = rt;
					o.uv = float2(0.5, (input[0].growthLength.x + input[0].growthLength.y) * _uvScale);
					outStream.Append(o);

					outStream.RestartStrip();

					// 3
					o.position = lb;
					o.uv = float2(0, (input[0].growthLength.x) * _uvScale);
					outStream.Append(o);

					// 4
					o.position = rb;
					o.uv = float2(0.5, (input[0].growthLength.x) * _uvScale);
					outStream.Append(o);

					// 5
					o.position = rt;
					o.uv = float2(0.5, (input[0].growthLength.x + input[0].growthLength.y) * _uvScale);
					outStream.Append(o);
					outStream.RestartStrip();
				}

				// ふた
				float4 fpos = UnityObjectToClipPos(input[0].forward_pos);
				// 1
				o.position = topVertex[0];
				o.uv = float2(0.5, 0);
				outStream.Append(o);

				o.position = topVertex[1];
				o.uv = float2(0.5, 1);
				outStream.Append(o);

				o.position = fpos;
				o.uv = float2(0.75, 0.5);
				outStream.Append(o);
				outStream.RestartStrip();
				
				// 2
				o.position = topVertex[1];
				o.uv = float2(0.5, 1);
				outStream.Append(o);

				o.position = topVertex[2];
				o.uv = float2(1, 1);
				outStream.Append(o);

				o.position = fpos;
				o.uv = float2(0.75, 0.5);
				outStream.Append(o);
				outStream.RestartStrip();

				// 3
				o.position = topVertex[2];
				o.uv = float2(1, 1);
				outStream.Append(o);

				o.position = topVertex[3];
				o.uv = float2(1, 0);
				outStream.Append(o);

				o.position = fpos;
				o.uv = float2(0.75, 0.5);
				outStream.Append(o);
				outStream.RestartStrip();

				// 4
				o.position = topVertex[3];
				o.uv = float2(1, 0);
				outStream.Append(o);

				o.position = topVertex[0];
				o.uv = float2(0.5, 0);
				outStream.Append(o);

				o.position = fpos;
				o.uv = float2(0.75, 0.5);
				outStream.Append(o);
				outStream.RestartStrip();

				//o.position = UnityObjectToClipPos(input[0].forward_pos);
				//outStream.Append(o);

				//o.position = UnityObjectToClipPos(input[0].back_pos);
				//outStream.Append(o);

				//outStream.RestartStrip();
			}

			fixed4 frag (g2f i) : SV_Target
			{
				//fixed4 col = fixed4(i.uv.x, i.uv.y, 0 ,1);
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				//// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
