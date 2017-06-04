Shader "Custom/ProceduralTree"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_uvScale("UV Scale", Range(0,1)) = 1
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

		CGINCLUDE
		#include "HLSLSupport.cginc"
		#include "UnityShaderVariables.cginc"

		#define UNITY_PASS_DEFERRED
		#include "UnityCG.cginc"
		#include "ProceduralTreeData.cginc"
		#include "Libs/Quaternion.cginc"
		#include "Lighting.cginc"
		#include "UnityPBSLighting.cginc"

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
			half3 worldNormal : TEXCOORD1;
			float3 worldPos : TEXCOORD2;
			half3 sh : TEXCOORD3; // SH
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;
		half _Glossiness;
		half _Metallic;

		StructuredBuffer<TreeData> _TreeBuffer;
		StructuredBuffer<int> _IndexBuffer;
		int _TreeCount;
		int _IndexCount;
		//int _DivideCount;
		float _uvScale;

		v2g vert(uint id : SV_VertexID)
		{
			//TreeData o = _TreeBuffer[id];

			v2g o = (v2g)0;

			if (id >= _IndexCount) {
				return o;
			}
			int idx = _IndexBuffer[id];
			float3 back_pos2 = float3(0, 0, 0);
			//float3 next_pos2 = float3(0, 0, 0);

			o.forward_pos = float4(_TreeBuffer[idx].position, 1);
			o.back_pos = float4(_TreeBuffer[idx].startPosition, 1);
			if (_TreeBuffer[idx].backID >= 0) {
				back_pos2 = float4(_TreeBuffer[_TreeBuffer[idx].backID].startPosition, 1);
			}
			else {
				back_pos2 = _TreeBuffer[idx].startPosition;
			}

			o.next_pos = (_TreeBuffer[idx].nextID >= 0) ? float4(_TreeBuffer[_TreeBuffer[idx].nextID].position, 1) : o.forward_pos;
			o.forward_radius = _TreeBuffer[idx].radius;
			//o.back_radius = (_TreeBuffer[idx].backID >= 0) ? _TreeBuffer[_TreeBuffer[idx].backID].radius : o.forward_radius;
			o.back_radius = _TreeBuffer[idx].startRadius;

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
			float4 topVertexLocal[4];
			float3 topVertexWorld[4];

			for (int i = 0; i < 4; i++)
			{

				float angle = (float)i / 4.0 * PI;
				float angle2 = (float)(i + 1) / 4.0 * PI;

				float4 fq = fromToRotation(float3(0, 0, 1), input[0].forward_dir);
				float4 bq = fromToRotation(float3(0, 0, 1), input[0].back_dir);
				float3 fnorm = rotateWithQuaternion(float3(0, 1, 0), fq);
				float3 bnorm = rotateWithQuaternion(float3(0, 1, 0), bq);
				float3 forward_right = rotateAngleAxis(fnorm, input[0].forward_dir, angle);
				float3 forward_right2 = rotateAngleAxis(fnorm, input[0].forward_dir, angle2);
				float3 back_right = rotateAngleAxis(bnorm, input[0].back_dir, angle);
				float3 back_right2 = rotateAngleAxis(bnorm, input[0].back_dir, angle2);

				float4 lt_local = input[0].forward_pos + float4(forward_right * input[0].forward_radius * 0.5, 0);
				float4 rt_local = input[0].forward_pos + float4(forward_right2 * input[0].forward_radius * 0.5, 0);
				float4 lb_local = input[0].back_pos + float4(back_right * input[0].back_radius * 0.5, 0);
				float4 rb_local = input[0].back_pos + float4(back_right2 * input[0].back_radius  * 0.5, 0);

				float4 lt_proj = UnityObjectToClipPos(lt_local);	// lt
				float4 rt_proj = UnityObjectToClipPos(rt_local);	// rt
				float4 lb_proj = UnityObjectToClipPos(lb_local);	// lb
				float4 rb_proj = UnityObjectToClipPos(rb_local);	// rb

				float3 lt_world = mul(unity_ObjectToWorld, lt_local).xyz;	// lt
				float3 rt_world = mul(unity_ObjectToWorld, rt_local).xyz;	// rt
				float3 lb_world = mul(unity_ObjectToWorld, lb_local).xyz;	// lb
				float3 rb_world = mul(unity_ObjectToWorld, rb_local).xyz;	// rb

				float3 normal1 = UnityObjectToWorldNormal(normalize(cross(lb_local - lt_local, rt_local - lb_local)));
				float3 normal2 = UnityObjectToWorldNormal(normalize(cross(rb_local - lb_local, rt_local - rb_local)));

				half3 sh1 = ShadeSH3Order(half4(normal1, 1.0));
				half3 sh2 = ShadeSH3Order(half4(normal2, 1.0));

				topVertexLocal[i] = lt_local;
				topVertex[i] = lt_proj;
				topVertexWorld[i] = lt_world;

				// １つめのtriangle
				// 0
				o.position = lt_proj;
				o.uv = float2(0, (input[0].growthLength.x + input[0].growthLength.y) * _uvScale);
				o.worldPos = lt_world;
				o.worldNormal = normal1;
				o.sh = sh1;
				outStream.Append(o);

				// 1
				o.position = lb_proj;
				o.uv = float2(0, (input[0].growthLength.x) * _uvScale);
				o.worldPos = lb_world;
				o.worldNormal = normal1;
				o.sh = sh1;
				outStream.Append(o);

				// 2
				o.position = rt_proj;
				o.uv = float2(0.5, (input[0].growthLength.x + input[0].growthLength.y) * _uvScale);
				o.worldPos = rt_world;
				o.worldNormal = normal1;
				o.sh = sh1;
				outStream.Append(o);

				outStream.RestartStrip();

				// 2つめのtriangle
				// 3
				o.position = lb_proj;
				o.uv = float2(0, (input[0].growthLength.x) * _uvScale);
				o.worldPos = lb_world;
				o.worldNormal = normal2;
				o.sh = sh2;
				outStream.Append(o);

				// 4
				o.position = rb_proj;
				o.uv = float2(0.5, (input[0].growthLength.x) * _uvScale);
				o.worldPos = rb_world;
				o.worldNormal = normal2;
				o.sh = sh2;
				outStream.Append(o);

				// 5
				o.position = rt_proj;
				o.uv = float2(0.5, (input[0].growthLength.x + input[0].growthLength.y) * _uvScale);
				o.worldPos = rt_world;
				o.worldNormal = normal2;
				o.sh = sh2;
				outStream.Append(o);
				outStream.RestartStrip();
			}

			// ふた
			float4 fpos = UnityObjectToClipPos(input[0].forward_pos);
			float3 wpos = mul(unity_ObjectToWorld, input[0].forward_pos).xyz;

			// 1
			o.position = topVertex[0];
			o.uv = float2(0.5, 0);
			o.worldPos = topVertexWorld[0];
			o.worldNormal = UnityObjectToWorldNormal(normalize(cross(topVertexLocal[1] - topVertexLocal[0], input[0].forward_pos - topVertexLocal[1])));
			o.sh = ShadeSH3Order(half4(o.worldNormal, 1.0));
			outStream.Append(o);

			o.position = topVertex[1];
			o.uv = float2(0.5, 1);
			o.worldPos = topVertexWorld[0];
			outStream.Append(o);

			o.position = fpos;
			o.uv = float2(0.75, 0.5);
			o.worldPos = wpos;
			outStream.Append(o);
			outStream.RestartStrip();

			// 2
			o.position = topVertex[1];
			o.uv = float2(0.5, 1);
			o.worldPos = topVertexWorld[1];
			o.worldNormal = UnityObjectToWorldNormal(normalize(cross(topVertexLocal[2] - topVertexLocal[1], input[0].forward_pos - topVertexLocal[2])));
			o.sh = ShadeSH3Order(half4(o.worldNormal, 1.0));
			outStream.Append(o);

			o.position = topVertex[2];
			o.uv = float2(1, 1);
			o.worldPos = topVertexWorld[2];
			outStream.Append(o);

			o.position = fpos;
			o.uv = float2(0.75, 0.5);
			o.worldPos = wpos;
			outStream.Append(o);
			outStream.RestartStrip();

			// 3
			o.position = topVertex[2];
			o.uv = float2(1, 1);
			o.worldPos = topVertexWorld[2];
			o.worldNormal = UnityObjectToWorldNormal(normalize(cross(topVertexLocal[3] - topVertexLocal[2], input[0].forward_pos - topVertexLocal[3])));
			o.sh = ShadeSH3Order(half4(o.worldNormal, 1.0));
			outStream.Append(o);

			o.position = topVertex[3];
			o.uv = float2(1, 0);
			o.worldPos = topVertexWorld[3];
			outStream.Append(o);

			o.position = fpos;
			o.uv = float2(0.75, 0.5);
			o.worldPos = wpos;
			outStream.Append(o);
			outStream.RestartStrip();

			// 4
			o.position = topVertex[3];
			o.uv = float2(1, 0);
			o.worldPos = topVertexWorld[3];
			o.worldNormal = UnityObjectToWorldNormal(normalize(cross(topVertexLocal[0] - topVertexLocal[3], input[0].forward_pos - topVertexLocal[0])));
			o.sh = ShadeSH3Order(half4(o.worldNormal, 1.0));
			outStream.Append(o);

			o.position = topVertex[0];
			o.uv = float2(0.5, 0);
			o.worldPos = topVertexWorld[0];
			outStream.Append(o);

			o.position = fpos;
			o.uv = float2(0.75, 0.5);
			o.worldPos = wpos;
			outStream.Append(o);
			outStream.RestartStrip();

			//o.position = UnityObjectToClipPos(input[0].forward_pos);
			//outStream.Append(o);

			//o.position = UnityObjectToClipPos(input[0].back_pos);
			//outStream.Append(o);

			//outStream.RestartStrip();
		}

		//fixed4 frag (g2f i) : SV_Target
		void frag(g2f IN,
			out half4 outDiffuse : SV_Target0,
			out half4 outSpecSmoothness : SV_Target1,
			out half4 outNormal : SV_Target2,
			out half4 outEmission : SV_Target3 )
		{
			float3 worldPos = IN.worldPos;

			fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

#ifdef UNITY_COMPILER_HLSL
			SurfaceOutputStandard o = (SurfaceOutputStandard)0;
#else
			SurfaceOutputStandard o;
#endif

			//o.Albedo = 0.0;
			//o.Emission = 0.0;
			//o.Alpha = 0.0;
			o.Occlusion = 1.0;
			o.Normal = IN.worldNormal;

			//fixed4 col = fixed4(i.uv.x, i.uv.y, 0 ,1);
			// sample the texture
			fixed4 col = tex2D(_MainTex, IN.uv);
			//// apply fog
			//UNITY_APPLY_FOG(i.fogCoord, col);

			o.Albedo = col.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = col.a;

			// Setup lighting environment
			UnityGI gi;
			UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
			gi.indirect.diffuse = 0;
			gi.indirect.specular = 0;
			gi.light.color = 0;
			gi.light.dir = half3(0, 1, 0);
			gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
			// Call GI (lightmaps/SH/reflections) lighting function
			UnityGIInput giInput;
			UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
			giInput.light = gi.light;
			giInput.worldPos = worldPos;
			giInput.worldViewDir = worldViewDir;
			giInput.atten = 1.0;

			giInput.ambient = IN.sh;

			giInput.probeHDR[0] = unity_SpecCube0_HDR;
			giInput.probeHDR[1] = unity_SpecCube1_HDR;

#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMax[0] = unity_SpecCube0_BoxMax;
			giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
			giInput.boxMax[1] = unity_SpecCube1_BoxMax;
			giInput.boxMin[1] = unity_SpecCube1_BoxMin;
			giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif

			LightingStandard_GI(o, giInput, gi);

			// call lighting function to output g-buffer
			outEmission = LightingStandard_Deferred(o, worldViewDir, gi, outDiffuse, outSpecSmoothness, outNormal);
			outDiffuse.a = 1.0;

#ifndef UNITY_HDR_ON
			outEmission.rgb = exp2(-outEmission.rgb);
#endif
			//return col;
		}
		ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Name "DEFERRED"
			//Tags{ "LightMode" = "ForwardBase" }
			Tags{ "LightMode" = "Deferred" }
			//Lighting On
			//Cull Off

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma exclude_renderers nomrt
			#pragma multi_compile_prepassfinal noshadow
			// make fog work
			//#pragma multi_compile_fog
			ENDCG
		}
	}

	FallBack "Diffuse"
}
