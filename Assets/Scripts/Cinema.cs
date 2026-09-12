using UnityEngine;

namespace Wildflight {
 [RequireComponent(typeof(Camera))]
 public sealed class Cinema : MonoBehaviour {
  Material film;
  void OnRenderImage(RenderTexture src, RenderTexture dst) {
   if (!film) film = new Material(Shader.Find("Hidden/Wildflight/Film"));
   Graphics.Blit(src, dst, film);
  }
  void OnDestroy() { if(film) Destroy(film); }
 }

 public sealed class RiverReflection : MonoBehaviour {
  public Material water;
  Camera mirror; RenderTexture target;
  void Start() {
   target = new RenderTexture(1024, 512, 16) { name="River reflection" };
   var go = new GameObject("Reflection camera");
   mirror=go.AddComponent<Camera>(); mirror.enabled=false;
   water.SetTexture("_ReflectionTex",target);
  }
  void LateUpdate() {
   if(!mirror || Time.frameCount%2!=0)return;
   var source=Camera.main; if(!source)return;
   mirror.CopyFrom(source);mirror.enabled=false;mirror.targetTexture=target;
   mirror.cullingMask=~(1<<4);mirror.depthTextureMode=DepthTextureMode.None;
   var reflection=Matrix4x4.identity;reflection.m11=-1;reflection.m13=.04f;
   mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
   Vector3 p=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,.04f,0));
   Vector3 n=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
   mirror.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(n.x,n.y,n.z,-Vector3.Dot(p,n)));
   GL.invertCulling=true;
   try {mirror.Render();} finally {GL.invertCulling=false;}
  }
  void OnDestroy(){if(target){target.Release();Destroy(target);}if(mirror)Destroy(mirror.gameObject);}
 }
}
