using System;
using UnityEngine;

public sealed class HGBlueOKBootstrap : MonoBehaviour
{
    private static HGBlueOKBootstrap instance;
    private Body observedBody;
    private float readyAt;
    private float nextAttempt;
    private bool startingGrantCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        HGMultiplayerRuntime runtime = HGMultiplayerRuntime.Instance;
        if (runtime != null && runtime.GetComponent<HGBlueOKBootstrap>() == null)
            runtime.gameObject.AddComponent<HGBlueOKBootstrap>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[HG HTML5] BlueOK legacy bootstrap skipped on WebGL; web transport can initialize separately.");
        return;
        #endif
        Debug.Log("[HG Multiplayer] BlueOK bootstrap active.");
    }

    private void Update()
    {
        EnsureRegistered();
        Body body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
        if (body != observedBody)
        {
            observedBody = body;
            readyAt = Time.unscaledTime + 0.45f;
            nextAttempt = readyAt;
            startingGrantCompleted = false;
        }
        if (body == null || body.slots == null || Time.unscaledTime < nextAttempt) return;
        if (Item.GlobalItems == null || !Item.GlobalItems.ContainsKey(HGBlueOKItem.ItemId)) return;

        if (startingGrantCompleted) return;
        if (HasBlueOK(body))
        {
            startingGrantCompleted = true;
            return;
        }

        nextAttempt = Time.unscaledTime + 1.0f; 
        Item item = SpawnBlueOK(body.transform.position + Vector3.right * 1.3f);
        if (item == null) return;
        item.condition = 1f;
        int? slot = body.FirstEmptySlot();
        if (slot.HasValue)
        {
            body.PickUpItem(item, slot.Value, true);
            if (item.transform.parent != null)
            {
                startingGrantCompleted = true;
                Debug.Log("[HG Multiplayer] Starting BlueOK granted once in slot " + slot.Value + ". Dropping it will not re-grant it.");
                return;
            }
        }
        startingGrantCompleted = true;
        Debug.Log("[HG Multiplayer] BlueOK starting copy spawned beside player because inventory had no usable slot; no duplicate will be granted.");
    }

    private static void EnsureRegistered()
    {
        if (Item.GlobalItems == null || Item.GlobalItems.ContainsKey(HGBlueOKItem.ItemId)) return;
        ItemInfo info = HGBlueOKItem.CreateInfo();
        bool ko = HGMultiplayerExtensions.IsKorean();
        info.fullName = ko ? "폭탄 받아라ㅏㅏㅏ!!" : "bombbombbombbomb";
        info.description = "BlueOK";
        info.SetTags();
        Item.GlobalItems.Add(HGBlueOKItem.ItemId, info);
        Debug.Log("[HG Multiplayer] BlueOK registered in Item.GlobalItems.");
    }

    private static bool HasBlueOK(Body body)
    {
        for (int i = 0; i < body.slots.Length; i++)
        {
            Item item = body.GetItem(i);
            if (item != null && string.Equals(item.id, HGBlueOKItem.ItemId, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static Item SpawnBlueOK(Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>(HGBlueOKItem.ItemId);
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            Item prefabItem = obj.GetComponent<Item>();
            if (prefabItem != null)
            {
                prefabItem.id = HGBlueOKItem.ItemId;
                prefabItem.condition = 1f;
                if (obj.GetComponent<HGBlueOKItem>() == null) obj.AddComponent<HGBlueOKItem>();
                return prefabItem;
            }
            Debug.LogWarning("[HG Multiplayer] blueok.prefab had no Item component; using runtime fallback.");
            Destroy(obj);
        }

        Sprite sprite = Resources.Load<Sprite>("blueok");
        if (sprite == null)
        {
            sprite = CreateFallbackSprite();
            Debug.LogWarning("[HG Multiplayer] BlueOK art was missing; v10 generated the compatibility sprite at runtime.");
        }
        GameObject go = new GameObject("blueok");
        go.SetActive(false);
        go.transform.position = position;
        go.layer = 7;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 1.1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.82f;
        Item item = go.AddComponent<Item>();
        item.id = HGBlueOKItem.ItemId;
        item.condition = 1f;
        go.AddComponent<HGBlueOKItem>();
        go.SetActive(true);
        Debug.Log("[HG Multiplayer] BlueOK created from runtime fallback.");
        return item;
    }
    private static Sprite fallbackSprite;

    private static Sprite CreateFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;
        const int w = 24, h = 24;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "blueok_v10_fallback";
        tex.filterMode = FilterMode.Point;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 edge = new Color32(12, 30, 60, 255);
        Color32 blue = new Color32(40, 145, 255, 255);
        Color32 shine = new Color32(190, 235, 255, 255);
        Color32[] px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = clear;
        Vector2 c = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
        for (int y = 1; y < h - 1; y++)
        for (int x = 1; x < w - 1; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            if (d <= 10.2f) px[y*w+x] = d > 8.9f ? edge : blue;
        }
        for (int y = 14; y < 19; y++)
        for (int x = 6; x < 10; x++) px[y*w+x] = shine;
        tex.SetPixels32(px); tex.Apply(false, true);
        fallbackSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        fallbackSprite.name = "blueok";
        return fallbackSprite;
    }
}
