using System.Collections.Generic;
using UnityEngine;

public struct TerrainBlock {
    public Vector2 position;
    public float hp, maxHp;
    public bool alive;
    public bool isWall, isBarricade, isRoadBlock;
    public SpriteRenderer renderer;
    public Sprite[] damageSprites; // [healthy, damaged, critical]
}

public class TerrainManager : MonoBehaviour {
    [SerializeField] private float wallMaxHp      = 5000f;
    [SerializeField] private float barricadeMaxHp = 280f;
    [SerializeField] private float roadBlockMaxHp = 300f;

    [SerializeField] private SpriteRenderer wallRenderer;
    [SerializeField] private SpriteRenderer[] barricadeRenderers;  // 3
    [SerializeField] private SpriteRenderer[] roadBlockRenderers;  // 3
    [SerializeField] private Sprite[] wallSprites;        // [healthy, damaged, critical]
    [SerializeField] private Sprite[] barricadeSprites;
    [SerializeField] private Sprite[] roadBlockSprites;

    private float _wallHp;
    private float[] _barricadeHp = new float[3];
    private float[] _roadBlockHp = new float[3];
    private bool[]  _barricadeAlive = { true, true, true };
    private bool[]  _roadBlockAlive = { true, true, true };

    void Awake() {
        _wallHp = wallMaxHp;
        for (int i = 0; i < 3; i++) {
            _barricadeHp[i] = barricadeMaxHp;
            _roadBlockHp[i] = roadBlockMaxHp;
        }
    }

    public void BossDamageWall(float dmg) {
        _wallHp = Mathf.Max(0f, _wallHp - dmg);
        UpdateWallSprite();
        GameEvents.RaiseWallHpChanged(_wallHp, wallMaxHp);
        if (_wallHp <= 0f) GameEvents.RaiseWallDestroyed();
    }

    public void BossSmashBlock(TerrainBlock block, float dmg) {
        for (int i = 0; i < 3; i++) {
            if (!_roadBlockAlive[i]) continue;
            if (Vector2.Distance(roadBlockRenderers[i].transform.position, block.position) < 0.5f) {
                _roadBlockHp[i] = Mathf.Max(0f, _roadBlockHp[i] - dmg);
                UpdateBlockSprite(i);
                if (_roadBlockHp[i] <= 0f) _roadBlockAlive[i] = false;
                return;
            }
        }
    }

    public void MinionDamage(Vector2 targetPos, float dmg) {
        for (int i = 0; i < 3; i++) {
            if (!_barricadeAlive[i]) continue;
            if (Vector2.Distance(barricadeRenderers[i].transform.position, targetPos) < 0.5f) {
                _barricadeHp[i] = Mathf.Max(0f, _barricadeHp[i] - dmg);
                if (_barricadeHp[i] <= 0f) _barricadeAlive[i] = false;
                return;
            }
        }
        BossDamageWall(dmg);
    }

    public Vector2? GetAliveBarricade() {
        for (int i = 0; i < 3; i++)
            if (_barricadeAlive[i]) return barricadeRenderers[i].transform.position;
        return null;
    }

    public Vector2? GetWallTarget() => _wallHp > 0f ? (Vector2?)wallRenderer.transform.position : null;

    public TerrainBlock? GetRoadBlockAhead(float bossX) {
        for (int i = 0; i < 3; i++) {
            if (!_roadBlockAlive[i]) continue;
            float rx = roadBlockRenderers[i].transform.position.x;
            if (bossX <= rx + 0.75f && bossX > rx - 0.15f)
                return new TerrainBlock { position = roadBlockRenderers[i].transform.position, alive = true };
        }
        return null;
    }

    private void UpdateWallSprite() {
        if (wallRenderer == null || wallSprites == null || wallSprites.Length < 3) return;
        float r = _wallHp / wallMaxHp;
        wallRenderer.sprite = r > 0.7f ? wallSprites[0] : r > 0.3f ? wallSprites[1] : wallSprites[2];
    }

    private void UpdateBlockSprite(int i) {
        if (roadBlockRenderers[i] == null || roadBlockSprites == null || roadBlockSprites.Length < 3) return;
        float r = _roadBlockHp[i] / roadBlockMaxHp;
        roadBlockRenderers[i].sprite = r > 0.6f ? roadBlockSprites[0] : r > 0.3f ? roadBlockSprites[1] : roadBlockSprites[2];
    }
}
