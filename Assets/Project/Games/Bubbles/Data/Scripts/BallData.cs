using System;
using UnityEngine;

namespace BubblesGame.Data
{
    [Serializable]
    public class BallData
    {
        [SerializeField] public float launch_time;
        [SerializeField] public Vector2Serializable launch_coord;
        [SerializeField] public float speed;
        [SerializeField] public ColorSerializable color;
        [SerializeField] public float size;
        [SerializeField] public float direction;
        [SerializeField] public float mature_time;
        [SerializeField] public float destroy_time;
        [SerializeField] public int[] hit_landmarks;
        [SerializeField] public int[] destroy_landmarks;

        [SerializeField] public string object_type;

        [SerializeField] public string collision_policy;

        [SerializeField] public int ball_id;

        public BallData() { }

        public float LaunchTime => launch_time;
        public Vector2 LaunchCoord => new Vector2(launch_coord.x, launch_coord.y);
        public float Speed => speed;
        public Color Color => new Color(color.r, color.g, color.b, color.a);
        public float Size => size;
        public float Direction => direction;
        public float MatureTime => mature_time;
        public float DestroyTime => destroy_time;
        public int[] HitLandmarks => hit_landmarks;
        public int[] DestroyLandmarks => destroy_landmarks;

        public string ObjectType => object_type;
        public string CollisionPolicy => collision_policy;
        public int BallId => ball_id;

        // Helper properties para tipos de super balls
        public bool IsHitSuper => object_type == "hit_super";
        public bool IsAvoidSuper => object_type == "avoid_super";
        public bool IsHitAllSuper => object_type == "hit_all_super";
        public bool IsDestroyAllSuper => object_type == "destroy_all_super";
        public bool IsSuperBall => IsHitSuper || IsAvoidSuper || IsHitAllSuper || IsDestroyAllSuper;
    }

    [Serializable]
    public class Vector2Serializable
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class ColorSerializable
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }
}
