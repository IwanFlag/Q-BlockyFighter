namespace QBlockyFighter.Utils
{
    public static class GameConfig
    {
        // ===== 服务器配置 =====
        public const int TICK_RATE = 20;
        public const int FRAME_INTERVAL_MS = 1000 / TICK_RATE;
        public const int PORT = 3000;
        public const int MAX_PLAYERS_PER_ROOM_1V1 = 2;
        public const int MAX_PLAYERS_PER_ROOM_5V5 = 10;
        public const int MAX_SPECTATORS = 10;

        // ===== 战斗配置 =====
        public const float MAX_STAMINA = 100f;
        public const float STAMINA_REGEN_RATE = 15f;      // 每秒恢复
        public const float STAMINA_REGEN_DELAY = 2f;       // 脱战后开始恢复的延迟
        public const float ATTACK_STAMINA_COST = 10f;
        public const float DODGE_STAMINA_COST = 25f;
        public const float BLOCK_STAMINA_COST = 5f;        // 每次格挡
        public const float PARRY_WINDOW = 0.15f;           // 弹反窗口（秒）
        public const float DODGE_IFRAME_DURATION = 0.3f;   // 闪避无敌帧
        public const float SUPER_ARMOR_THRESHOLD = 0.5f;   // 霸体免伤比例

        // ===== 连招配置 =====
        public const int MAX_LIGHT_COMBO = 4;              // 最大连击数
        public const float COMBO_WINDOW = 0.5f;            // 连招输入窗口
        public const float ATTACK_SWITCH_BONUS = 0.2f;     // 切换武器后首击加成

        // ===== 角色定位属性 =====
        public static readonly float[] BASE_HP_BY_ROLE = { 800, 800, 1000, 1000, 1200, 600, 600, 600, 700, 700 }; // 刺客,战士,坦克,远程,辅助
        public static readonly float[] BASE_ATK_BY_ROLE = { 120, 120, 100, 100, 80, 130, 130, 130, 90, 90 };

        // ===== 脱战回血（每秒百分比）=====
        public static readonly float[] REGEN_BY_ROLE = { 0.01f, 0.01f, 0.02f, 0.02f, 0.03f, 0.01f, 0.01f, 0.01f, 0.02f, 0.03f };

        // ===== 击杀回血 =====
        public const float KILL_HEAL_PERCENT = 0.5f;

        // ===== 武器品质加成 =====
        public static readonly float[] QUALITY_BONUS = { 0f, 0.1f, 0.25f, 0.5f }; // 黑铁,青铜,白银,黄金

        // ===== 武器品质名称 =====
        public static readonly string[] QUALITY_NAMES = { "黑铁", "青铜", "白银", "黄金" };

        // ===== 排位段位 =====
        public static readonly string[] RANK_TIERS = { "初悟", "凝心", "破障", "通玄", "化境", "归真", "无极" };

        // ===== 游戏节奏 =====
        public const float WILD_MONSTER_LEVEL_INTERVAL = 300f;  // 野怪升级间隔（秒）
        public const float BOSS_SPAWN_TIME = 600f;              // Boss刷新时间
        public const float GAME_MAX_TIME = 1500f;               // 单局最长时间（25分钟）
        public const float LATE_GAME_TIME = 1080f;              // 后期事件时间（18分钟）
        public const float AUTO_REGEN_DELAY = 5f;               // 脱战自动回血延迟

        // ===== 服务器URL =====
        public const string DEFAULT_SERVER_URL = "ws://localhost:3000";
        public const string API_ROOMS_URL = "/api/rooms";
        public const string API_STATUS_URL = "/api/status";
    }
}
