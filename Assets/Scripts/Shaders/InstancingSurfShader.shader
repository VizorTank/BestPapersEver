// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Custom/InstancingSurfShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #include "UnityCG.cginc"

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow vertex:vert
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 5.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        struct BlockSideData {
            float3 position;
            int rotation;
            int type;
        };

        static const float4x4 _blockSides[6] = {
            float4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, -0.5,
                0, 0, 0, 1
            ),
            float4x4(
                -1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, -1, 0.5,
                0, 0, 0, 1
            ),
            float4x4(
                1, 0, 0, 0,
                0, 0, -1, 0.5,
                0, 1, 0, 0,
                0, 0, 0, 1
            ),
            float4x4(
                1, 0, 0, 0,
                0, 0, 1, -0.5,
                0, -1, 0, 0,
                0, 0, 0, 1
            ),
            float4x4(
                0, 0, 1, -0.5,
                0, 1, 0, 0,
                -1, 0, 0, 0,
                0, 0, 0, 1
            ),
            float4x4(
                0, 0, -1, 0.5,
                0, 1, 0, 0,
                1, 0, 0, 0,
                0, 0, 0, 1
            )
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        // UNITY_INSTANCING_BUFFER_START(Props)
        //     // put more per-instance properties here
        // UNITY_INSTANCING_BUFFER_END(Props)

        // #ifdef SHADER_API_GLCORE
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<BlockSideData> _BlockSideDataBuffer;
        #endif

        struct appdata{
            float4 vertex : SV_POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;

            uint id : SV_InstanceID;
        };

        void setup() {}

        float4x4 move(float4x4 mat, float3 vec)
        {
            mat[0][3] += vec[0];
            mat[1][3] += vec[1];
            mat[2][3] += vec[2];
            return mat;
        }

        void vert (inout appdata_full v) {
            // #ifdef SHADER_API_GLCORE
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float4x4 mat = _blockSides[_BlockSideDataBuffer[unity_InstanceID].rotation];
            mat = move(mat, _BlockSideDataBuffer[unity_InstanceID].position);
            v.vertex = mul(mat, v.vertex);
            #endif
        }   

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
