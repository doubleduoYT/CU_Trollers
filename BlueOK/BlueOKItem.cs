using System;
using System.Collections;
using UnityEngine;

public sealed class HGBlueOKItem : MonoBehaviour
{
    public const string ItemId = "blueok";
    private const float ThrowArmSpeed = 7.0f;
    private const float MinImpactSpeed = 4.3f;
    private const float BlastRadius = 9.5f;
    private const float LaunchSpeed = 82f;

    private Item item;
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sprite;
    private AudioSource audioSource;
    private AudioClip boom;
    private Transform lastParent;
    private float releasedAt;
    private bool armed;
    private bool exploded;

    private void Awake()
    {
        item = GetComponent<Item>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        lastParent = transform.parent;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 34f;
        boom = Resources.Load<AudioClip>("sounds/blueok_boom");
    }

    private void Start()
    {
        if (WorldGeneration.world != null)
            audioSource.outputAudioMixerGroup = WorldGeneration.world.soundMixerGroup;
    }

    private void Update()
    {
        Transform parent = transform.parent;
        if (lastParent != null && parent == null)
        {
            releasedAt = Time.time;
            armed = rb != null && rb.velocity.magnitude >= ThrowArmSpeed;
        }
        lastParent = parent;

        if (!armed && parent == null && releasedAt > 0f && Time.time - releasedAt <= 0.24f &&
            rb != null && rb.velocity.magnitude >= ThrowArmSpeed)
            armed = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (exploded || !armed || transform.parent != null || collision == null) return;
        if (collision.relativeVelocity.magnitude < MinImpactSpeed) return;
        Collider2D other = collision.collider;
        if (other == null) return;
        int ground = LayerMask.NameToLayer("Ground");
        bool floor = other.CompareTag("BlockGround") || (ground >= 0 && other.gameObject.layer == ground);
        if (!floor) return;
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        Vector2 origin = transform.position;
        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        SpawnParticle(origin);
        if (audioSource != null && boom != null) audioSource.PlayOneShot(boom, 1f);
        HGHaptics.WorldExplosion(origin, BlastRadius, LaunchSpeed);
        HGMultiplayerExtensions.RequestBlueOKExplosionFx(origin);
        ApplyBlast(origin);
        StartCoroutine(ExitAnimation());
    }

    private void ApplyBlast(Vector2 origin)
    {
        Body[] bodies = FindObjectsOfType<Body>();
        for (int i = 0; i < bodies.Length; i++)
        {
            Body body = bodies[i];
            if (body == null) continue;
            float dist = Vector2.Distance(origin, body.transform.position);
            if (dist > BlastRadius) continue;
            Vector2 dir = ((Vector2)body.transform.position - origin);
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            dir.Normalize();
            Vector2 velocity = dir * LaunchSpeed + Vector2.up * 17f;
            HGTemporaryNoclipFlight.Launch(body, velocity);
        }

        BuildingEntity[] entities = FindObjectsOfType<BuildingEntity>();
        for (int i = 0; i < entities.Length; i++)
        {
            BuildingEntity e = entities[i];
            if (e == null || (!e.animal && e.GetComponent<HGBuilderDummy>() == null)) continue;
            float dist = Vector2.Distance(origin, e.transform.position);
            if (dist > BlastRadius) continue;
            Rigidbody2D er = e.GetComponent<Rigidbody2D>();
            if (er == null) continue;
            Vector2 dir = ((Vector2)e.transform.position - origin);
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            dir.Normalize();
            HGTemporaryNoclipFlight.LaunchRigidbody(e.gameObject, er, dir * LaunchSpeed + Vector2.up * 17f);
        }

        HGMultiplayerRuntime rt = HGMultiplayerRuntime.Instance;
        if (HGMultiplayerState.Enabled && rt != null && rt.IsRunning)
        {
            foreach (int id in rt.GetConnectedPlayerIds())
            {
                if (id == rt.LocalNetworkPlayerId) continue;
                Vector3 pos;
                if (!rt.TryGetPlayerWorldPosition(id, out pos)) continue;
                if (Vector2.Distance(origin, pos) > BlastRadius) continue;
                Vector2 dir = (Vector2)pos - origin;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                HGMultiplayerExtensions.RequestBlueOKBlast(id, origin, dir.normalized);
            }
        }
    }

    public static void PlayRemoteExplosionFx(Vector2 origin)
    {
        SpawnParticle(origin);
        HGHaptics.WorldExplosion(origin, BlastRadius, LaunchSpeed);
        AudioClip clip = Resources.Load<AudioClip>("sounds/blueok_boom");
        if (clip == null) return;
        GameObject go = new GameObject("HG BlueOK Remote Explosion Audio");
        go.transform.position = origin;
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = 34f;
        if (WorldGeneration.world != null) source.outputAudioMixerGroup = WorldGeneration.world.soundMixerGroup;
        source.clip = clip;
        source.Play();
        UnityEngine.Object.Destroy(go, clip.length + 0.25f);
    }

    public static void ApplyNetworkBlastToLocal(Vector2 direction)
    {
        Body body = HGMultiplayerExtensions.GetLocalBody();
        if (body == null) return;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
        direction.Normalize();
        HGTemporaryNoclipFlight.Launch(body, direction * LaunchSpeed + Vector2.up * 17f);
    }

    private static void SpawnParticle(Vector2 origin)
    {
        GameObject prefab = Resources.Load<GameObject>("Special/ExplosionParticle");
        if (prefab == null) prefab = Resources.Load<GameObject>("ExplosionParticle");
        if (prefab != null) UnityEngine.Object.Instantiate(prefab, origin, Quaternion.identity);
    }

    private IEnumerator ExitAnimation()
    {
        float duration = 0.55f;
        float t = 0f;
        Vector3 baseScale = transform.localScale;
        Color baseColor = sprite != null ? sprite.color : Color.white;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            transform.localScale = baseScale * Mathf.Lerp(1f, 4.2f, eased);
            if (sprite != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, eased);
                sprite.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    public static ItemInfo CreateInfo()
    {
        ItemInfo info = new ItemInfo
        {
            category = "misc",
            slotRotation = 0f,
            usable = false,
            usableWithLMB = false,
            usableOnLimb = false,
            onlyHoldInHands = false,
            destroyAtZeroCondition = false,
            autoAttack = false,
            weight = 1.1f,
            tags = "throwable",
            value = 12,
            rec = new Recognition(4)
        };
        return info;
    }
}
