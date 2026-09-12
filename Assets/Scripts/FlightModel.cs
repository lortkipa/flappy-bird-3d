using System;

namespace Wildflight {
 // Pure simulation shared by the game and deterministic build-time checks.
 public sealed class FlightModel {
  public const float Gravity = -15f, Impulse = 5.6f, Radius = .28f;
  public float Y {get; private set;} = 5.2f;
  public float Velocity {get; private set;}
  public float Distance {get; private set;}
  public int Score {get; private set;}
  public bool Alive {get; private set;} = true;
  public float Speed => Math.Min(6.9f,4.1f+Score*.07f);
  public void Reset(){Y=5.2f;Velocity=0;Distance=0;Score=0;Alive=true;}
  public void Flap(){if(Alive)Velocity=Impulse;}
  public void Step(float dt) {
   if(!Alive)return;
   Y+=Velocity*dt+.5f*Gravity*dt*dt;Velocity+=Gravity*dt;Distance+=Speed*dt;
   if(Y<.65f+Radius||Y>11.5f-Radius)Alive=false;
  }
  public bool HitsPipe(float pipeX,float birdX,float gapCenter,float halfGap) {
   return Math.Abs(pipeX-birdX)<1.08f+Radius && (Y-Radius<gapCenter-halfGap||Y+Radius>gapCenter+halfGap);
  }
  public void Crash(){Alive=false;}
  public void Pass(){if(Alive)Score++;}
 }
}
