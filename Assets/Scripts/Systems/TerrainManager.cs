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
        EnsureStateSize();
        _wallHp = wallMaxHp;
        for (int i = 0; i < _barricadeHp.Length; i++)
            _barricadeHp[i] = barricadeMaxHp;
        for (int i = 0; i < _roadBlockHp.Length; i++)
            _roadBlockHp[i] = roadBlockMaxHp;
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
            if (roadBlockRenderers == null || i >= roadBlockRenderers.Length || roadBlockRenderers[i] == null) continue;
            if (Vector2.Distance(roadBlockRenderers[i].transform.position, block.position) < 0.5f) {
                _roadBlockHp[i] = Mathf.Max(0f, _roadBlockHp[i] - dmg);
                UpdateBlockSprite(i);
                if (_roadBlockHp[i] <= 0f) _roadBlockAlive[i] = false;
                return;
            }
        }
    }

    public void MinionDamage(Vector2 targetPos, float dmg) {
        EnsureStateSize();
        for (int i = 0; i < _barricadeAlive.Length; i++) {
            if (!_barricadeAlive[i]) continue;
            if (barricadeRenderers == null || i >= barricadeRenderers.Length || barricadeRenderers[i] == null) continue;
            if (Vector2.Distance(barricadeRenderers[i].transform.position, targetPos) < 0.5f) {
                _barricadeHp[i] = Mathf.Max(0f, _barricadeHp[i] - dmg);
                UpdateBarricadeSprite(i);
                if (_barricadeHp[i] <= 0f) {
                    _barricadeAlive[i] = false;
                    barricadeRenderers[i].enabled = false;
                }
                return;
            }
        }
        BossDamageWall(dmg);
    }

    public Vector2? GetAliveBarricade() {
        EnsureStateSize();
        if (barricadeRenderers == null) return null;
        for (int i = 0; i < _barricadeAlive.Length; i++)
            if (_barricadeAlive[i] && i < barricadeRenderers.Length && barricadeRenderers[i] != null)
                return barricadeRenderers[i].transform.position;
        return null;
    }

    public Vector2? GetClosestAliveBarricade(Vector2 from) {
        EnsureStateSize();
        if (barricadeRenderers == null) return null;

        int best = -1;
        float bestSqrDist = float.MaxValue;
        for (int i = 0; i < _barricadeAlive.Length; i++) {
            if (!_barricadeAlive[i]) continue;
            if (i >= barricadeRenderers.Length || barricadeRenderers[i] == null) continue;

            float sqrDist = ((Vector2)barricadeRenderers[i].transform.position - from).sqrMagnitude;
            if (sqrDist < bestSqrDist) {
                bestSqrDist = sqrDist;
                best = i;
            }
        }

        return best >= 0 ? barricadeRenderers[best].transform.position : null;
    }

    public Vector2? GetWallTarget() {
        if (_wallHp <= 0f || wallRenderer == null) return null;
        return wallRenderer.transform.position;
    }

    public TerrainBlock? GetRoadBlockAhead(float bossX) {
        EnsureStateSize();
        if (roadBlockRenderers == null) return null;
        for (int i = 0; i < _roadBlockAlive.Length; i++) {
            if (!_roadBlockAlive[i]) continue;
            if (i >= roadBlockRenderers.Length || roadBlockRenderers[i] == null) continue;
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

    private void UpdateBarricadeSprite(int i) {
        if (barricadeRenderers[i] == null || barricadeSprites == null || barricadeSprites.Length < 3) return;
        float r = _barricadeHp[i] / barricadeMaxHp;
        barricadeRenderers[i].sprite = r > 0.6f ? barricadeSprites[0] : r > 0.3f ? barricadeSprites[1] : barricadeSprites[2];
    }

    private void EnsureStateSize() {
        int barricadeCount = barricadeRenderers != null ? barricadeRenderers.Length : 3;
        int roadBlockCount = roadBlockRenderers != null ? roadBlockRenderers.Length : 3;

        if (_barricadeHp == null || _barricadeHp.Length != barricadeCount) {
            _barricadeHp = new float[barricadeCount];
            _barricadeAlive = new bool[barricadeCount];
            for (int i = 0; i < barricadeCount; i++) {
                _barricadeHp[i] = barricadeMaxHp;
                _barricadeAlive[i] = true;
            }
        }

        if (_roadBlockHp == null || _roadBlockHp.Length != roadBlockCount) {
            _roadBlockHp = new float[roadBlockCount];
            _roadBlockAlive = new bool[roadBlockCount];
            for (int i = 0; i < roadBlockCount; i++) {
                _roadBlockHp[i] = roadBlockMaxHp;
                _roadBlockAlive[i] = true;
            }
        }
    }
}
