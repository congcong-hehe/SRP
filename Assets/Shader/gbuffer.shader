Shader "HeRP/gbuffer"
{
    Properties
    {
        _MainTex("Albedo Map", 2D) = "white" {}
        [Space(25)]

        _MetallicMap ("Metallic Map", 2D) = "white" {}
        [Space(25)]

        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        [Space(25)]

        _OccusionMap ("Occusion Map", 2D) = "white" {}
        [Space(25)]

        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "LightMode" = "gbuffer" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float4 tangent : TEXCOORD3;
            };

            float4 _MainTex_ST;

            sampler2D _MainTex;
            sampler2D _MetallicMap;
            sampler2D _RoughnessMap;
            sampler2D _OccusionMap;
            sampler2D _NormalMap;

            v2f vert(appData v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;
                o.tangent = v.tangent;
                return o;
            }

            float3 calNormalFromMap(float3 texture_normal, float3 normal, float4 tangent)
            {
                fixed3 worldNormal = UnityObjectToWorldNormal(normal);  
                fixed3 worldTangent = UnityObjectToWorldDir(tangent.xyz);  
                fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangent.w;
                float3x3 TBN = float3x3(worldTangent, worldBinormal, worldNormal);
                return normalize(mul(TBN, texture_normal));
            }

            void frag(
                v2f i,
                out float4 GT0 : SV_Target0,
                out float4 GT1 : SV_Target1,
                out float4 GT2 : SV_Target2,
                out float4 GT3 : SV_Target3
            )
            {
                float4 color = tex2D(_MainTex, i.uv);

                float3 normal = (calNormalFromMap(UnpackNormal(tex2D(_NormalMap, i.uv)), i.normal, i.tangent) + 1) * 0.5;

                float metallic = tex2D(_MetallicMap, i.uv).r;
                float roughness = tex2D(_MetallicMap, i.uv).r;
                float ao = tex2D(_OccusionMap, i.uv).r;

                GT0 = color;
                GT1 = float4(normal, 0);
                GT2 = float4(i.worldPos, 0);
                GT3 = float4(metallic,roughness, ao, 0);
            }
            ENDCG
        }
    }
}