using System.Collections.Generic;
using UnityEngine;

public sealed class HGTemporaryNoclipFlight : MonoBehaviour
{
    private readonly Dictionary<Collider2D, bool> states = new Dictionary<Collider2D, bool>();
    private Body body;
    private Rigidbody2D rootRb;
    private float startedAt;
    private float endAt;

    public static void Launch(Body target, Vector2 velocity)
    {
        if (target == null) return;
        HGTemporaryNoclipFlight flight = target.GetComponent<HGTemporaryNoclipFlight>();
        if (flight == null) flight = target.gameObject.AddComponent<HGTemporaryNoclipFlight>();
        flight.Begin(target, velocity);
    }

    public static void LaunchRigidbody(GameObject target, Rigidbody2D rigidbody, Vector2 velocity)
    {
        if (target == null || rigidbody == null) return;
        HGTemporaryNoclipFlight flight = target.GetComponent<HGTemporaryNoclipFlight>();
        if (flight == null) flight = target.AddComponent<HGTemporaryNoclipFlight>();
        flight.BeginRigidbody(rigidbody, velocity);
    }

    private void Begin(Body target, Vector2 velocity)
    {
        Restore();
        body = target;
        rootRb = target.rb;
        DisableColliders(target.gameObject);
        ApplyBodyVelocity(target, velocity);
        startedAt = Time.time;
        endAt = Time.time + 2.75f;
    }

    private void BeginRigidbody(Rigidbody2D rigidbody, Vector2 velocity)
    {
        Restore();
        body = null;
        rootRb = rigidbody;
        DisableColliders(gameObject);
        rigidbody.bodyType = RigidbodyType2D.Dynamic;
        rigidbody.velocity = velocity;
        startedAt = Time.time;
        endAt = Time.time + 2.75f;
    }

    private static void ApplyBodyVelocity(Body target, Vector2 velocity)
    {
        if (target.rb != null)
        {
            target.rb.velocity = velocity;
            target.rb.angularVelocity = 0f;
        }
        if (target.limbs != null)
        {
            for (int i = 0; i < target.limbs.Length; i++)
            {
                Limb limb = target.limbs[i];
                if (limb != null && limb.rb != null)
                    limb.rb.velocity = velocity + UnityEngine.Random.insideUnitCircle * 5f;
            }
        }
    }

    private void DisableColliders(GameObject root)
    {
        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null || states.ContainsKey(c)) continue;
            states[c] = c.enabled;
            c.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        float speed = MaxSpeed();
        if (Time.time >= endAt || (Time.time - startedAt > 0.62f && speed < 11f))
        {
            Restore();
            Destroy(this);
        }
    }

    private float MaxSpeed()
    {
        float speed = rootRb != null ? rootRb.velocity.magnitude : 0f;
        if (body != null && body.limbs != null)
        {
            for (int i = 0; i < body.limbs.Length; i++)
            {
                Limb limb = body.limbs[i];
                if (limb != null && limb.rb != null) speed = Mathf.Max(speed, limb.rb.velocity.magnitude);
            }
        }
        return speed;
    }

    private void Restore()
    {
        foreach (KeyValuePair<Collider2D, bool> pair in states)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        states.Clear();
    }

    private void OnDisable() { Restore(); }
    private void OnDestroy() { Restore(); }
}
