using UnityEngine;
using UnityEngine.AI;
using Cmis3150.Fps.Core;
using Cmis3150.Fps.Player;

namespace Cmis3150.Fps.AI
{
    public enum GuardState
    {
        Patrol,
        Chase,
        PursueStolen,
        Flee,
        Dead
    }

    /// <summary>
    /// NavMeshAgent guard. Complete interaction to narrate in the walkthrough video:
    /// 1) ProtectionZone.OnTriggerEnter → OnPlayerEnteredZone → Chase + Shoot
    /// 2) OnTriggerExit → OnPlayerExitedZone → back to Patrol (unless box stolen)
    /// 3) NotifyTreasureStolen → PursueStolen forever
    /// 4) AllTreasuresCollected → Flee
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardAI : MonoBehaviour
    {
        [Header("Zone / Patrol")]
        [SerializeField] ProtectionZone zone;
        [SerializeField] float patrolReachThreshold = 0.6f;
        [SerializeField] float patrolWait = 0.4f;

        [Header("Combat")]
        [SerializeField] float shootRange = 18f;
        [SerializeField] float shootCooldown = 1.1f;
        [SerializeField] int bulletDamage = 1;
        [SerializeField] Transform muzzle;
        [SerializeField] LayerMask losMask = ~0;

        [Header("Flee")]
        [SerializeField] float fleeDistance = 12f;

        NavMeshAgent _agent;
        GuardState _state = GuardState.Patrol;
        Transform _player;
        float _nextShoot;
        float _patrolWaitUntil;
        bool _boxStolen;

        public GuardState State => _state;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (muzzle == null)
            {
                var m = transform.Find("Muzzle");
                muzzle = m != null ? m : transform;
            }
        }

        void OnEnable()
        {
            GameEvents.AllTreasuresCollected += OnAllTreasuresCollected;
        }

        void OnDisable()
        {
            GameEvents.AllTreasuresCollected -= OnAllTreasuresCollected;
        }

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterGuard();
            PickNewPatrolPoint();
        }

        void Update()
        {
            if (_state == GuardState.Dead) return;

            switch (_state)
            {
                case GuardState.Patrol:
                    TickPatrol();
                    break;
                case GuardState.Chase:
                case GuardState.PursueStolen:
                    TickChase(shoot: true);
                    break;
                case GuardState.Flee:
                    TickFlee();
                    break;
            }
        }

        public void BindZone(ProtectionZone z) => zone = z;

        public void OnPlayerEnteredZone(Transform player)
        {
            if (_state == GuardState.Dead || _state == GuardState.Flee) return;
            _player = player;
            if (_boxStolen)
                SetState(GuardState.PursueStolen);
            else
                SetState(GuardState.Chase);
        }

        public void OnPlayerExitedZone()
        {
            if (_state == GuardState.Dead || _state == GuardState.Flee) return;
            if (_boxStolen)
            {
                // Rubric: after steal, pursue forever even outside the zone.
                SetState(GuardState.PursueStolen);
                return;
            }

            _player = null;
            SetState(GuardState.Patrol);
            PickNewPatrolPoint();
        }

        public void NotifyTreasureStolen(Transform player)
        {
            _boxStolen = true;
            _player = player != null ? player : _player;
            if (_state == GuardState.Flee || _state == GuardState.Dead) return;
            SetState(GuardState.PursueStolen);
        }

        void OnAllTreasuresCollected()
        {
            if (_state == GuardState.Dead) return;
            SetState(GuardState.Flee);
        }

        public void NotifyDied()
        {
            _state = GuardState.Dead;
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;
        }

        void SetState(GuardState s)
        {
            _state = s;
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = false;
        }

        void TickPatrol()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            if (Time.time < _patrolWaitUntil) return;
            if (!_agent.pathPending && _agent.remainingDistance <= patrolReachThreshold)
            {
                _patrolWaitUntil = Time.time + patrolWait;
                PickNewPatrolPoint();
            }
        }

        void TickChase(bool shoot)
        {
            if (_player == null)
            {
                var pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            if (_player == null || _agent == null || !_agent.isOnNavMesh) return;
            _agent.SetDestination(_player.position);

            if (!shoot) return;
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= shootRange && Time.time >= _nextShoot)
                TryShoot();
        }

        void TickFlee()
        {
            if (_player == null)
            {
                var pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            if (_player == null || _agent == null || !_agent.isOnNavMesh) return;

            Vector3 away = (transform.position - _player.position).normalized;
            Vector3 target = transform.position + away * fleeDistance;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }

        void PickNewPatrolPoint()
        {
            if (_agent == null || !_agent.enabled) return;
            Vector3 center = zone != null ? zone.Center : transform.position;
            float radius = zone != null ? zone.Radius : 6f;

            for (int i = 0; i < 12; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(r.x, 0f, r.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    if (_agent.isOnNavMesh)
                        _agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        void TryShoot()
        {
            _nextShoot = Time.time + shootCooldown;
            if (_player == null) return;

            Vector3 origin = muzzle.position;
            Vector3 dir = (_player.position + Vector3.up * 1.0f - origin).normalized;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, shootRange, losMask))
            {
                var health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(bulletDamage);
            }
        }
    }
}
