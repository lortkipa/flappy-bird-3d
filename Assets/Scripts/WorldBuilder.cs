using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wildflight {
 public sealed class WorldBuilder {
  readonly System.Random rng = new System.Random(128);
  public Material copper, edge, dark, moss, gold;
  Material bark, needles, rock, earth;
  readonly List<GameObject> scenery = new List<GameObject>();
  public Transform root;
  public float Rand(float a,float b) => a+(float)rng.NextDouble()*(b-a);

  public static Material Mat(string name,Color color,float metallic=0,float smooth=.3f) {
   var m=new Material(Shader.Find("Standard")){name=name,color=color,enableInstancing=true};
   m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);return m;
  }
  public static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Transform parent=null) {
   var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);
   o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;
   Object.Destroy(o.GetComponent<Collider>());return o;
  }
  Texture2D Texture(Color a,Color b,int seed,bool streak=false) {
   int size=256;var t=new Texture2D(size,size,TextureFormat.RGB24,true){name="Procedural weathering",wrapMode=TextureWrapMode.Repeat};
   var pixels=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
    float u=x/(float)size,v=y/(float)size;
    float n=Mathf.PerlinNoise(u*7+seed,v*(streak?2:7)+seed)*.55f+Mathf.PerlinNoise(u*33+seed,v*33)*.3f+Mathf.PerlinNoise(u*115,v*115)*.15f;
    pixels[y*size+x]=Color.Lerp(a,b,Mathf.SmoothStep(.12f,.88f,n));
   }
   t.SetPixels(pixels);t.Apply();return t;
  }
  void Textured(Material m,Color a,Color b,int seed,Vector2 tile,bool streak=false) {
   m.mainTexture=Texture(a,b,seed,streak);m.mainTextureScale=tile;
  }
  public void Build() {
   root=new GameObject("Misty river valley").transform;
   copper=Mat("Oxidized copper",Color.white,.7f,.46f);
   Textured(copper,new Color(.055f,.16f,.13f),new Color(.38f,.25f,.105f),14,new Vector2(3,4),true);
   edge=Mat("Worn bronze edges",new Color(.37f,.24f,.10f),.78f,.51f);
   dark=Mat("Pipe interior",new Color(.023f,.038f,.031f),.35f,.27f);
   moss=Mat("Moss",new Color(.16f,.23f,.045f),0,.2f);
   gold=Mat("Warm brass",new Color(.65f,.42f,.15f),.7f,.6f);
   bark=Mat("Cedar bark",Color.white);
   Textured(bark,new Color(.035f,.046f,.029f),new Color(.18f,.145f,.08f),19,new Vector2(2,6),true);
   needles=new Material(Shader.Find("Wildflight/Foliage")){name="Cedar fronds",enableInstancing=true};
   needles.mainTexture=Resources.Load<Texture2D>("Textures/Cedar");needles.color=new Color(.62f,.82f,.74f);
   rock=Mat("River stone",Color.white,0,.28f);
   Textured(rock,new Color(.09f,.12f,.09f),new Color(.33f,.35f,.26f),71,Vector2.one*2);
   earth=Mat("Forest floor",Color.white);
   Textured(earth,new Color(.04f,.065f,.025f),new Color(.2f,.225f,.085f),31,Vector2.one*9);

   RenderSettings.skybox=new Material(Shader.Find("Wildflight/Atmosphere"));
   RenderSettings.ambientMode=AmbientMode.Trilight;
   RenderSettings.ambientSkyColor=new Color(.40f,.53f,.52f);
   RenderSettings.ambientEquatorColor=new Color(.23f,.32f,.25f);
   RenderSettings.ambientGroundColor=new Color(.11f,.14f,.10f);
   RenderSettings.ambientIntensity=1;
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
   RenderSettings.fogColor=new Color(.36f,.49f,.46f);RenderSettings.fogDensity=.012f;
   var sun=new GameObject("Late afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;
   sun.transform.rotation=Quaternion.Euler(22,-36,0);sun.color=new Color(1,.88f,.68f);sun.intensity=1.65f;
   sun.shadows=LightShadows.Soft;sun.shadowStrength=.7f;sun.shadowBias=.04f;
   RenderSettings.sun=sun;
   var fill=new GameObject("Sky fill").AddComponent<Light>();fill.type=LightType.Directional;
   fill.transform.rotation=Quaternion.Euler(45,155,0);fill.color=new Color(.44f,.72f,.79f);fill.intensity=.6f;

   var waterMat=new Material(Shader.Find("Wildflight/River"));
   var water=Shape("Reflecting river",PrimitiveType.Plane,new Vector3(0,.02f,20),new Vector3(30,1,22),waterMat,root);
   water.layer=4;water.AddComponent<RiverReflection>().water=waterMat;

   Land("Far bank",-105,105,7,85,earth,false);
   Land("Near bank",-105,105,-22,-7,earth,false);
   for(int ridge=0;ridge<4;ridge++) {
    var m=Mat("Distant ridge "+ridge,Color.Lerp(new Color(.13f,.25f,.22f),new Color(.39f,.49f,.42f),ridge/4f));
    Land("Mountain ridge "+ridge,-170,170,60+ridge*28,105+ridge*28,m,true);
   }
   var treeMeshes=new Mesh[5];for(int i=0;i<5;i++)treeMeshes[i]=TreeMesh(i);
   for(int i=0;i<340;i++) {
    float x=Rand(-105,105),z=Rand(15,115);float height=Rand(7,19);
    var tree=new GameObject("Western cedar");tree.transform.SetParent(root,false);
    tree.transform.position=new Vector3(x,Ground(x,z),z);tree.transform.localScale=Vector3.one*height;
    tree.transform.rotation=Quaternion.Euler(0,Rand(0,360),0);
    tree.AddComponent<MeshFilter>().sharedMesh=treeMeshes[i%5];tree.AddComponent<MeshRenderer>().sharedMaterials=new[]{needles,bark};
    if(z>55)tree.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    scenery.Add(tree);
   }
   for(int i=0;i<70;i++) {
    float x=Rand(-95,95),z=Rand(5.5f,10);float s=Rand(.35f,1.6f);
    var stone=Shape("Mossy river stone",PrimitiveType.Sphere,new Vector3(x,.2f,z),new Vector3(s*1.6f,s*.8f,s),rock,root);
    stone.transform.rotation=Quaternion.Euler(Rand(0,40),Rand(0,180),Rand(0,25));
    scenery.Add(stone);
   }
   // Foreground grasses and reeds form a dark, natural frame.
   var grassVerts=new List<Vector3>();var grassTris=new List<int>();
   for(int i=0;i<1200;i++) {
    float x=Rand(-70,70),z=Rand(-12,-7);float y=Ground(x,z);float h=Rand(.25f,1.15f);
    AddTriangle(grassVerts,grassTris,new Vector3(x-.025f,y,z),new Vector3(x+.025f,y,z),new Vector3(x+Rand(-.2f,.2f),y+h,z+.08f));
   }
   MeshObject("Swaying river grasses",grassVerts,grassTris,moss,root);
   for(int i=0;i<30;i++) {
    float x=Rand(-40,40),z=Rand(6,9);float y=.2f;
    Shape("Reed stem",PrimitiveType.Cylinder,new Vector3(x,y+.65f,z),new Vector3(.025f,.65f,.025f),moss,root);
    Shape("Cattail",PrimitiveType.Capsule,new Vector3(x,y+1.3f,z),new Vector3(.09f,.22f,.09f),edge,root);
   }
   Dust();
  }
  public void Scroll(float distance) {
   foreach(var obj in scenery) {
    obj.transform.position+=Vector3.left*distance*.4f;
    if(obj.transform.position.x< -105)obj.transform.position+=Vector3.right*210;
   }
  }
  float Ground(float x,float z) {
   float shore=z>0?Mathf.Clamp01((z-6)/13):Mathf.Clamp01((-z-6)/5);
   return -.2f+shore*(.5f+Mathf.PerlinNoise(x*.035f+14,z*.042f+8)*3.6f);
  }
  void Land(string name,float xmin,float xmax,float zmin,float zmax,Material mat,bool mountain) {
   int nx=100,nz=28;var verts=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();
   for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++) {
    float px=Mathf.Lerp(xmin,xmax,x/(float)nx),pz=Mathf.Lerp(zmin,zmax,z/(float)nz);
    float h=Ground(px,pz);
    if(mountain)h+=(Mathf.Pow(Mathf.PerlinNoise(px*.021f+5,pz*.02f),2)*55+Mathf.PerlinNoise(px*.07f+8,pz*.06f)*8)*Mathf.Sin(z/(float)nz*Mathf.PI);
    verts.Add(new Vector3(px,h,pz));uv.Add(new Vector2(x/(float)nx,z/(float)nz));
    if(z<nz&&x<nx){int a=z*(nx+1)+x;tris.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});}
   }
   var obj=MeshObject(name,verts,tris,mat,root);obj.GetComponent<MeshFilter>().sharedMesh.SetUVs(0,uv);
  }
  static void AddTriangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c) {
   int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);
   t.Add(n+2);t.Add(n+1);t.Add(n);
  }
  public static GameObject MeshObject(string name,List<Vector3> v,List<int> t,Material mat,Transform parent) {
   var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
  }
  Mesh TreeMesh(int variant) {
   var v=new List<Vector3>();var t=new List<int>();var trunk=new List<int>();var uv=new List<Vector2>();
   // Crossed frond cards show individual needles and irregular silhouettes.
   for(int level=0;level<23;level++) {
    float y=.13f+level*.037f, radius=(1-y)*.32f;
    int branches=10;
    for(int b=0;b<branches;b++) {
     float a=b*2*Mathf.PI/branches+level*1.7f+variant;
     var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var tangent=new Vector3(-dir.z,0,dir.x);
     float r=radius*Rand(.68f,1.23f);var tip=dir*r+Vector3.up*(y-.018f);
     var start=Vector3.up*(y+.045f);float w=r*.45f;
     for(int j=0;j<2;j++) {
      var side=j==0?tangent*w:(tangent*.25f+Vector3.up)*w*.8f;
      int n=v.Count;v.Add(start-side);v.Add(start+side);v.Add(tip+side*.68f);v.Add(tip-side*.68f);
      uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});
      t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
     }
    }
   }
   for(int i=0;i<8;i++) {
    float a=i*Mathf.PI*2/8,b=(i+1)*Mathf.PI*2/8;int n=v.Count;
    v.Add(new Vector3(Mathf.Cos(a)*.014f,0,Mathf.Sin(a)*.014f));
    v.Add(new Vector3(Mathf.Cos(b)*.014f,0,Mathf.Sin(b)*.014f));v.Add(Vector3.up);
    uv.AddRange(new[]{new Vector2(i/8f,0),new Vector2((i+1)/8f,0),new Vector2(i/8f,1)});
    trunk.AddRange(new[]{n,n+2,n+1});
   }
   var m=new Mesh{name="Cedar needle mesh",subMeshCount=2};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.SetTriangles(trunk,1);m.RecalculateNormals();return m;
  }
  public GameObject Pipe(float height,bool upper,Transform parent) {
   var p=new GameObject(upper?"Hanging pipe":"Rising pipe");p.transform.SetParent(parent,false);
   float sign=upper?1:-1;
   Shape("Weathered copper barrel",PrimitiveType.Cylinder,new Vector3(0,sign*height*.5f,0),new Vector3(1.85f,height*.5f,1.85f),copper,p.transform);
   // Open flange with an annular top surface and a dark recessed bore.
   var verts=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();int n=64;
   for(int j=0;j<4;j++)for(int i=0;i<=n;i++) {
    float a=i*Mathf.PI*2/n;float r=j<2?1.08f:.77f;float y=(j==0||j==3)?0:sign*.34f;
    verts.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));uv.Add(new Vector2(i/(float)n,j/3f));
   }
   for(int j=0;j<4;j++)for(int i=0;i<n;i++) {
    int a=j*(n+1)+i,b=((j+1)%4)*(n+1)+i;
    if(upper)tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});
    else tris.AddRange(new[]{a+1,b,a,b+1,b,a+1});
   }
   var flange=MeshObject("Cast flange",verts,tris,edge,p.transform);flange.GetComponent<MeshFilter>().sharedMesh.SetUVs(0,uv);
   Shape("Recessed opening",PrimitiveType.Cylinder,new Vector3(0,sign*.36f,0),new Vector3(1.56f,.01f,1.56f),dark,p.transform);
   for(int i=0;i<12;i++) {
    float a=i*Mathf.PI*2/12;
    Shape("Flange bolt",PrimitiveType.Cylinder,new Vector3(Mathf.Cos(a)*.95f,-sign*.035f,Mathf.Sin(a)*.95f),new Vector3(.10f,.04f,.10f),gold,p.transform);
   }
   for(float y=1.8f;y<height;y+=2.7f)
    Shape("Joint collar",PrimitiveType.Cylinder,new Vector3(0,sign*y,0),new Vector3(1.94f,.065f,1.94f),edge,p.transform);
   return p;
  }
  void Dust() {
   var go=new GameObject("Sunlit pollen");go.transform.position=new Vector3(0,5,2);
   var ps=go.AddComponent<ParticleSystem>();var main=ps.main;
   main.startLifetime=14;main.startSpeed=.12f;main.startSize=.026f;main.maxParticles=120;
   main.startColor=new Color(1,.86f,.53f,.45f);main.simulationSpace=ParticleSystemSimulationSpace.World;
   var emission=ps.emission;emission.rateOverTime=8;
   var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(35,12,10);
   var noise=ps.noise;noise.enabled=true;noise.strength=.2f;noise.frequency=.2f;
   var mat=new Material(Shader.Find("Particles/Standard Unlit"));mat.SetColor("_Color",new Color(1,.84f,.5f,.45f));
   go.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;
  }
 }
}
