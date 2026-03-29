using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Map
{
    public class MapManager : MonoBehaviour
    {
        public enum MapType
        {
            DragonArena,      // 龙城演武 - 1V1
            RomanColosseum,   // 罗马竞技场 - 1V1
            BambooForest,     // 竹林幽径 - 1V1
            NorthSeaIce,      // 北海冰原 - 5V5
            ChangAnCity,      // 长安城 - 5V5
            PersianGarden     // 波斯花园 - 5V5
        }

        public static MapManager Instance { get; private set; }
        public MapType CurrentMap { get; private set; }

        private List<GameObject> mapObjects = new List<GameObject>();
        private List<GameObject> destructibles = new List<GameObject>();
        private Transform[] spawnPoints;

        void Awake()
        {
            Instance = this;
        }

        public void LoadMap(MapType map, string mode)
        {
            ClearMap();
            CurrentMap = map;
            Debug.Log($"[Map] 加载地图: {map} ({mode})");

            // 创建地面
            CreateGround();

            // 根据地图类型生成场景
            switch (map)
            {
                case MapType.DragonArena:
                    CreateDragonArena();
                    break;
                case MapType.RomanColosseum:
                    CreateRomanColosseum();
                    break;
                case MapType.BambooForest:
                    CreateBambooForest();
                    break;
                case MapType.NorthSeaIce:
                    CreateNorthSeaIce();
                    break;
                case MapType.ChangAnCity:
                    CreateChangAnCity();
                    break;
                case MapType.PersianGarden:
                    CreatePersianGarden();
                    break;
            }

            // 创建重生点
            CreateSpawnPoints(mode == "5v5" ? 10 : 2);
        }

        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.name = "Ground";
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);
            mapObjects.Add(ground);
        }

        // ===== 龙城演武 =====
        private void CreateDragonArena()
        {
            // 中央擂台
            CreatePlatform(Vector3.zero, new Vector3(12, 0.5f, 12), new Color(0.6f, 0.4f, 0.2f));
            // 围栏柱子
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 7, 1.5f, Mathf.Sin(angle) * 7);
                CreatePillar(pos, new Color(0.8f, 0.2f, 0.1f));
            }
            // 角落灯笼
            CreateLantern(new Vector3(-6, 0, -6));
            CreateLantern(new Vector3(6, 0, -6));
            CreateLantern(new Vector3(-6, 0, 6));
            CreateLantern(new Vector3(6, 0, 6));
        }

        // ===== 罗马竞技场 =====
        private void RomanColosseum()
        {
            CreatePlatform(Vector3.zero, new Vector3(14, 0.3f, 14), new Color(0.7f, 0.65f, 0.5f));
            // 观众台（环形柱子）
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 10, 2f, Mathf.Sin(angle) * 10);
                CreatePillar(pos, new Color(0.8f, 0.75f, 0.6f));
            }
        }

        // ===== 竹林幽径 =====
        private void CreateBambooForest()
        {
            CreatePlatform(Vector3.zero, new Vector3(16, 0.1f, 16), new Color(0.25f, 0.35f, 0.2f));
            // 竹子
            for (int i = 0; i < 30; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-12f, 12f));
                if (pos.magnitude < 4f) continue;
                CreateTree(pos, new Color(0.3f, 0.6f, 0.2f));
            }
        }

        // ===== 北海冰原 =====
        private void CreateNorthSeaIce()
        {
            CreatePlatform(Vector3.zero, new Vector3(25, 0.1f, 25), new Color(0.8f, 0.9f, 0.95f));

            // 封闭空间A（左上）
            CreateRoom(new Vector3(-12, 0, 10), 8f, 6f, new Color(0.7f, 0.85f, 0.95f));
            // 封闭空间B（右下）
            CreateRoom(new Vector3(12, 0, -10), 8f, 6f, new Color(0.7f, 0.85f, 0.95f));

            // 冰柱
            for (int i = 0; i < 8; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f));
                CreatePillar(pos + Vector3.up * 2, new Color(0.7f, 0.85f, 0.95f));
            }
            // 冰山
            CreateRock(new Vector3(-10, 0, 0), new Color(0.75f, 0.88f, 0.95f), 3f);
            CreateRock(new Vector3(10, 0, 5), new Color(0.75f, 0.88f, 0.95f), 2.5f);
        }

        // ===== 长安城 =====
        private void CreateChangAnCity()
        {
            CreatePlatform(Vector3.zero, new Vector3(25, 0.2f, 25), new Color(0.5f, 0.45f, 0.4f));

            // 封闭宫殿区（中央）
            CreateRoom(new Vector3(0, 0, 0), 10f, 10f, new Color(0.55f, 0.4f, 0.3f));

            // 废墟建筑
            CreateRuin(new Vector3(-15, 0, -8), 4f);
            CreateRuin(new Vector3(15, 0, -5), 3f);
            CreateRuin(new Vector3(-8, 0, 15), 3.5f);
            CreateRuin(new Vector3(8, 0, 15), 2.5f);
        }

        // ===== 波斯花园 =====
        private void CreatePersianGarden()
        {
            CreatePlatform(Vector3.zero, new Vector3(25, 0.1f, 25), new Color(0.6f, 0.55f, 0.3f));

            // 封闭花园（中央）
            CreateRoom(new Vector3(0, 0, 0), 8f, 8f, new Color(0.5f, 0.6f, 0.3f));

            // 绿洲水池
            CreatePond(new Vector3(0, 0, 8), 3f);
            // 灌木
            for (int i = 0; i < 15; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-12f, 12f));
                if (Vector3.Distance(pos, Vector3.zero) < 6f) continue;
                CreateTree(pos, new Color(0.2f, 0.5f, 0.15f));
            }
        }

        // ===== 地图元素创建 =====
        private void CreatePlatform(Vector3 pos, Vector3 size, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = pos;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().material.color = color;
            obj.name = "Platform";
            obj.isStatic = true;
            mapObjects.Add(obj);
        }

        private void CreatePillar(Vector3 pos, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
            obj.GetComponent<Renderer>().material.color = color;
            obj.name = "Pillar";
            obj.isStatic = true;
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            mapObjects.Add(obj);
        }

        private void CreateTree(Vector3 pos, Color color)
        {
            // 树干
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.position = pos + Vector3.up * 1.5f;
            trunk.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
            trunk.GetComponent<Renderer>().material.color = new Color(0.4f, 0.25f, 0.1f);
            trunk.name = "TreeTrunk";
            trunk.isStatic = true;
            mapObjects.Add(trunk);

            // 树冠
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.position = pos + Vector3.up * 3.5f;
            canopy.transform.localScale = new Vector3(2f, 2f, 2f);
            canopy.GetComponent<Renderer>().material.color = color;
            canopy.name = "TreeCanopy";
            canopy.isStatic = true;
            mapObjects.Add(canopy);
        }

        private void CreateRock(Vector3 pos, Color color, float scale = 1f)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = pos + Vector3.up * scale * 0.5f;
            obj.transform.localScale = Vector3.one * scale;
            obj.transform.rotation = Random.rotation;
            obj.GetComponent<Renderer>().material.color = color;
            obj.name = "Rock";
            obj.isStatic = true;
            mapObjects.Add(obj);
        }

        private void CreateRuin(Vector3 pos, float height)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = pos + Vector3.up * height / 2f;
            obj.transform.localScale = new Vector3(Random.Range(2f, 4f), height, Random.Range(2f, 4f));
            obj.GetComponent<Renderer>().material.color = new Color(0.5f, 0.45f, 0.35f);
            obj.name = "Ruin";
            obj.isStatic = true;
            mapObjects.Add(obj);
        }

        private void CreateBridge(Vector3 pos, float length)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = pos + Vector3.up * 0.3f;
            obj.transform.localScale = new Vector3(length, 0.3f, 2f);
            obj.GetComponent<Renderer>().material.color = new Color(0.45f, 0.3f, 0.15f);
            obj.name = "Bridge";
            obj.isStatic = true;
            mapObjects.Add(obj);
        }

        /// <summary>
        /// 创建封闭空间（带机关门的房间）
        /// 四面墙，每面留出门洞，门洞上安装GateController
        /// </summary>
        private void CreateRoom(Vector3 center, float width, float depth, Color wallColor)
        {
            float wallHeight = 4f;
            float wallThick = 0.5f;
            float doorWidth = 3f; // 门洞宽度

            // 北墙（z + depth/2）
            CreateWallSegment(center + new Vector3(0, wallHeight / 2f, depth / 2f),
                width, wallHeight, wallThick, wallColor, true, "North");
            // 南墙
            CreateWallSegment(center + new Vector3(0, wallHeight / 2f, -depth / 2f),
                width, wallHeight, wallThick, wallColor, true, "South");
            // 东墙
            CreateWallSegment(center + new Vector3(width / 2f, wallHeight / 2f, 0),
                wallThick, wallHeight, depth, wallColor, true, "East");
            // 西墙
            CreateWallSegment(center + new Vector3(-width / 2f, wallHeight / 2f, 0),
                wallThick, wallHeight, depth, wallColor, true, "West");

            // 在每个门洞安装机关门
            // 北门
            CreateGate(center + new Vector3(0, 0, depth / 2f), 0, "Gate_North");
            // 南门
            CreateGate(center + new Vector3(0, 0, -depth / 2f), 180, "Gate_South");
        }

        /// <summary>创建墙段（中间留门洞）</summary>
        private void CreateWallSegment(Vector3 center, float w, float h, float d, Color color, bool hasDoor, string name)
        {
            if (!hasDoor)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = center;
                wall.transform.localScale = new Vector3(w, h, d);
                wall.GetComponent<Renderer>().material.color = color;
                wall.name = $"Wall_{name}";
                wall.isStatic = true;
                mapObjects.Add(wall);
                return;
            }

            float doorWidth = 3f;
            float sideWidth;

            // 根据墙的方向决定分割方式
            if (w > d) // 横墙（南北）
            {
                sideWidth = (w - doorWidth) / 2f;
                // 左半墙
                CreateWallBlock(center + new Vector3(-doorWidth / 2f - sideWidth / 2f, 0, 0),
                    sideWidth, h, d, color, $"Wall_{name}_L");
                // 右半墙
                CreateWallBlock(center + new Vector3(doorWidth / 2f + sideWidth / 2f, 0, 0),
                    sideWidth, h, d, color, $"Wall_{name}_R");
                // 门楣
                CreateWallBlock(center + new Vector3(0, h / 2f - 0.5f, 0),
                    doorWidth, 1f, d, color, $"Wall_{name}_Top");
            }
            else // 竖墙（东西）
            {
                sideWidth = (d - doorWidth) / 2f;
                CreateWallBlock(center + new Vector3(0, 0, -doorWidth / 2f - sideWidth / 2f),
                    w, h, sideWidth, color, $"Wall_{name}_L");
                CreateWallBlock(center + new Vector3(0, 0, doorWidth / 2f + sideWidth / 2f),
                    w, h, sideWidth, color, $"Wall_{name}_R");
                CreateWallBlock(center + new Vector3(0, h / 2f - 0.5f, 0),
                    w, 1f, doorWidth, color, $"Wall_{name}_Top");
            }
        }

        private void CreateWallBlock(Vector3 pos, float w, float h, float d, Color color, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = pos;
            wall.transform.localScale = new Vector3(w, h, d);
            wall.GetComponent<Renderer>().material.color = color;
            wall.name = name;
            wall.isStatic = true;
            var rb = wall.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            mapObjects.Add(wall);
        }

        /// <summary>创建机关门</summary>
        private void CreateGate(Vector3 position, float rotationY, string name)
        {
            var gateGo = new GameObject(name);
            gateGo.transform.position = position;
            gateGo.transform.rotation = Quaternion.Euler(0, rotationY, 0);
            var gate = gateGo.AddComponent<GateController>();
            gate.lockedDuration = 30f;
            gate.closeSpeed = 2f;
            gate.openSpeed = 1.5f;
            gate.requireHold = true;
            gate.holdTime = 1.5f;
            mapObjects.Add(gateGo);
        }

        private void CreatePond(Vector3 pos, float radius)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.position = pos + Vector3.up * 0.05f;
            obj.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
            var renderer = obj.GetComponent<Renderer>();
            renderer.material.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
            obj.name = "Pond";
            obj.isStatic = true;
            mapObjects.Add(obj);
        }

        private void CreateLantern(Vector3 pos)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.position = pos + Vector3.up * 1f;
            obj.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            obj.GetComponent<Renderer>().material.color = new Color(1f, 0.4f, 0.1f);
            obj.name = "Lantern";
            obj.isStatic = true;
            mapObjects.Add(obj);
        }

        private void CreateSpawnPoints(int count)
        {
            spawnPoints = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"SpawnPoint_{i}");
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                float dist = count <= 2 ? 5f : 15f;
                go.transform.position = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
                spawnPoints[i] = go.transform;
                mapObjects.Add(go);
            }
        }

        public Vector3 GetSpawnPoint(int index)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return Vector3.zero;
            return spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)].position;
        }

        public void ClearMap()
        {
            foreach (var obj in mapObjects)
            {
                if (obj != null) Destroy(obj);
            }
            mapObjects.Clear();
            destructibles.Clear();
        }
    }
}
