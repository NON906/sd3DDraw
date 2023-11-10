Shader "Hidden/SD3DDraw/CalcMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AllTex ("AllDepth", 2D) = "white" {}
        _TargetTex ("TargetDepth", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
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
                float2 uvtarget : TEXCOORD2;
                float2 uvmask : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _AllTex;
            float4 _AllTex_ST;
            sampler2D _TargetTex;
            float4 _TargetTex_ST;
            sampler2D _MaskTex;
            float4 _MaskTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvall = TRANSFORM_TEX(v.uv, _AllTex);
                o.uvtarget = TRANSFORM_TEX(v.uv, _TargetTex);
                o.uvmask = TRANSFORM_TEX(v.uv, _MaskTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float coltarget = tex2D(_TargetTex, i.uvtarget).x;
                float colall = tex2D(_AllTex, i.uvall).x;
                float addmask = step(0.001, colall - coltarget) * step(0.001, coltarget);
    
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_MaskTex, i.uvmask);
                col.w = (1 - addmask) * mask.x;
                
                return col;
            }
            ENDCG
        }
    }
}
