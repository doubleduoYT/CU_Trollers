using System;
using UnityEngine;

public sealed class HGMetalPipeBootstrap : MonoBehaviour
{
    private static HGMetalPipeBootstrap instance;
    private Body observedBody;
    private bool startingGrantCompleted;
    private float bodyReadyAt;
    private float nextAttempt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        HGMultiplayerRuntime runtime = HGMultiplayerRuntime.Instance;
        if (runtime != null && runtime.GetComponent<HGMetalPipeBootstrap>() == null)
            runtime.gameObject.AddComponent<HGMetalPipeBootstrap>();
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
    }

    private void Update()
    {
        EnsureRegistered();

        Body body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
        if (body != observedBody)
        {
            observedBody = body;
            startingGrantCompleted = false;
            bodyReadyAt = Time.unscaledTime + 0.35f;
            nextAttempt = bodyReadyAt;
        }

        if (body == null || startingGrantCompleted || Time.unscaledTime < nextAttempt)
        {
            return;
        }
        if (Item.GlobalItems == null || !Item.GlobalItems.ContainsKey(HGMetalPipeItem.ItemId) || body.slots == null || body.slots.Length == 0)
        {
            return;
        }

        if (HasMetalPipe(body))
        {
            startingGrantCompleted = true;
            return;
        }

        nextAttempt = Time.unscaledTime + 1.0f;
        startingGrantCompleted = GiveStartingPipe(body);
    }

    private static void EnsureRegistered()
    {
        if (Item.GlobalItems == null || Item.GlobalItems.ContainsKey(HGMetalPipeItem.ItemId))
        {
            return;
        }

        ItemInfo info = HGMetalPipeItem.CreateInfo();
        bool korean = HGLanguageRuntime.IsKorean;
        info.fullName = korean ? "쇠파이프" : "Metal Pipe";
        info.description = "OH MY EARS!(악 내 귀!)";
        info.SetTags();
        Item.GlobalItems.Add(HGMetalPipeItem.ItemId, info);
    }

    private static bool HasMetalPipe(Body body)
    {
        for (int i = 0; i < body.slots.Length; i++)
        {
            Item item = body.GetItem(i);
            if (item != null && string.Equals(item.id, HGMetalPipeItem.ItemId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool GiveStartingPipe(Body body)
    {
        Item item = SpawnMetalPipe(body.transform.position + Vector3.right * 0.8f);
        GameObject obj = item != null ? item.gameObject : null;
        if (item == null)
        {
            Debug.LogError("[HG Metal Pipe] Could not create the Metal Pipe item.");
            if (obj != null) Destroy(obj);
            return false;
        }
        item.condition = 1f;

        for (int i = 0; i < body.slots.Length; i++)
        {
            if (body.slots[i] == null || !body.slots[i].isHand || body.HoldingItem(i) || !body.slots[i].canPickUp)
            {
                continue;
            }
            body.PickUpItem(item, i, true);
            if (item.transform.parent != null)
            {
                Debug.Log("[HG Metal Pipe] Starting Metal Pipe equipped in hand slot " + i + ".");
                return true;
            }
        }

        Debug.Log("[HG Metal Pipe] Both hands occupied; starting Metal Pipe dropped beside player.");
        return true;
    }
    private static Sprite fallbackSprite;

    public static Item SpawnMetalPipe(Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>(HGMetalPipeItem.ItemId);
        GameObject obj = prefab != null ? Instantiate(prefab, position, Quaternion.identity) : CreateRuntimePipe(position);
        Item item = obj != null ? obj.GetComponent<Item>() : null;
        if (item != null)
        {
            item.id = HGMetalPipeItem.ItemId;
            item.condition = 1f;
            if (obj.GetComponent<HGMetalPipeItem>() == null) obj.AddComponent<HGMetalPipeItem>();
        }
        return item;
    }

    private static GameObject CreateRuntimePipe(Vector3 position)
    {
        GameObject go = new GameObject("metalpipe");
        go.SetActive(false);
        go.transform.position = position;
        go.layer = 7;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateFallbackPipeSprite();
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 1.65f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.9f, 0.45f);
        Item item = go.AddComponent<Item>();
        item.id = HGMetalPipeItem.ItemId;
        item.condition = 1f;
        go.AddComponent<HGMetalPipeItem>();
        go.SetActive(true);
        Debug.LogWarning("[HG Metal Pipe] v10 created the compatibility Metal Pipe because its old prefab was not present.");
        return go;
    }

    private static Sprite CreateFallbackPipeSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;
        const int w = 48, h = 10;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "metalpipe_v10_fallback";
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[w*h];
        Color32 clear = new Color32(0,0,0,0);
        Color32 dark = new Color32(55,62,68,255);
        Color32 mid = new Color32(130,142,150,255);
        Color32 light = new Color32(205,215,220,255);
        for (int i=0;i<px.Length;i++) px[i]=clear;
        for (int y=2;y<=7;y++)
        for (int x=1;x<w-1;x++) px[y*w+x] = (y==2||y==7) ? dark : mid;
        for (int x=4;x<w-5;x++) px[5*w+x]=light;
        for (int y=1;y<=8;y++) { px[y*w+1]=dark; px[y*w+(w-2)]=dark; }
        tex.SetPixels32(px); tex.Apply(false, true);
        fallbackSprite = Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f), 16f);
        fallbackSprite.name = "metalpipe";
        return fallbackSprite;
    }
}
