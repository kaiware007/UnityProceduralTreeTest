Shader "Custom/ProceduralTree"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			// make fog work
			//#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "ProceduralTreeData.cginc"

			struct v2g {
				float4 forward_pos : SV_POSITION;
				float4 back_pos : TEXCOORD0;
				float4 next_pos : TEXCOORD1;
				float forward_radius : TEXCOORD2;
				float back_radius : TEXCOORD3;
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

			v2g vert (uint id : SV_VertexID)
			{
				//TreeData o = _TreeBuffer[id];

				v2g o = (v2g)0;
				
				int idx = _IndexBuffer[id];

				o.forward_pos = float4(_TreeBuffer[idx].position,1);
				o.back_pos = (_TreeBuffer[idx].backID >= 0) ? float4(_TreeBuffer[_TreeBuffer[idx].backID].position,1) : o.forward_pos;
				o.next_pos = (_TreeBuffer[idx].nextID >= 0) ? float4(_TreeBuffer[_TreeBuffer[idx].nextID].position,1) : o.forward_pos;
				o.forward_radius = _TreeBuffer[idx].radius;
				o.back_radius = (_TreeBuffer[idx].backID >= 0) ? _TreeBuffer[_TreeBuffer[idx].backID].radius : o.forward_radius;

				//o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				
				return o;
			}
			
			// ジオメトリシェーダ
			[maxvertexcount(2)]
			//void geom(point TreeData input[1], inout TriangleStream<g2f> outStream) 
			void geom(point v2g input[1], inout LineStream<g2f> outStream)
			{
				g2f o = (g2f)0;

				o.position = UnityObjectToClipPos(input[0].forward_pos);
				outStream.Append(o);

				o.position = UnityObjectToClipPos(input[0].back_pos);
				outStream.Append(o);

				outStream.RestartStrip();
			}

			fixed4 frag (g2f i) : SV_Target
			{
				fixed4 col = fixed4(1,0,0,1);
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				//// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
