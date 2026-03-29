using UnityEngine;

namespace QBlockyFighter.AI
{
    public enum AIState { Idle, Patrol, Chase, Attack, Stunned, Dead }

    public class EnemyAI : MonoBehaviour
    {
        public float AttackDamage = 15f;
        public float AttackRange = 2f;
        public float DetectRange = 10f;
        public float MoveSpeed = 3f;
        public float AttackCooldown = 1.5f;

        private AIState state = AIState.Idle;
        private Transform target;
        private HealthSystem health;
        private float attackTimer;
        private float patrolTimer;
        private Vector3 patrolTarget;
        private Rigidbody rb;

        void Start()
        {
            health = GetComponent<HealthSystem>();
            rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            state = AIState.Patrol;
            SetRandomPatrolTarget();
        }

        void Update()
        {
            if (health != null && health.IsDead) { state = AIState.Dead; return; }

            attackTimer -= Time.deltaTime;

            switch (state)
            {
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase();
                    break;
                case AIState.Attack:
                    UpdateAttack();
                    break;
                case AIState.Stunned:
                    break;
            }

            // 自动检测玩家
            if (state == AIState.Patrol || state == AIState.Idle)
            {
                FindTarget();
            }
        }

        private void FindTarget()
        {
            var players = FindObjectsOfType<PlayerController>();
            float minDist = DetectRange;
            Transform closest = null;
            foreach (var p in players)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = p.transform;
                }
            }
            if (closest != null)
            {
                target = closest;
                state = AIState.Chase;
            }
        }

        private void UpdatePatrol()
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0 || Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                SetRandomPatrolTarget();
            }
            MoveToward(patrolTarget, MoveSpeed * 0.5f);
        }

        private void UpdateChase()
        {
            if (target == null) { state = AIState.Patrol; return; }
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > DetectRange * 1.5f) { target = null; state = AIState.Patrol; return; }
            if (dist <= AttackRange) { state = AIState.Attack; return; }
            MoveToward(target.position, MoveSpeed);
            FaceTarget(target.position);
        }

        private void UpdateAttack()
        {
            if (target == null) { state = AIState.Patrol; return; }
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > AttackRange * 1.5f) { state = AIState.Chase; return; }

            if (attackTimer <= 0)
            {
                PerformAttack();
                attackTimer = AttackCooldown;
            }
        }

        private void PerformAttack()
        {
            if (target == null) return;
            var playerHP = target.GetComponent<HealthSystem>();
            if (playerHP != null && !playerHP.IsDead)
            {
                playerHP.TakeDamage(AttackDamage, gameObject);
            }
        }

        private void MoveToward(Vector3 targetPos, float speed)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;
            transform.position += dir * speed * Time.deltaTime;
        }

        private void FaceTarget(Vector3 targetPos)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        private void SetRandomPatrolTarget()
        {
            patrolTarget = transform.position + new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
            patrolTimer = Random.Range(3f, 6f);
        }

        public void Stun(float duration)
        {
            state = AIState.Stunned;
            Invoke(nameof(EndStun), duration);
        }

        private void EndStun()
        {
            state = target != null ? AIState.Chase : AIState.Patrol;
        }

        public AIState GetState() => state;
    }
}
