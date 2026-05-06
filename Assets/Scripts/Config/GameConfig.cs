using UnityEngine;

public static class GameConfig {
    public static readonly float[] SQUAD_SLOT_X = { 0.68f, 1.28f, 1.88f, 2.48f, 3.08f };
    public const float SQUAD_Y       = 2.00f;   // center Y: feet at 2.0 - 0.7 = 1.30 (just above ground)
    public const float BOSS_START_X  = 14.0f;
    public const float BOSS_STOP_X   = 5.0f;    // boss stops between barricade field and squad
    public const float BOSS_Y        = 2.50f;   // center Y: canvas extends ±2.2, creature bottom ≈ 1.3
    public const float DEFENSE_LINE  = 1.3f;
    public const float WALL_X        = 7.0f;
    public const float WALL_Y        = 2.02f;
    public const float SPAWN_X_MIN   = 11.6f;
    public const float SPAWN_X_MAX   = 12.6f;
    public const float SPAWN_Y_MIN   = 1.75f;   // raised for larger monster canvas centers
    public const float SPAWN_Y_MAX   = 2.00f;
}
