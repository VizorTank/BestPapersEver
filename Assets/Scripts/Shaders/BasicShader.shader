Shader "Custom/BasicShader"
{
	Properties
	{
		_Color ("Color", Color) = (.25, .5, .5, 1)
		_MainTex ("Texture", 2D) = "White" { }
	}

	SubShader
	{
		Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		LOD 100
		Lighting Off

		Pass
		{

			CGPROGRAM
			#pragma vertex vertFunction
			#pragma fragment fragFunction
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
			
			struct v2f
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			float4 _Color;
			sampler2D _MainTex;
			float GlobalLightLevel;

			v2f vertFunction(appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;

				return o;
			}

			fixed4 fragFunction(v2f i) : SV_TARGET
			{
				fixed4 col;
				float localLightLevel = clamp(GlobalLightLevel, 0, 1);
				col = tex2D(_MainTex, i.uv);
				//clip(col.a - 1);
				col = col * _Color;
				col = lerp(col, float4(0, 0, 0, 1), localLightLevel);

				return col;
			}

			ENDCG
		}
    }
}
