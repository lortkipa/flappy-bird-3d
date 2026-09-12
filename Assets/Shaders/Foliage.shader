Shader "Wildflight/Foliage" {
 Properties { _MainTex("Frond",2D)="white"{} _Color("Tint",Color)=(.6,.8,.68,1) _Cutoff("Cutout",Range(0,1))=.35 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard alphatest:_Cutoff addshadow vertex:vert
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color;
  struct Input {float2 uv_MainTex;float facing:VFACE;};
  void vert(inout appdata_full v) {
   float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
   v.vertex.x+=sin(_Time.y*1.2+world.x*.18+world.z*.23)*.006*v.vertex.y;
  }
  void surf(Input IN,inout SurfaceOutputStandard o) {
   fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;
   o.Albedo=c.rgb;o.Alpha=c.a;o.Smoothness=.16;
   o.Normal=float3(0,0,IN.facing>0?1:-1);
   o.Emission=c.rgb*.09;
  }
  ENDCG
 }
 Fallback "Transparent/Cutout/Diffuse"
}
