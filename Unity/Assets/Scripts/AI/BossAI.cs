using UnityEngine;

namespace QBlockyFighter.AI
{
    public class BossAI : MonoBehaviour
    {
        public int BossType = 0; // 0=近战, 1=远程, 2=混合
        public float MaxHP = 5000f;
        public float AttackDamage = 40f;
        public float AttackRange = 3f;
        public float MoveSpeed = 2.5f;

        private HealthSystem health;
        private Transform target;
        private float attackTimer;
        private float phaseTimer;
        private int currentPhase = 0;
        private bool enraged = false;

        // Boss技能
        private float skillTimer;
        private float skillCooldown = 8f;

        void Start()
        {
            health = GetComponent<HealthSystem>();
            phaseTimer = 0;
        }

        void Update()
        {
            if (health != null && health.IsDead) return;

            attackTimer -= Time.deltaTime;
            skillTimer -= Time.deltaTime;
            phaseTimer += Time.deltaTime;

            FindTarget();

            // 阶段切换
            if (health != null)
            {
                float HpPercent = health.CurrentHp / health.MaxHp;
                if (HpPercent < 0.3f && currentPhase < 2) { currentPhase = 2; enraged = true; OnPhaseChange(); }
                else if (HpPercent < 0.6f && currentPhase < 1) { currentPhase = 1; OnPhaseChange(); }
            }

            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.position);

                if (dist <= AttackRange)
                {
                    if (attackTimer <= 0)
                    {
                        BossAttack();
                        attackTimer = enraged ? 0.8f : 1.5f;
                    }
                }
                else
                {
                    MoveToward(target.position);
                }

                // 释放技能
                if (skillTimer <= 0)
                {
                    UseBossSkill();
                    skillTimer = enraged ? skillCooldown * 0.6f : skillCooldown;
                }
            }
        }

        private void FindTarget()
        {
            var players = FindObjectsOfType<PlayerController>();
            float minDist = Mathf.Infinity;
            foreach (var p in players)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = p.transform;
                }
            }
        }

        private void BossAttack()
        {
            if (target == null) return;
            float dmg = AttackDamage * (enraged ? 1.5f : 1f);
            var hp = target.GetComponent<HealthSystem>();
            if (hp != null) hp.TakeDamage(dmg, gameObject);
        }

        private void UseBossSkill()
        {
            switch (BossType)
            {
                case 0: // 近战Boss - 旋风斩
                    MeleeSpinAttack();
                    break;
                case 1: // 远程Boss - 弹幕
                    RangedBarrage();
                    break;
                case 2: // 混合Boss - 召唤
                    SummonMinions();
                    break;
            }
        }

        private void MeleeSpinAttack()
        {
            // AOE范围攻击
            var colliders = Physics.OverlapSphere(transform.position, 5f);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Player"))
                {
                    var hp = col.GetComponent<HealthSystem>();
                    if (hp != null) hp.TakeDamage(AttackDamage * 2f, gameObject);
                }
            }
            Debug.Log("[Boss] 旋风斩!");
        }

        private void RangedBarrage()
        {
            // 发射多方向弹幕
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                // 实际项目中应实例化弹幕预制体
                Debug.Log($"[Boss] 弹幕方向 {angle}°");
            }
        }

        private void SummonMinions()
        {
            // 召唤小怪
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * 5f;
                pos.y = 0;
                var minion = GameObject.CreatePrimitive(PrimitiveType.Cube);
                minion.transform.position = pos;
                minion.transform.localScale = Vector3.one * 0.8f;
                minion.name = "BossMinion";
                minion.tag = "Enemy";
                var mhp = minion.AddComponent<HealthSystem>();
                mhp.MaxHp = 100;
                mhp.CurrentHp = 100;
                var ai = minion.AddComponent<EnemyAI>();
                ai.AttackDamage = 10;
            }
            Debug.Log("[Boss] 召唤小怪!");
        }

        private void OnPhaseChange()
        {
            Debug.Log($"[Boss] 进入阶段 {currentPhase + 1}" + (enraged ? " [狂暴]" : ""));
            // 阶段切换时的视觉效果由特效系统处理
        }

        private void MoveToward(Vector3 targetPos)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;
            float speed = enraged ? MoveSpeed * 1.3f : MoveSpeed;
            transform.position += dir * speed * Time.deltaTime;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
        }

        public int GetCurrentPhase() => currentPhase;
        public bool IsEnraged() => enraged;
    }
}
