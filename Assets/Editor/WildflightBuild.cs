using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Wildflight.Editor {
 public static class WildflightBuild {
  public static void InspectBird() {
   var bird=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Hummingbird"));
   foreach(var t in bird.GetComponentsInChildren<Transform>())Debug.Log("BIRD_PART "+t.name+" pos="+t.position+" scale="+t.lossyScale);
   foreach(var r in bird.GetComponentsInChildren<Renderer>())Debug.Log("BIRD_BOUNDS "+r.name+" "+r.bounds);
  }
  [MenuItem("Wildflight/Prepare game")]
  public static void Prepare() {
   AssetDatabase.Refresh();
   PlayerSettings.companyName="Wildflight Studio";PlayerSettings.productName="Wildflight";
   PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
   PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
   PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
   PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64,false);
   PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore});
   var graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
   if(graphics.Length>0) {
    var so=new SerializedObject(graphics[0]);var shaders=so.FindProperty("m_AlwaysIncludedShaders");
    foreach(string name in new[]{"Wildflight/River","Wildflight/Atmosphere","Wildflight/Foliage","Hidden/Wildflight/Film","Standard","Particles/Standard Unlit"}) {
     var shader=Shader.Find(name);if(!shader)throw new Exception("Missing shader: "+name);
     bool exists=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
     if(!exists){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;}
    }
   so.ApplyModifiedPropertiesWithoutUndo();
   }
   var texture=AssetImporter.GetAtPath("Assets/Resources/Textures/Cedar.png") as TextureImporter;
   if(texture){texture.alphaIsTransparency=true;texture.mipmapEnabled=true;texture.SaveAndReimport();}
   var importer=AssetImporter.GetAtPath("Assets/Resources/Models/Hummingbird.fbx") as ModelImporter;
   if(importer) {
    importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
    importer.importAnimation=false;importer.globalScale=1;importer.bakeAxisConversion=true;importer.SaveAndReimport();
   }
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.012f;
   RenderSettings.fogColor=new Color(.36f,.49f,.46f);
   new GameObject("Wildflight — press Play").AddComponent<WildflightGame>();
   EditorSceneManager.SaveScene(scene,"Assets/Scenes/Riverlands.unity");
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Riverlands.unity",true)};
   AssetDatabase.SaveAssets();Debug.Log("WILDFLIGHT_PREPARED");
  }
  [MenuItem("Wildflight/Run simulation checks")]
  public static void Check() {
   var f=new FlightModel();f.Flap();f.Step(.1f);
   Require(Math.Abs(f.Y-5.685f)<.0001f,"Ballistic flap trajectory");
   Require(!f.HitsPipe(-4,-4,5.2f,2.15f),"Clear gap must be safe");
   Require(f.HitsPipe(-4,-4,9,2.15f),"Flange overlap must collide");
   Require(!f.HitsPipe(2,-4,9,2.15f),"Distant pipe must be safe");
   f.Pass();Require(f.Score==1,"Pass increments score");
   f.Crash();f.Pass();Require(f.Score==1,"Dead bird cannot score");
   f.Reset();Require(f.Alive&&f.Score==0&&f.Distance==0,"Restart clears simulation");
   for(int i=0;i<300;i++)f.Step(1f/120);
   Require(!f.Alive,"River collision ends flight");
   var a=new FlightModel();var b=new FlightModel();a.Flap();b.Flap();
   for(int i=0;i<60;i++)a.Step(1f/120);for(int i=0;i<30;i++)b.Step(1f/60);
   Require(Math.Abs(a.Y-b.Y)<.0001f,"Frame rate independent trajectory");
   Debug.Log("WILDFLIGHT_CHECKS_PASSED: 9 simulation checks");
  }
  static void Require(bool pass,string name){if(!pass)throw new Exception("FAILED: "+name);}
  [MenuItem("Wildflight/Build Linux player")]
  public static void BuildLinux() {
   Prepare();Check();Directory.CreateDirectory("Builds/Linux");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
    scenes=new[]{"Assets/Scenes/Riverlands.unity"},locationPathName="Builds/Linux/Wildflight.x86_64",
    target=BuildTarget.StandaloneLinux64,options=BuildOptions.None
   });
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
   Debug.Log("WILDFLIGHT_BUILD_SUCCEEDED "+report.summary.totalSize);
  }
  [MenuItem("Wildflight/Build WebGL player")]
  public static void BuildWebGL() {
   Prepare();Check();Directory.CreateDirectory("Builds/WebGL");
   PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
   PlayerSettings.WebGL.decompressionFallback=false;
   PlayerSettings.WebGL.dataCaching=true;
   PlayerSettings.WebGL.template="PROJECT:Wildflight";
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
    scenes=new[]{"Assets/Scenes/Riverlands.unity"},locationPathName="Builds/WebGL",
    target=BuildTarget.WebGL,options=BuildOptions.None
   });
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
   Debug.Log("WILDFLIGHT_WEBGL_BUILD_SUCCEEDED "+report.summary.totalSize);
  }
 }
}
