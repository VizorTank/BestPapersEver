Shader "Custom/InstancedIndirectColor2" {
    // Properties
	// {
	// 	// _MainTex ("Texture", 2D) = "White" { }
	// 	_MainTexArray ("Texture", 2DArray) = "White" { }
    //     // _Test ("Test", 2DArray) = "White" { }
	// 	// _HaveTexture ("Have Texture", int) = 1 { } 
	// }
    SubShader {
        Tags { "RenderType" = "Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray
            
            #include "UnityCG.cginc"
            
            struct appdata_t {
                float4 vertex   : POSITION;
                float2 uv : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                float3 uv : TEXCOORD0;
                fixed4 color    : COLOR;
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

            static const int3 neighbours[6] = {
                int3(0, 0, -1),
                int3(0, 0, 1),
                int3(0, 1, 0),
                int3(0, -1, 0),
                int3(-1, 0, 0),
                int3(1, 0, 0)
            };

            static const float4 blockTypes[6] = {
                float4(1, 0, 0, 1),
                float4(0, 1, 0, 1),
                float4(0, 0, 1, 1),
                float4(1, 1, 0, 1),
                float4(0, 1, 1, 1),
                float4(1, 0, 1, 1)
            };

            // sampler2D _MainTex;
            // UNITY_DECLARE_TEX2DARRAY(_MainTexArray);
            UNITY_DECLARE_TEX2DARRAY(_MainTexArray2);
            
            StructuredBuffer<BlockSideData> _BlockSideDataBuffer;
            // StructuredBuffer<int> _BlockIdBuffer;
            // StructuredBuffer<int> _BlockIsTransparentBuffer;

            // float AmbientOcclusionIntensity;
            // float4 AmbientOcclusionColor;

            float4x4 move(float4x4 mat, float3 vec)
            {
                mat[0][3] += vec[0];
                mat[1][3] += vec[1];
                mat[2][3] += vec[2];
                return mat;
            }

            // int GetIndex(int3 pos) {
            //     return pos.x + (pos.y + pos.z * 16) * 16;
            // }

            // float4 AO(float4 color, float3 vert, int rot, int3 gPos, int type)
            // {
            //     if (!_BlockIsTransparentBuffer[type - 1]) return 0;

            //     int3 range = float3(2, 2, 2);
            //     int3 start = 0;
            //     int3 offset = 0;

            //     switch(rot)
            //     {
            //         case 0: start.z -= 1; range.z -= 1; break;
            //         case 1: start.z += 1; range.z -= 1; break;
            //         case 2: start.y += 1; range.y -= 1; break;
            //         case 3: start.y -= 1; range.y -= 1; break;
            //         case 4: start.x -= 1; range.x -= 1; break;
            //         case 5: start.x += 1; range.x -= 1; break;
            //     }

            //     if (rot == 0)
            //     {
            //         // OK
            //         offset.x -= 1 - vert.x;
            //         offset.y -= 1 - vert.y;
            //     }
            //     else if (rot == 1)
            //     {
            //         // OK
            //         offset.x -= vert.x;
            //         offset.y -= 1 - vert.y;
            //     }
            //     else if (rot == 2)
            //     {
            //         // OK
            //         offset.x -= 1 - vert.x;
            //         offset.z -= 1 - vert.y;
            //     }
            //     else if (rot == 3)
            //     {
            //         // OK
            //         offset.x -= 1 - vert.x;
            //         offset.z -= vert.y;
            //     }
            //     else if (rot == 4)
            //     {
            //         // OK
            //         offset.z -= vert.x;
            //         offset.y -= 1 - vert.y;
            //     }
            //     else
            //     {
            //         // OK
            //         offset.z -= 1 - vert.x;
            //         offset.y -= 1 - vert.y;
            //     }

            //     int count = 0;

            //     for (int x = 0; x < range.x; x++)
            //     {
            //         int mx = gPos.x + offset.x + start.x + x;
            //         if (mx < 0 || mx >= 16) continue;
            //         for (int y = 0; y < range.y; y++)
            //         {
            //             int my = gPos.y + offset.y + start.y + y;
            //             if (my < 0 || my >= 16) continue;
            //             for (int z = 0; z < range.z; z++)
            //             {
            //                 int mz = gPos.z + offset.z + start.z + z;
            //                 if (mz < 0 || mz >= 16) continue;
            //                 int t = _BlockIsTransparentBuffer[_BlockIdBuffer[GetIndex(int3(mx, my, mz))] - 1];
            //                 // int t = _BlockIdBuffer[GetIndex(int3(mx, my, mz))];
            //                 if (t != 0) 
            //                     count++;
            //             }
            //         }
            //     }
            //     return (1 - AmbientOcclusionColor) * count / 4.0;
            // }

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;
                float4x4 mat = _blockSides[_BlockSideDataBuffer[instanceID].rotation];
                mat = move(mat, _BlockSideDataBuffer[instanceID].position);
                // float4 pos = mul(_BlockSideDataBuffer[instanceID].mat, i.vertex);
                float4 pos = mul(mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);
                // o.color = _BlockSideDataBuffer[instanceID].color;
                // float3 col = blockTypes[_BlockSideDataBuffer[instanceID].type];
                // o.color = blockTypes[_BlockSideDataBuffer[instanceID].type];
                // float3 col = i.vertex + 0.5;
                // o.color = i.vertex + 0.5;

                // o.uv = i.uv;
                // o.uv = float3(i.uv.xy, 0);
                o.uv.xy = i.uv.xy;
                o.uv.z = _BlockSideDataBuffer[instanceID].type - 1;
                o.color = float4(0, 0, 0, 1);
                // o.color = AO(blockTypes[_BlockSideDataBuffer[instanceID].type],
                //         i.vertex + 0.5, 
                //         _BlockSideDataBuffer[instanceID].rotation,
                //         _BlockSideDataBuffer[instanceID].position,
                //         _BlockSideDataBuffer[instanceID].type) * AmbientOcclusionIntensity;

                return o;
            }

            
            
            fixed4 frag(v2f i) : SV_Target {
                // float4 col = float4(1, 1, 1, 1);
                // if (i.color.x >= 0.5)
                //     col.x = 1;
                // else
                //     col.x = 0;
                // if (i.color.y >= 0.5)
                //     col.y = 1;
                // else
                //     col.y = 0;
                fixed4 col;
                // col = tex2D(_MainTex, i.uv);
                col = UNITY_SAMPLE_TEX2DARRAY(_MainTexArray2, i.uv);
                return col - i.color;
            }
            
            ENDCG
        }
    }
}