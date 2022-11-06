Shader "Unlit/Hmm"
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
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members worldPos)
#pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            v2f vert (appdata v)
            {
                v2f o;

                // float phase = _Time * 30.0;
                // float4 wpos = v.vertex;//mul(v.vertex, unity_ObjectToWorld);
                // float offset = (wpos.x + (wpos.z * 0.2)) * 0.5;
                // float a = round(sin(phase + wpos.z) * 2.0);
                // if (a < 0)
                //     wpos.y = -1;
                // else
                //     wpos.y = 1;
                // o.vertex = UnityObjectToClipPos(wpos);

                


                float phase = _Time * 30.0;
                float4 wpos = v.vertex;//mul( unity_ObjectToWorld, v.vertex);// mul( unity_ObjectToWorld, v.vertex);
                // float offset = (wpos.x + (wpos.z * 0.2)) * 0.5;
                // float a = round(sin(phase + wpos.x) * 2.0);
                // if (wpos.x == 0)
                //     wpos.y = -1;
                // else
                //     wpos.y = 1;
                o.vertex = UnityObjectToClipPos(wpos);// mul(unity_WorldToObject, wpos);
                o.worldPos = mul (unity_ObjectToWorld, wpos);
                // float phase = _Time * 20.0;
                // float offset = (v.vertex.x + (v.vertex.z * 0.2)) * 0.5;
                // v.vertex.y = sin(phase + offset) * 0.2;


                // o.vertex = UnityObjectToClipPos(v.vertex);
                // o.vertex = floor(o.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = float4(1, 1, 1, 1);// tex2D(_MainTex, i.uv);
                // i.vertex.y = round(i.vertex.y);
                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                if (i.worldPos.y > 0)
                {
                    col = float4(0, 0, 0, 1);
                    // i.vertex.y = i.vertex.y + 1;
                }
                return col;
            }
            ENDCG
        }
    }
}
