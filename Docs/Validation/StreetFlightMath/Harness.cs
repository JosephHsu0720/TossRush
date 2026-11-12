// Portable checks for the actual flight-model source. These substitutes implement only
// its math dependencies; this is NOT a substitute for Unity Rigidbody/rolling tests.
using System;
using TossRush;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();
        public float magnitude=>MathF.Sqrt(x*x+y*y+z*z);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator -(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 ClampMagnitude(Vector3 a,float max)=>a.magnitude>max?a*(max/a.magnitude):a;
    }
    public static class Physics { public static Vector3 gravity=>new Vector3(0,-9.81f,0); }
    public static class Mathf { public static float Max(float a,float b)=>MathF.Max(a,b); }
}
namespace TossRush
{
    public sealed class ProjectilePhysicsProfile
    {
        public float mass=.8f,referenceArea=.035f,dragCoefficient=.8f,spinLift=.025f,maxSpinAcceleration=6,angularDamping=.3f;
        public Vector3 initialSpin=new Vector3(0,12,0);
        public static implicit operator bool(ProjectilePhysicsProfile p)=>p!=null;
        public static bool operator !(ProjectilePhysicsProfile p)=>p==null;
    }
}
public static class Harness
{
    static int checks;
    static void Require(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static float Error(ProjectilePhysicsProfile profile,float yaw,float elevation,float targetX,float targetZ,Vector3 wind)
    {
        const float degrees=MathF.PI/180;
        float y=yaw*degrees,e=elevation*degrees,pitch=(38-elevation)*degrees;
        var position=new Vector3(MathF.Sin(y)*MathF.Cos(pitch)*.5f,1.65f-MathF.Sin(pitch)*.5f,-6+MathF.Cos(y)*MathF.Cos(pitch)*.5f);
        var velocity=new Vector3(MathF.Sin(y)*MathF.Cos(e),MathF.Sin(e),MathF.Cos(y)*MathF.Cos(e))*11.8f;
        Vector3 spin=profile.initialSpin;
        for(int step=0;step<250;step++){
            Vector3 before=position;
            ProjectileFlight.Step(ref position,ref velocity,ref spin,profile,wind,1,.02f);
            if(before.y>1.5f&&position.y<=1.5f){
                float t=(before.y-1.5f)/(before.y-position.y);Vector3 cross=before+(position-before)*t;
                return MathF.Max(MathF.Abs(cross.x-targetX)/.34f,MathF.Abs(cross.z-targetZ)/.24f);
            }
        }
        return float.MaxValue;
    }
    public static void Main()
    {
        var p=new ProjectilePhysicsProfile();var forward=new Vector3(0,0,10);
        Vector3 right=ProjectileFlight.Acceleration(p,forward,Vector3.zero,1,p.initialSpin);
        Vector3 left=ProjectileFlight.Acceleration(p,forward,Vector3.zero,1,-p.initialSpin);
        Require(right.x>2.9f&&left.x< -2.9f,"Opposite spin must produce opposite visible curve.");
        Require(MathF.Abs(right.y+9.81f)<.00001f,"Vertical spin must not cancel gravity.");
        Vector3 bounded=ProjectileFlight.Acceleration(p,new Vector3(0,0,100),Vector3.zero,1,new Vector3(0,1000,0));
        Require(MathF.Abs(bounded.x)<=6.0001f,"Spin acceleration must remain bounded.");
        p.spinLift=0;
        Require((ProjectileFlight.Acceleration(p,forward,Vector3.zero,1,p.initialSpin)-ProjectileFlight.Acceleration(p,forward,Vector3.zero,1,Vector3.zero)).magnitude<.000001f,"Existing profiles with zero spin lift must retain ordinary flight.");
        p.spinLift=.025f;
        Vector3 pos=Vector3.zero,vel=forward,spin=p.initialSpin;
        for(int i=0;i<50;i++)ProjectileFlight.Step(ref pos,ref vel,ref spin,p,Vector3.zero,1,.02f);
        Require(pos.x>.8f&&spin.y<9,"Curve must be visible while angular damping reduces spin.");
        int scenarios=0;float worst=0;
        foreach(float x in new[]{-.55f,.55f})foreach(float z in new[]{1.1f,1.4f})foreach(float side in new[]{-1f,1f})foreach(float speed in new[]{.4f,1.8f})foreach(float spread in new[]{-15f,15f}){
            float radians=spread*MathF.PI/180;
            var wind=new Vector3(MathF.Cos(radians)*side*speed,0,MathF.Sin(radians)*side*speed);
            float best=float.MaxValue;
            for(float yaw=-38;yaw<=38;yaw++)for(float elevation=8;elevation<=63;elevation++)best=MathF.Min(best,Error(p,yaw,elevation,x,z,wind));
            Require(best<1,"No player-reachable direct arc for a street variation: "+x+", "+z+", wind="+side*speed+", error="+best);
            scenarios++;worst=MathF.Max(worst,best);
        }
        Console.WriteLine("PASS "+checks+" checks, including "+scenarios+" extreme street target/wind scenarios. Worst normalized opening error="+worst.ToString("0.000"));
        Console.WriteLine("Scope: shared flight math only; no Unity collision, lid roll, car movement or rendered UI validation.");
    }
}
