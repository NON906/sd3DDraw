Shader "Hidden/SD3DDraw/CalcMask"
{
    Properties
    {
        _MainTex ("TargetDepth", 2D) = "white" {}
        _AllTex ("AllDepth", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
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
                float2 uvall : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _AllTex;
            float4 _AllTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvall = TRANSFORM_TEX(v.uv, _AllTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float coltarget = tex2D(_MainTex, i.uv).x;
                float colall = tex2D(_AllTex, i.uvall).x;
                float addmask = step(0.001, colall - coltarget) * step(0.0001, coltarget);
                float retval = 1 - addmask;
                
                return fixed4(retval, retval, retval, 1);
            }
            ENDCG
        }
    }
}
