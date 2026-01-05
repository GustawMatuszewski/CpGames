Shader "Unlit/Grass_SpawnPatch_NoInstancing"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.1, 0.4, 0.1, 1)
        _TipColor ("Tip Color", Color) = (0.3, 0.7, 0.2, 1)
        _PatchSize ("Spawn Spread", Float) = 2.0
        _Density ("Blades Per Patch", Range(1, 20)) = 8
        _MaxHeight ("Max Height", Float) = 1.0
        _WindSpeed ("Wind Speed", Float) = 2.0
        _FadeEnd ("Fade Distance", Float) = 50
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0 
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 col : COLOR;
                float fade : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _BaseColor, _TipColor;
            float _PatchSize, _MaxHeight, _WindSpeed, _FadeEnd, _Density;

            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            v2g vert (appdata v) {
                v2g o;
                o.pos = v.vertex;
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // This spawns up to 20 blades (60 verts) from 1 triangle
            [maxvertexcount(60)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream) {
                for(int i = 0; i < (int)_Density; i++) {
                    float2 seed = float2(input[0].worldPos.x + i, input[0].worldPos.z + i);
                    float h = hash(seed);
                    
                    // Offset based on your Public Patch Size variable
                    float3 offset = float3((h - 0.5) * _PatchSize, 0, (hash(seed + 0.5) - 0.5) * _PatchSize);
                    
                    float dist = distance(input[0].worldPos + offset, _WorldSpaceCameraPos);
                    float fade = saturate(1.0 - (dist / _FadeEnd));

                    // Stop drawing if too far away
                    if (fade <= 0) continue;

                    for(int j = 0; j < 3; j++) {
                        g2f o;
                        float3 vLocal = input[j].pos.xyz;
                        
                        // Wind movement (Sine wave)
                        float wind = sin(_Time.y * _WindSpeed + input[0].worldPos.x + i) * 0.1;
                        
                        float3 fWorld = input[0].worldPos + offset;
                        fWorld.y += vLocal.y * _MaxHeight * (0.8 + h * 0.4) * fade;
                        fWorld.xz += vLocal.xz + (wind * input[j].uv.y);

                        o.pos = UnityWorldToClipPos(float4(fWorld, 1.0));
                        o.uv = input[j].uv;
                        o.col = lerp(_BaseColor, _TipColor, input[j].uv.y);
                        o.fade = fade;
                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            fixed4 frag (g2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a * i.fade - 0.1);
                return tex * i.col;
            }
            ENDCG
        }
    }
}