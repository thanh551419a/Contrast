using UnityEditor;
using UnityEngine;
using Contrast.Player;
using Contrast.Level;

public static class PlayerPlatformAuditor
{
    [InitializeOnLoadMethod]
    public static void RunAudit()
    {
        Debug.Log("=== AUDIT: PLAYER & PLATFORM GEOMETRY START ===");
        
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Transform pt = player.transform;
            BoxCollider2D pcol = player.GetComponent<BoxCollider2D>();
            SpriteRenderer psr = player.GetComponent<SpriteRenderer>();

            Debug.Log($"[AUDIT-PLAYER] GameObject: {player.gameObject.name}");
            Debug.Log($"[AUDIT-PLAYER] Position: {pt.position}, LocalPosition: {pt.localPosition}");
            Debug.Log($"[AUDIT-PLAYER] Rotation: {pt.rotation.eulerAngles}, LocalScale: {pt.localScale}, LossyScale: {pt.lossyScale}");
            Debug.Log($"[AUDIT-PLAYER] Parent: {(pt.parent != null ? pt.parent.name : "null")}");

            if (pcol != null)
            {
                Debug.Log($"[AUDIT-PLAYER-COLLIDER] Size: {pcol.size}, Offset: {pcol.offset}, Enabled: {pcol.enabled}, IsTrigger: {pcol.isTrigger}");
                Debug.Log($"[AUDIT-PLAYER-COLLIDER] Bounds: center={pcol.bounds.center}, extents={pcol.bounds.extents}, min={pcol.bounds.min}, max={pcol.bounds.max}");
            }
            else
            {
                Debug.Log("[AUDIT-PLAYER-COLLIDER] BoxCollider2D is NULL!");
            }

            if (psr != null)
            {
                Sprite s = psr.sprite;
                string sName = s != null ? s.name : "null";
                Vector2 sPivot = s != null ? s.pivot : Vector2.zero;
                Vector2 sRect = s != null ? new Vector2(s.rect.width, s.rect.height) : Vector2.zero;
                float ppu = s != null ? s.pixelsPerUnit : 0;
                Debug.Log($"[AUDIT-PLAYER-SPRITE] Sprite: {sName}, Rect: {sRect}, Pivot: {sPivot}, PPU: {ppu}, DrawMode: {psr.drawMode}, Size: {psr.size}");
                Debug.Log($"[AUDIT-PLAYER-SPRITE] Bounds: center={psr.bounds.center}, min={psr.bounds.min}, max={psr.bounds.max}");
            }
            else
            {
                Debug.Log("[AUDIT-PLAYER-SPRITE] SpriteRenderer is NULL!");
            }
        }
        else
        {
            Debug.Log("[AUDIT-PLAYER] PlayerController not found in active scene!");
        }

        ColorPlatform[] platforms = Object.FindObjectsByType<ColorPlatform>(FindObjectsSortMode.None);
        Debug.Log($"[AUDIT-PLATFORMS] Found {platforms.Length} platforms.");
        foreach (var plat in platforms)
        {
            Transform t = plat.transform;
            BoxCollider2D col = plat.GetComponent<BoxCollider2D>();
            SpriteRenderer sr = plat.GetComponent<SpriteRenderer>();

            Debug.Log($"[AUDIT-PLATFORM] Name: {plat.gameObject.name}, Position: {t.position}, LocalScale: {t.localScale}, LossyScale: {t.lossyScale}");
            if (col != null)
            {
                Debug.Log($"[AUDIT-PLATFORM-COLLIDER] {plat.gameObject.name}: Size: {col.size}, Offset: {col.offset}, Bounds: min={col.bounds.min}, max={col.bounds.max}");
            }
            if (sr != null)
            {
                Debug.Log($"[AUDIT-PLATFORM-SPRITE] {plat.gameObject.name}: DrawMode: {sr.drawMode}, Size: {sr.size}, Bounds: min={sr.bounds.min}, max={sr.bounds.max}");
            }

            if (player != null && player.GetComponent<BoxCollider2D>() != null && col != null)
            {
                float playerBottom = player.GetComponent<BoxCollider2D>().bounds.min.y;
                float platTop = col.bounds.max.y;
                float diff = playerBottom - platTop;
                Debug.Log($"[AUDIT-RELATION] {plat.gameObject.name} -> PlayerBottomY={playerBottom}, PlatformTopY={platTop}, Diff(Bottom - Top)={diff}");
            }
        }

        Debug.Log("=== AUDIT: PLAYER & PLATFORM GEOMETRY END ===");
    }
}
