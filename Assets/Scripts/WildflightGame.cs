using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wildflight {
 public sealed class WildflightGame : MonoBehaviour {
  public enum Mode { Ready, Flying, Paused, Crashed }
  public Mode State {get; private set;}
  public readonly FlightModel Flight=new FlightModel();
  sealed class Gate {public Transform root;public float center;public bool passed;}
  readonly List<Gate> gates=new List<Gate>();
  WorldBuilder world;Transform bird,nearWing,farWing;Quaternion nearRest,farRest;
  FlightAudio sound;Camera cam;Font sans;
  int best;float accumulator,wingKick,deathTime,scorePulse;
  Vector3 birdPosition;System.Random random=new System.Random();
  const float BirdX=-4,HalfGap=2.15f,Spacing=10.5f;
  Color cream=new Color(.94f,.95f,.85f),accent=new Color(.81f,.9f,.47f);
  Texture2D white;GUIStyle label;bool styles;
  readonly Dictionary<int,GUIStyle> textStyles=new Dictionary<int,GUIStyle>();
  bool smoke;string captureDir;

  void Start() {
   Application.targetFrameRate=120;QualitySettings.vSyncCount=1;
   QualitySettings.antiAliasing=4;QualitySettings.shadowDistance=75;QualitySettings.shadows=ShadowQuality.All;
   QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowCascades=4;
   cam=new GameObject("Wildflight camera").AddComponent<Camera>();cam.tag="MainCamera";
   cam.transform.position=new Vector3(0,6,-24);cam.transform.LookAt(new Vector3(1,4.5f,0));
   cam.fieldOfView=43;cam.nearClipPlane=.1f;cam.farClipPlane=320;cam.allowHDR=true;
   cam.depthTextureMode=DepthTextureMode.Depth;cam.gameObject.AddComponent<AudioListener>();cam.gameObject.AddComponent<Cinema>();
   world=new WorldBuilder();world.Build();
   bird=new GameObject("Player hummingbird").transform;
   var model=Resources.Load<GameObject>("Models/Hummingbird");
   if(!model)throw new InvalidOperationException("Missing Blender hummingbird FBX. Run tools/create_bird.py.");
   var visual=Instantiate(model,bird);visual.name="Blender hummingbird";visual.transform.localScale*=1.08f;
   foreach(var t in visual.GetComponentsInChildren<Transform>()) {
    if(t.name=="WingNear")nearWing=t;if(t.name=="WingFar")farWing=t;
   }
   if(nearWing)nearRest=nearWing.localRotation;if(farWing)farRest=farWing.localRotation;
   foreach(var r in visual.GetComponentsInChildren<Renderer>())foreach(var mat in r.materials) {
    Color c=mat.color;mat.shader=Shader.Find("Standard");mat.color=c;
    bool eye=mat.name.Contains("Obsidian"),feather=mat.name.Contains("emerald")||mat.name.Contains("Jade")||mat.name.Contains("ruby");
    mat.SetFloat("_Glossiness",eye?.92f:feather?.52f:.32f);mat.SetFloat("_Metallic",feather?.48f:.1f);
    mat.SetInt("_Cull",0);
   }
   var light=new GameObject("Bird rim light").AddComponent<Light>();light.type=LightType.Point;
   light.transform.SetParent(bird,false);light.transform.localPosition=new Vector3(-1,2,-2);light.color=new Color(1,.8f,.46f);light.intensity=2;light.range=5;
   for(int i=0;i<5;i++)CreateGate(10+i*Spacing,5.2f+(i==0?0:Mathf.Sin(i*1.7f)*1.3f));
   sound=gameObject.AddComponent<FlightAudio>();best=PlayerPrefs.GetInt("WildflightBest",0);
   sans=Resources.Load<Font>("Fonts/Interface");
   bird.position=new Vector3(2.8f,5.2f,0);birdPosition=bird.position;
   sans.RequestCharactersInTexture("PlayPause0123456789",20);
   sans.RequestCharactersInTexture("0123456789",48);
   var args=Environment.GetCommandLineArgs();smoke=Array.IndexOf(args,"--smoke-test")>=0;
   int ci=Array.IndexOf(args,"--capture-dir");captureDir=ci>=0&&ci+1<args.Length?args[ci+1]:Path.Combine(Application.dataPath,"../Captures");
   if(smoke)StartCoroutine(SmokeTest());
  }
  void CreateGate(float x,float center) {
   var root=new GameObject("Copper passage").transform;root.position=new Vector3(x,0,0);
   var lower=world.Pipe(10,false,root);lower.transform.localPosition=Vector3.up*(center-HalfGap);
   var upper=world.Pipe(12,true,root);upper.transform.localPosition=Vector3.up*(center+HalfGap);
   gates.Add(new Gate{root=root,center=center});
  }
  void PositionGate(Gate gate,float x,float center) {
   gate.root.position=new Vector3(x,0,0);gate.center=center;gate.passed=false;
   gate.root.GetChild(0).localPosition=Vector3.up*(center-HalfGap);
   gate.root.GetChild(1).localPosition=Vector3.up*(center+HalfGap);
  }
  public void Begin() {
   Flight.Reset();State=Mode.Flying;accumulator=0;scorePulse=0;
   for(int i=0;i<gates.Count;i++)PositionGate(gates[i],10+i*Spacing,5.2f+(i==0?0:Mathf.Sin(i*1.7f)*1.3f));
   bird.position=new Vector3(BirdX,5.2f,0);birdPosition=bird.position;Flap();
  }
  void Flap(){Flight.Flap();wingKick=1;if(sound)sound.Flap();}
  void Pause(){if(State==Mode.Flying)State=Mode.Paused;else if(State==Mode.Paused)State=Mode.Flying;}
  void Update() {
   // Preserve the horizontal play area when a phone rotates into portrait.
   cam.fieldOfView=2*Mathf.Atan(Mathf.Tan(21.5f*Mathf.Deg2Rad)*Mathf.Max(1,(4f/3f)/cam.aspect))*Mathf.Rad2Deg;
   if(!smoke) {
    if(Input.GetKeyDown(KeyCode.M))sound.Toggle();
    bool toggledPause=Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P);
    if(toggledPause)Pause();
    if(!toggledPause) {
     bool pressed=Input.GetKeyDown(KeyCode.Space);
     if(State==Mode.Flying) {
      if(Input.touchCount==0&&Input.GetMouseButtonDown(0)&&!OverPause(Input.mousePosition))pressed=true;
      if(Input.touchCount>0&&Input.GetTouch(0).phase==TouchPhase.Began&&!OverPause(Input.GetTouch(0).position))pressed=true;
     }
     if(pressed){if(State==Mode.Ready)Begin();else if(State==Mode.Flying)Flap();else if(State==Mode.Paused)Pause();else if(CanReplay())Begin();}
     if(Input.GetKeyDown(KeyCode.R)&&CanReplay())Begin();
    }
   }
   float dt=Mathf.Min(Time.deltaTime,.05f);
   if(State==Mode.Flying) {
    accumulator+=dt;
    while(accumulator>=1f/120f&&State==Mode.Flying){Simulate(1f/120f);accumulator-=1f/120f;}
   }
   if(State!=Mode.Paused) {
    bird.localScale=Vector3.one*(State==Mode.Ready?1.5f:1);
    wingKick=Mathf.MoveTowards(wingKick,0,dt*3);
    float phase=Time.time*(State==Mode.Flying?38:27);
    float angle=Mathf.Sin(phase)*(32+wingKick*24);
    if(State==Mode.Crashed)angle=Mathf.Lerp(angle,-32,Mathf.Clamp01((Time.unscaledTime-deathTime)*3));
    if(nearWing)nearWing.localRotation=nearRest*Quaternion.Euler(angle,0,0);
    if(farWing)farWing.localRotation=farRest*Quaternion.Euler(-angle,0,0);
    float y=State==Mode.Ready?5.2f+Mathf.Sin(Time.time*1.9f)*.13f:Flight.Y;
    if(State==Mode.Crashed)y=Mathf.Max(.6f,Flight.Y-Mathf.Pow(Time.unscaledTime-deathTime,2)*3);
    birdPosition=new Vector3(State==Mode.Ready?2.8f:BirdX,y,0);
    bird.position=Vector3.Lerp(bird.position,birdPosition,1-Mathf.Exp(-dt*24));
    float tilt=State==Mode.Ready?6:State==Mode.Crashed?-65:Mathf.Clamp(Flight.Velocity*4,-50,23);
    bird.rotation=Quaternion.Slerp(bird.rotation,Quaternion.Euler(0,-8,tilt),1-Mathf.Exp(-dt*9));
   }
   scorePulse=Mathf.MoveTowards(scorePulse,0,dt*2);
  }
  void Simulate(float dt) {
   float speed=Flight.Speed;Flight.Step(dt);world.Scroll(speed*dt);
   foreach(var gate in gates) {
    gate.root.position+=Vector3.left*(speed*dt);
    if(Flight.HitsPipe(gate.root.position.x,BirdX,gate.center,HalfGap))Flight.Crash();
    if(!gate.passed&&gate.root.position.x+1.08f< BirdX-FlightModel.Radius) {
     gate.passed=true;Flight.Pass();if(Flight.Alive){scorePulse=1;sound.Score();}
    }
    if(gate.root.position.x< -21) {
     float right=-100,previous=5.2f;
     foreach(var g in gates)if(g.root.position.x>right){right=g.root.position.x;previous=g.center;}
     float center=Mathf.Clamp(previous+(float)(random.NextDouble()*2.8-1.4),3.35f,8.1f);
     PositionGate(gate,right+Spacing,center);
    }
   }
   if(!Flight.Alive)Crash();
  }
  void Crash() {
   State=Mode.Crashed;deathTime=Time.unscaledTime;sound.Hit();
   if(Flight.Score>best){best=Flight.Score;if(!smoke){PlayerPrefs.SetInt("WildflightBest",best);PlayerPrefs.Save();}}
  }
  void OnApplicationFocus(bool focus){if(!focus&&State==Mode.Flying&&!smoke)State=Mode.Paused;}
  void OnApplicationPause(bool paused){if(paused&&State==Mode.Flying&&!smoke)State=Mode.Paused;}
  bool CanReplay()=>State==Mode.Crashed&&Time.unscaledTime-deathTime>.35f;
  float UiScale()=>Mathf.Clamp(Screen.height/900f,.9f,2f);
  Rect UiBounds() {
   float s=UiScale();Rect safe=Screen.safeArea;
   return new Rect(safe.x/s,(Screen.height-safe.yMax)/s,safe.width/s,safe.height/s);
  }
  Rect PauseRect(){Rect bounds=UiBounds();return new Rect(bounds.xMax-112,bounds.y+20,92,52);}
  bool OverPause(Vector2 position)=>PauseRect().Contains(new Vector2(position.x,Screen.height-position.y)/UiScale());
  void InitStyles() {
   if(styles)return;styles=true;white=Texture2D.whiteTexture;
   label=new GUIStyle(GUI.skin.label){font=sans,richText=false,wordWrap=false,padding=new RectOffset(0,0,0,0)};
  }
  void Box(Rect rect,Color color) {GUI.color=color;GUI.DrawTexture(rect,white);GUI.color=Color.white;}
  void Text(string text,Rect rect,int size,Color color) {
   if(!textStyles.TryGetValue(size,out var style)) {
    style=new GUIStyle(label){fontSize=size,alignment=TextAnchor.MiddleCenter};textStyles.Add(size,style);
   }
   style.normal.textColor=color;GUI.Label(rect,text,style);
  }
  bool Button(string text,Rect rect,bool primary=false) {
   bool hover=rect.Contains(Event.current.mousePosition);
   Box(rect,primary?(hover?new Color(.9f,.97f,.65f):accent):new Color(.035f,.075f,.055f,hover?.95f:.82f));
   Text(text,rect,20,primary?new Color(.09f,.15f,.1f):cream);
   return GUI.Button(rect,GUIContent.none,GUIStyle.none);
  }
  void OnGUI() {
   if(!bird)return;InitStyles();float s=UiScale();
   Matrix4x4 previous=GUI.matrix;
   GUI.matrix=Matrix4x4.Scale(new Vector3(s,s,1));
   Rect bounds=UiBounds();
   var scoreRect=new Rect(bounds.center.x-64,bounds.y+16,128,60);
   var shadowRect=scoreRect;shadowRect.position+=new Vector2(1,2);
   Text(Flight.Score.ToString(),shadowRect,48,new Color(0,0,0,.65f));
   Text(Flight.Score.ToString(),scoreRect,48,Color.Lerp(cream,accent,scorePulse));
   if(State==Mode.Flying) {
    if(Button("Pause",PauseRect()))Pause();
   } else if(State==Mode.Ready||State==Mode.Paused||CanReplay()) {
    var playRect=new Rect(bounds.center.x-64,bounds.center.y-30,128,60);
    if(Button("Play",playRect,true)){if(State==Mode.Paused)Pause();else Begin();}
   }
   GUI.matrix=previous;
  }
  IEnumerator Capture(string name) {
   yield return new WaitForEndOfFrame();
   var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();
   File.WriteAllBytes(Path.Combine(captureDir,name+".png"),tex.EncodeToPNG());Destroy(tex);
  }
  IEnumerator SmokeTest() {
   Directory.CreateDirectory(captureDir);
   yield return new WaitForSeconds(2);yield return Capture("01-title");
   Begin();float start=Time.time;
   while(Flight.Score<3&&State==Mode.Flying&&Time.time-start<24) {
    Gate next=null;foreach(var g in gates)if(g.root.position.x>BirdX-1.36f&&(next==null||g.root.position.x<next.root.position.x))next=g;
    float target=next==null?5.2f:next.center;
    if(Flight.Y<target-.3f&&Flight.Velocity<0)Flap();
    yield return null;
   }
   bool scoring=Flight.Score>=3;
   yield return Capture("02-flight");
   Pause();float y=Flight.Y,distance=Flight.Distance;yield return new WaitForSeconds(.3f);
   bool pause=State==Mode.Paused&&Flight.Y==y&&Flight.Distance==distance;
   yield return Capture("03-pause");Pause();
   yield return new WaitForSeconds(3);bool death=State==Mode.Crashed;
   yield return Capture("04-retry");
   Begin();bool restart=State==Mode.Flying&&Flight.Score==0&&Flight.Alive;
   string result="{\"scoring\":"+scoring.ToString().ToLower()+",\"pause\":"+pause.ToString().ToLower()+",\"collision\":"+death.ToString().ToLower()+",\"restart\":"+restart.ToString().ToLower()+",\"birdImported\":"+(nearWing&&farWing?"true":"false")+"}";
   File.WriteAllText(Path.Combine(captureDir,"smoke-test.json"),result);Debug.Log("WILDFLIGHT_SMOKE "+result);
   Application.Quit(scoring&&pause&&death&&restart?0:2);
  }
 }
}
