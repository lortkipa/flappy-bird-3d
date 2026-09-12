Shader "Hidden/Wildflight/Film" {
 Properties { _MainTex("Source",2D)="white"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
 Pass {
  CGPROGRAM
  #pragma vertex vert_img
  #pragma fragment frag
  #include "UnityCG.cginc"
  sampler2D _MainTex; float4 _MainTex_TexelSize;
  UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
  fixed4 frag(v2f_img i):SV_Target {
   float3 c=tex2D(_MainTex,i.uv).rgb;
   float3 bloom=0;
   for(int k=0;k<8;k++) {
    float a=k*.7854;float2 v=float2(cos(a),sin(a))*_MainTex_TexelSize.xy*5;
    bloom+=max(0,tex2D(_MainTex,i.uv+v).rgb-1);
   }
   c+=bloom*.035;
   float depth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv));
   float fog=(1-exp(-max(0,depth-24)*.009))*step(depth,280);
   float3 mist=lerp(float3(.23,.34,.32),float3(.53,.58,.46),saturate(i.uv.y));
   c=lerp(c,mist,fog*.60);
   c=c*(2.51*c+.03)/(c*(2.43*c+.59)+.14);
   float2 d=i.uv-.5;c*=1-dot(d,d)*.48;
   return float4(c,1);
  }
  ENDCG
 } }
}
