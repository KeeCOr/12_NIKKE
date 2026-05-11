using UnityEngine;

[System.Serializable]
public class SpawnZone {
    public float xMin   = 12.5f;
    public float xMax   = 13.5f;
    public float yMin   = 4.50f;
    public float yMax   = 5.50f;
    [Range(0.1f, 10f)]
    public float weight = 1f;   // 가중치 — 높을수록 이 경로에서 더 많이 스폰
}

[CreateAssetMenu(fileName = "MapConfig", menuName = "SquadVsMonster/MapConfig")]
public class MapConfigSO : ScriptableObject {
    [Header("스폰 경로 목록 (복수 경로 지원)")]
    public SpawnZone[] spawnZones = new SpawnZone[] { new SpawnZone() };

    [Header("도착 지점 (좌측 하단)")]
    public float destX = 1.3f;
    public float destY = 0.70f;

    // 가중치 기반으로 스폰 영역 하나를 랜덤 선택 후 해당 구역 내 랜덤 점 반환
    public Vector2 RandomSpawnPoint() {
        if (spawnZones == null || spawnZones.Length == 0)
            return new Vector2(13f, 5f);

        float total = 0f;
        foreach (var z in spawnZones) total += z.weight;
        float r = Random.value * total;
        foreach (var z in spawnZones) {
            r -= z.weight;
            if (r <= 0f)
                return new Vector2(Random.Range(z.xMin, z.xMax), Random.Range(z.yMin, z.yMax));
        }
        var last = spawnZones[spawnZones.Length - 1];
        return new Vector2(Random.Range(last.xMin, last.xMax), Random.Range(last.yMin, last.yMax));
    }

    // 전체 스폰 구역 중심 → 도착 지점 방향 벡터 (폴백 이동 방향)
    public Vector2 DiagonalDirection() {
        if (spawnZones == null || spawnZones.Length == 0)
            return new Vector2(-1f, -0.378f).normalized;
        float cx = 0f, cy = 0f;
        foreach (var z in spawnZones) {
            cx += (z.xMin + z.xMax) * 0.5f;
            cy += (z.yMin + z.yMax) * 0.5f;
        }
        cx /= spawnZones.Length;
        cy /= spawnZones.Length;
        return new Vector2(destX - cx, destY - cy).normalized;
    }
}
