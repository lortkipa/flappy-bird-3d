using UnityEngine;

namespace Wildflight {
 public sealed class FlightAudio : MonoBehaviour {
  AudioSource effects, ambience, music;
  WildflightGame game;
  AudioClip flap, score, hit;
  public bool Muted {get; private set;}
  void Awake() {
   effects=gameObject.AddComponent<AudioSource>();effects.volume=.32f;
   ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.volume=.19f;
   game=GetComponent<WildflightGame>();
   music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;
   music.spatialBlend=0;music.volume=0;music.priority=64;
   music.clip=Resources.Load<AudioClip>("Audio/RiverRun");
   flap=Tone("Wingbeat",.13f,180,65,.65f);
   score=Tone("Clear",.3f,880,1320,.02f);
   hit=Tone("Impact",.3f,120,35,.65f);
   int n=44100*8;var samples=new float[n];var rng=new System.Random(44);float smooth=0;
   for(int i=0;i<n;i++) {
    smooth=Mathf.Lerp(smooth,(float)rng.NextDouble()*2-1,.035f);
    float t=i/44100f;
    float chirp=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*1.57f)),18)*Mathf.Sin(t*7200+Mathf.Sin(t*31)*15)*.07f;
    samples[i]=smooth*.65f+chirp;
   }
   var clip=AudioClip.Create("River breeze and birds",n,1,44100,false);clip.SetData(samples,0);ambience.clip=clip;ambience.Play();
   Muted=PlayerPrefs.GetInt("WildflightMuted",0)==1;ApplyMute();
   if(music.clip)music.Play();else Debug.LogError("Missing River Run music. Run tools/create_music.py.");
  }
  void Update() {
   float target=game&&game.State==WildflightGame.Mode.Flying?.48f:
    game&&game.State==WildflightGame.Mode.Paused?.12f:
    game&&game.State==WildflightGame.Mode.Crashed?.2f:.3f;
   music.volume=Mathf.MoveTowards(music.volume,target,Time.unscaledDeltaTime*.5f);
  }
  AudioClip Tone(string name,float seconds,float start,float end,float noise) {
   int n=(int)(44100*seconds);var data=new float[n];var rng=new System.Random(72);float phase=0;
   for(int i=0;i<n;i++) {
    float f=i/(float)n;phase+=Mathf.Lerp(start,end,f)*Mathf.PI*2/44100;
    data[i]=(Mathf.Sin(phase)*(1-noise)+((float)rng.NextDouble()*2-1)*noise)*Mathf.Sin(Mathf.PI*f)*Mathf.Pow(1-f,2);
   }
   var clip=AudioClip.Create(name,n,1,44100,false);clip.SetData(data,0);return clip;
  }
  void ApplyMute(){effects.mute=Muted;ambience.mute=Muted;music.mute=Muted;}
  public void Toggle(){Muted=!Muted;ApplyMute();PlayerPrefs.SetInt("WildflightMuted",Muted?1:0);PlayerPrefs.Save();}
  public void Flap()=>effects.PlayOneShot(flap);
  public void Score()=>effects.PlayOneShot(score);
  public void Hit()=>effects.PlayOneShot(hit);
 }
}
