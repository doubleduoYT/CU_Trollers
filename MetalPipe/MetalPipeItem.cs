using System;
using UnityEngine;

public sealed class HGMetalPipeItem : MonoBehaviour
{
    public const string ItemId = "metalpipe";
    private const float Damage = 38f;
    private const float StructuralDamage = 55f;
    private const float Knockback = 240f;
    private const float PlayerLaunchSpeed = 24f;
    private const float PlayerLaunchLift = 0.38f;
    private const float Reach = 5.1f;
    private const float Cooldown = 0.32f;

    private Item item;
    private AudioSource pipeSource;
    private AudioClip pipeClip;
    private float nextDropSoundTime;

    private void Awake()
    {
        item = GetComponent<Item>();
        pipeSource = gameObject.AddComponent<AudioSource>();
        pipeSource.playOnAwake = false;
        pipeSource.loop = false;
        pipeSource.spatialBlend = 1f;
        pipeSource.dopplerLevel = 0f;
        pipeSource.rolloffMode = AudioRolloffMode.Logarithmic;
        pipeSource.minDistance = 1.5f;
        pipeSource.maxDistance = 22f;
        pipeClip = Resources.Load<AudioClip>("sounds/metalpipe");
    }

    private void Start()
    {
        if (WorldGeneration.world != null)
        {
            pipeSource.outputAudioMixerGroup = WorldGeneration.world.soundMixerGroup;
        }
    }

    private void LateUpdate()
    {
        if (item != null && item.condition != 1f)
        {
            item.condition = 1f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (item == null || transform.parent != null || collision == null)
        {
            return;
        }
        if (collision.relativeVelocity.magnitude < 2.2f || Time.time < nextDropSoundTime)
        {
            return;
        }
        nextDropSoundTime = Time.time + 0.18f;
        PlayPipeSound(0.9f, 0.94f, 1.06f);
    }

    public static ItemInfo CreateInfo()
    {
        ItemInfo info = new ItemInfo
        {
            category = "tool",
            slotRotation = -90f,
            usable = true,
            usableWithLMB = true,
            usableOnLimb = false,
            onlyHoldInHands = true,
            destroyAtZeroCondition = false,
            autoAttack = true,
            weight = 1.65f,
            tags = "tool,backflip",
            value = 28,
            rec = new Recognition(5),
            qualities = new System.Collections.Generic.List<CraftingQuality>
            {
                new CraftingQuality("hammering", 30f)
            }
        };
        info.useAction = delegate(Body body, Item heldItem)
        {
            HGMetalPipeItem behaviour = heldItem.GetComponent<HGMetalPipeItem>();
            if (behaviour == null)
            {
                behaviour = heldItem.gameObject.AddComponent<HGMetalPipeItem>();
            }
            behaviour.PerformAttack(body);
        };
        return info;
    }

    private void PerformAttack(Body attacker)
    {
        if (attacker == null || !attacker.conscious || attacker.attackCooldown > 0f || attacker.limbs == null || attacker.limbs.Length < 2)
        {
            return;
        }

        Vector2 origin = attacker.limbs[1].transform.position;
        Vector2 direction = ((Vector2)attacker.targetLookPos - origin).normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = attacker.isRight ? Vector2.right : Vector2.left;
        }

        HGMultiplayerRuntime runtime = HGMultiplayerRuntime.Instance;
        int remotePlayerId;
        Vector3 remotePosition;
        if (HGMultiplayerState.Enabled && runtime != null && runtime.IsRunning &&
            runtime.TryFindRemotePlayerAlongRay(origin, direction, Reach, 1.55f, out remotePlayerId, out remotePosition))
        {
            float remoteDistance = Vector2.Distance(origin, remotePosition);
            if (!HasSolidWorldHitBefore(origin, direction, remoteDistance))
            {
                AttackInfo remoteSwing = CreateAttack(Mathf.Max(0.05f, remoteDistance - 0.12f));
                attacker.Attack(remoteSwing, 0);
                HGMultiplayerExtensions.RequestMetalPipeKnock(remotePlayerId, direction);
                PlayPipeSound(0.84f, 0.96f, 1.05f);
                return;
            }
        }

        float dummyHitDistance;
        HGBuilderDummy dummyTarget = FindFirstDummy(origin, direction, Reach, out dummyHitDistance);
        if (dummyTarget != null)
        {
            AttackInfo dummySwing = CreateAttack(Mathf.Max(0.05f, dummyHitDistance - 0.08f));
            attacker.Attack(dummySwing, 0);
            dummyTarget.ApplyMetalPipeHit(direction);
            PlayPipeSound(0.86f, 0.94f, 1.06f);
            return;
        }

        float bodyHitDistance;
        Body bodyTarget = FindFirstOtherBody(attacker, origin, direction, Reach, out bodyHitDistance);

        AttackInfo attack = CreateAttack(bodyTarget != null ? Mathf.Max(0.05f, bodyHitDistance - 0.08f) : Reach);

        if (bodyTarget != null)
        {
            attacker.Attack(attack, 0);
            PushBodyOnly(bodyTarget, direction);
            PlayPipeSound(0.82f, 0.96f, 1.05f);
            return;
        }

        attacker.Attack(attack, 0);
        PlayPipeSound(0.82f, 0.96f, 1.05f);
    }

    private static AttackInfo CreateAttack(float distance)
    {
        return new AttackInfo
        {
            damage = Damage,
            structuralDamage = StructuralDamage,
            attackCooldownMult = 0.78f,
            distance = distance,
            knockBack = Knockback,
            cooldown = Cooldown,
            attackAnim = Resources.Load<GameObject>("SwingAnim"),
            staminaUse = 0.85f,
            piercing = false,
            swingSounds = new string[1] { "BSSwing1" },
            volume = 0.08f,
            rotateAmount = 14f,
            metalMoreDamage = false
        };
    }

    private static bool HasSolidWorldHitBefore(Vector2 origin, Vector2 direction, float maxDistance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, Mathf.Max(0f, maxDistance - 0.1f));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null) continue;
            if (t.CompareTag("BlockGround")) return true;
            BuildingEntity entity = t.GetComponent<BuildingEntity>();
            if (entity == null) entity = t.GetComponentInParent<BuildingEntity>();
            if (entity != null && !entity.cantHit) return true;
        }
        return false;
    }

    private static HGBuilderDummy FindFirstDummy(Vector2 origin, Vector2 direction, float distance, out float hitDistance)
    {
        hitDistance = distance;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);
        Array.Sort(hits, delegate(RaycastHit2D a, RaycastHit2D b)
        {
            return a.distance.CompareTo(b.distance);
        });
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null) continue;
            HGBuilderDummy dummy = t.GetComponentInParent<HGBuilderDummy>();
            if (dummy != null)
            {
                hitDistance = hits[i].distance;
                return dummy;
            }
            if (t.CompareTag("BlockGround") || t.GetComponentInParent<BuildingEntity>() != null)
                break;
        }
        return null;
    }

    private static Body FindFirstOtherBody(Body attacker, Vector2 origin, Vector2 direction, float distance, out float hitDistance)
    {
        hitDistance = distance;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);
        Array.Sort(hits, delegate(RaycastHit2D a, RaycastHit2D b)
        {
            return a.distance.CompareTo(b.distance);
        });

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == null)
            {
                continue;
            }
            if (t == attacker.transform || t.IsChildOf(attacker.transform))
            {
                continue;
            }

            Limb limb = t.GetComponent<Limb>();
            if (limb == null)
            {
                limb = t.GetComponentInParent<Limb>();
            }
            Body other = limb != null ? limb.body : t.GetComponentInParent<Body>();
            if (other != null && other != attacker)
            {
                hitDistance = hits[i].distance;
                return other;
            }

            if (t.CompareTag("BlockGround") || t.GetComponent<BuildingEntity>() != null || t.GetComponentInParent<BuildingEntity>() != null)
            {
                break;
            }
        }
        return null;
    }

    private static void PushBodyOnly(Body target, Vector2 direction)
    {
        ApplyPlayerKnockback(target, direction);
    }

    public static void ApplyPlayerKnockback(Body target, Vector2 direction)
    {
        if (target == null)
        {
            return;
        }

        Vector2 launch = direction;
        if (launch.sqrMagnitude < 0.0001f)
        {
            launch = Vector2.right;
        }
        launch.Normalize();

        launch.y = Mathf.Max(launch.y, PlayerLaunchLift);
        launch.Normalize();

        target.Ragdoll();

        Vector2 launchVelocity = launch * PlayerLaunchSpeed;
        if (target.limbs != null)
        {
            for (int i = 0; i < target.limbs.Length; i++)
            {
                Limb limb = target.limbs[i];
                if (limb == null || limb.rb == null)
                {
                    continue;
                }

                limb.rb.simulated = true;
                float spread = 0.94f + 0.12f * ((i % 3) / 2f);
                limb.rb.velocity += launchVelocity * spread;
                limb.rb.angularVelocity += ((i & 1) == 0 ? 1f : -1f) * (110f + i * 7f);
            }
        }

        if (target.rb != null)
        {
            target.rb.velocity += launchVelocity * 0.45f;
        }
    }

    private void PlayPipeSound(float volume, float pitchMin, float pitchMax)
    {
        if (pipeSource == null || pipeClip == null)
        {
            return;
        }
        pipeSource.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        pipeSource.PlayOneShot(pipeClip, volume);
    }
}
