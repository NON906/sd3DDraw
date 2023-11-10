Shader "Hidden/SD3DDraw/Overlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseTex ("BaseTexture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent" }
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
                float2 uvbase : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BaseTex;
            float4 _BaseTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvbase = TRANSFORM_TEX(v.uv, _BaseTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
    			fixed4 bcol = tex2D(_BaseTex, i.uvbase);
    			fixed4 mcol = tex2D(_MainTex, i.uv);		
				fixed3 col = lerp(bcol.rgb, mcol.rgb, mcol.a);
				return fixed4(col.r, col.g, col.b, 1);
            }
            ENDCG
        }
    }
}
