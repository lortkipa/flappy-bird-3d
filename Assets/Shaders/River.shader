Shader "Wildflight/River" {
 Properties { _Color("Deep water", Color)=(.045,.17,.16,1) _ReflectionTex("Reflection",2D)="black"{} }
 SubShader {
  Tags { "RenderType"="Opaque" }
  CGPROGRAM
  #pragma surface surf Standard vertex:vert
  #pragma target 3.0
  sampler2D _ReflectionTex; fixed4 _Color;
  struct Input { float3 worldPos; float4 screenPos; float3 viewDir; };
  void vert(inout appdata_full v) {
   float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
   v.vertex.y+=(sin(p.x*1.1+_Time.y*.8)+sin(p.z*1.7+p.x*.3+_Time.y*.6))*.017;
  }
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float2 p=IN.worldPos.xz;
   float2 ripple=float2(sin(p.x*2.7+p.y*1.2+_Time.y),cos(p.y*3.6-p.x*.8+_Time.y*.7))*.012;
   float2 uv=IN.screenPos.xy/max(IN.screenPos.w,.0001)+ripple*.055;
   float3 reflection=tex2D(_ReflectionTex,uv).rgb;
   float fresnel=pow(1-saturate(dot(normalize(IN.viewDir),float3(0,0,1))),3);
   o.Albedo=_Color.rgb*.55;
   o.Emission=reflection*lerp(.18,.55,fresnel)*float3(.63,.79,.70);
   o.Normal=normalize(float3(ripple.x,ripple.y,1));
   o.Metallic=.35;o.Smoothness=.92;
  }
  ENDCG
 }
 Fallback "Standard"
}
