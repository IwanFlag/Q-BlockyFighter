using UnityEngine;
using System.Collections.Generic;
using QBlockyFighter.Utils;

namespace QBlockyFighter.Core
{
    /// <summary>
    /// 武器掉落/拾取系统 - 地图上随机刷新武器品质升级道具
    /// </summary>
    public class WeaponDropSystem : MonoBehaviour
    {
        public static WeaponDropSystem Instance { get; private set; }

        [Header("掉落配置")]
        public float dropInterval = 120f;        // 掉落间隔（秒）
        public float dropLifetime = 30f;         // 掉落物存在时间
        public int maxDrops = 5;                 // 最大同时掉落物数量
        public float dropRadius = 20f;           // 掉落范围

        private float dropTimer;
        private List<GameObject> activeDrops = new List<GameObject>();

        // 品质道具数据
        private static readonly (string name, string icon, WeaponQuality quality, float weight)[] DropTable = {
            ("青铜之证", "🥉", WeaponQuality.Bronze, 0.50f),
            ("白银之证", "🥈", WeaponQuality.Silver, 0.30f),
            ("黄金之证", "🥇", WeaponQuality.Gold, 0.15f),
            ("黑铁之证", "🏅", WeaponQuality.Black, 0.05f), // 重置为黑铁（陷阱道具）
        };

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            dropTimer += Time.deltaTime;
            if (dropTimer >= dropInterval && activeDrops.Count < maxDrops)
            {
                dropTimer = 0f;
                SpawnDrop();
            }

            // 清理已销毁的掉落物
            activeDrops.RemoveAll(d => d == null);
        }

        /// <summary>在随机位置生成一个品质升级道具</summary>
        public void SpawnDrop()
        {
            Vector3 pos = GetRandomPosition();
            var dropData = GetRandomDrop();
            var drop = CreateDropObject(pos, dropData);
            activeDrops.Add(drop);

            // 自动销毁
            Destroy(drop, dropLifetime);
            Debug.Log($"[掉落] {dropData.name} 刷新在 {pos}");
        }

        /// <summary>在指定位置生成掉落物</summary>
        public void SpawnDropAt(Vector3 pos, WeaponQuality quality)
        {
            var data = GetDropByQuality(quality);
            var drop = CreateDropObject(pos, data);
            activeDrops.Add(drop);
            Destroy(drop, dropLifetime);
        }

        /// <summary>击杀掉落 - 根据击杀者武器品质掉落</summary>
        public void SpawnKillDrop(Vector3 position, WeaponQuality killerQuality)
        {
            // 击杀者品质越高，掉落品质越好
            float roll = Random.value;
            WeaponQuality dropQuality;

            if (killerQuality >= WeaponQuality.Gold)
                dropQuality = roll < 0.3f ? WeaponQuality.Gold : WeaponQuality.Silver;
            else if (killerQuality >= WeaponQuality.Silver)
                dropQuality = roll < 0.15f ? WeaponQuality.Gold : WeaponQuality.Bronze;
            else
                dropQuality = WeaponQuality.Bronze;

            SpawnDropAt(position, dropQuality);
        }

        private Vector3 GetRandomPosition()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(5f, dropRadius);
            return new Vector3(Mathf.Cos(angle) * dist, 0.5f, Mathf.Sin(angle) * dist);
        }

        private (string name, string icon, WeaponQuality quality, float weight) GetRandomDrop()
        {
            float roll = Random.value;
            float cumulative = 0f;
            foreach (var entry in DropTable)
            {
                cumulative += entry.weight;
                if (roll <= cumulative) return entry;
            }
            return DropTable[0];
        }

        private (string name, string icon, WeaponQuality quality, float weight) GetDropByQuality(WeaponQuality quality)
        {
            foreach (var entry in DropTable)
            {
                if (entry.quality == quality) return entry;
            }
            return DropTable[0];
        }

        private GameObject CreateDropObject(Vector3 pos, (string name, string icon, WeaponQuality quality, float weight) data)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
            go.name = $"Drop_{data.name}";
            go.tag = "WeaponDrop";

            // 颜色标识品质
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = GetQualityColor(data.quality);

            // 触发器碰撞
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            // 拾取组件
            var pickup = go.AddComponent<WeaponPickup>();
            pickup.quality = data.quality;
            pickup.dropName = data.name;

            // 旋转动画（由WeaponPickup处理）

            return go;
        }

        private Color GetQualityColor(WeaponQuality quality)
        {
            return quality switch
            {
                WeaponQuality.Black => new Color(0.4f, 0.4f, 0.4f),
                WeaponQuality.Bronze => new Color(0.8f, 0.5f, 0.2f),
                WeaponQuality.Silver => new Color(0.75f, 0.75f, 0.8f),
                WeaponQuality.Gold => new Color(1f, 0.84f, 0f),
                _ => Color.gray
            };
        }

        /// <summary>清除所有掉落物</summary>
        public void ClearAll()
        {
            foreach (var d in activeDrops)
            {
                if (d != null) Destroy(d);
            }
            activeDrops.Clear();
        }
    }

    /// <summary>掉落物拾取组件</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public WeaponQuality quality;
        public string dropName;

        private float rotateSpeed = 90f;
        private float bobSpeed = 2f;
        private float bobHeight = 0.2f;
        private Vector3 startPos;

        void Start()
        {
            startPos = transform.position;
        }

        void Update()
        {
            // 旋转 + 浮动
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var weaponSystem = other.GetComponent<WeaponSystem>();
            if (weaponSystem == null) return;

            // 只有品质更高的道具才能升级
            if (quality > weaponSystem.Quality)
            {
                weaponSystem.Quality = quality;
                Debug.Log($"[拾取] {other.name} 获得 {dropName} ({quality})");
                Destroy(gameObject);
            }
            else if (quality == WeaponQuality.Black && weaponSystem.Quality > WeaponQuality.Black)
            {
                // 陷阱道具 - 重置品质
                weaponSystem.ResetQuality();
                Debug.Log($"[陷阱] {other.name} 品质被重置!");
                Destroy(gameObject);
            }
        }
    }
}
