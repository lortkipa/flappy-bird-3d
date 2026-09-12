Shader "Wildflight/Atmosphere" {
 Properties { _SunDir("Sun",Vector)=(-.4,.3,.7,0) }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _SunDir;
   struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
   v2f vert(appdata_base v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o; }
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   fixed4 frag(v2f i):SV_Target {
    float3 d=normalize(i.dir);float h=saturate(d.y);
    float3 col=lerp(float3(.72,.77,.66),float3(.19,.36,.41),pow(h,.55));
    float sun=pow(saturate(dot(d,normalize(_SunDir.xyz))),12);
    col+=float3(.6,.38,.12)*sun;
    col+=float3(2.8,2.0,1.05)*pow(saturate(dot(d,normalize(_SunDir.xyz))),1800);
    float2 p=d.xz/max(.08,d.y)*2;
    float n=noise(p)+noise(p*2.1)*.5+noise(p*4.3)*.25;
    float cloud=smoothstep(.86,1.25,n)*smoothstep(.04,.22,d.y)*.38;
    col=lerp(col,float3(.85,.84,.73),cloud);
    return float4(col,1);
   }
   ENDCG
  }
 }
}
