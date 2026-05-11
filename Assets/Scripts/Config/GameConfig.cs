using UnityEngine;

public static class GameConfig {
    // Squad positioned at lower-left
    public static readonly float[] SQUAD_SLOT_X = { 0.68f, 1.28f, 1.88f, 2.48f, 3.08f };
    public const float SQUAD_Y       = 0.70f;

    // Boss moves diagonally from upper-right (START) toward lower-left (STOP)
    public const float BOSS_START_X  = 14.0f;
    public const float BOSS_START_Y  = 5.50f;
    public const float BOSS_STOP_X   = 5.0f;
    public const float BOSS_STOP_Y   = 2.10f;
    public const float BOSS_Y        = 5.50f;   // initial Y (= BOSS_START_Y)

    public const float DEFENSE_LINE  = 1.3f;

    // Wall sits along the diagonal path (X=7.0 → Y≈2.85)
    public const float WALL_X        = 7.0f;
    public const float WALL_Y        = 2.85f;

    // Minions spawn upper-right
    public const float SPAWN_X_MIN   = 12.5f;
    public const float SPAWN_X_MAX   = 13.5f;
    public const float SPAWN_Y_MIN   = 4.50f;
    public const float SPAWN_Y_MAX   = 5.50f;
}
