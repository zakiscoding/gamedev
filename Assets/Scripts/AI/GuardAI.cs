using System.Collections;
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
    /// NavMeshAgent guard (state machine). Complete interaction for the walkthrough video:
    /// 1) ProtectionZone.OnTriggerEnter → OnPlayerEnteredZone → SetState(Chase)
    /// 2) Update → TickChase → agent.SetDestination(player) + TryShoot → PlayerHealth.TakeDamage
    /// 3) ProtectionZone.OnTriggerExit → OnPlayerExitedZone → SetState(Patrol) → PickNewPatrolPoint
    /// 4) TreasureBox.OnTriggerEnter → NotifyTreasureStolen → PursueStolen (chases anywhere)
    /// 5) GameEvents.AllTreasuresCollected → OnAllTreasuresCollected → Flee
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardAI : MonoBehaviour
    {
        [Header("Zone / Patrol")]
        [SerializeField] ProtectionZone zone;
        [Tooltip("If true, patrol inside patrolCenter/patrolRadius instead of the whole zone (e.g. guard inside the house).")]
        [SerializeField] bool usePatrolOverride;
        [SerializeField] Vector3 patrolCenter;
        [SerializeField] float patrolRadius = 3f;
        [SerializeField] float patrolReachThreshold = 0.6f;
        [SerializeField] float patrolWait = 0.8f;
        [SerializeField] float patrolSpeed = 2.0f;
        [SerializeField] float chaseSpeed = 3.4f;

        [Header("Combat")]
        [SerializeField] float shootRange = 16f;
        [SerializeField] float shootCooldown = 1.6f;
        [Range(0f, 1f)]
        [SerializeField] float hitChance = 0.35f;
        [SerializeField] int bulletDamage = 1;
        [SerializeField] Transform muzzle;
        [SerializeField] Material tracerMaterial;

        [Header("Flee")]
        [SerializeField] float fleeDistance = 12f;
        [SerializeField] float fleeSpeed = 3.0f;

        NavMeshAgent _agent;
        GuardState _state = GuardState.Patrol;
        Transform _player;
        float _nextShoot;
        float _patrolWaitUntil;
        bool _boxStolen;
        LineRenderer _tracer;

        public GuardState State => _state;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (muzzle == null)
            {
                var m = transform.Find("Muzzle");
                muzzle = m != null ? m : transform;
            }

            if (tracerMaterial != null)
            {
                _tracer = gameObject.AddComponent<LineRenderer>();
                _tracer.sharedMaterial = tracerMaterial;
                _tracer.positionCount = 2;
                _tracer.startWidth = 0.04f;
                _tracer.endWidth = 0.02f;
                _tracer.enabled = false;
                _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void OnEnable() => GameEvents.AllTreasuresCollected += OnAllTreasuresCollected;
        void OnDisable() => GameEvents.AllTreasuresCollected -= OnAllTreasuresCollected;

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterGuard();
            _agent.speed = patrolSpeed;
            _nextShoot = Time.time + 1f;

            // The agent is saved disabled and enabled here, after the NavMeshSurface has loaded
            // its baked data in OnEnable. If it is still off the mesh, snap it onto the NavMesh.
            _agent.enabled = true;
            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _agent.enabled = false;
                transform.position = hit.position;
                _agent.enabled = true;
            }
            PickNewPatrolPoint();
        }

        void Update()
        {
            switch (_state)
            {
                case GuardState.Patrol:
                    TickPatrol();
                    break;
                case GuardState.Chase:
                case GuardState.PursueStolen:
                    TickChase();
                    break;
                case GuardState.Flee:
                    TickFlee();
                    break;
            }
        }

        // ---------- setup (called by LevelBootstrap in the Editor) ----------
        public void BindZone(ProtectionZone z) => zone = z;

        public void SetPatrolArea(Vector3 center, float radius)
        {
            usePatrolOverride = true;
            patrolCenter = center;
            patrolRadius = radius;
        }

        public void SetMuzzle(Transform m, Material tracer)
        {
            muzzle = m;
            tracerMaterial = tracer;
        }

        // ---------- events from ProtectionZone / TreasureBox / GameEvents ----------

        public void OnPlayerEnteredZone(Transform player)
        {
            if (_state == GuardState.Dead || _state == GuardState.Flee) return;
            _player = player;
            SetState(_boxStolen ? GuardState.PursueStolen : GuardState.Chase);
        }

        public void OnPlayerExitedZone()
        {
            if (_state == GuardState.Dead || _state == GuardState.Flee) return;
            if (_boxStolen)
            {
                // Box stolen: keep pursuing the player even outside the zone.
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
            if (player != null) _player = player;
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
            if (_state == GuardState.Dead) return;
            _state = s;
            if (_agent == null || !_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.speed = s switch
            {
                GuardState.Patrol => patrolSpeed,
                GuardState.Flee => fleeSpeed,
                _ => chaseSpeed
            };
            _agent.stoppingDistance = s == GuardState.Patrol || s == GuardState.Flee ? 0.3f : 4f;
        }

        // ---------- per-frame behaviours ----------

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

        void TickChase()
        {
            EnsurePlayer();
            if (_player == null || _agent == null || !_agent.isOnNavMesh) return;

            // Safety net: if the player is clearly outside the zone but OnTriggerExit was missed
            // (e.g. after a respawn/teleport), treat it as leaving the zone.
            if (_state == GuardState.Chase && zone != null)
            {
                Vector3 d = _player.position - zone.Center;
                d.y = 0f;
                if (d.magnitude > zone.Radius + 1f)
                {
                    OnPlayerExitedZone();
                    return;
                }
            }

            _agent.SetDestination(_player.position);

            // Face the player while shooting.
            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 8f * Time.deltaTime);

            if (to.magnitude <= shootRange && Time.time >= _nextShoot)
                TryShoot();
        }

        void TickFlee()
        {
            EnsurePlayer();
            if (_player == null || _agent == null || !_agent.isOnNavMesh) return;
            if (_agent.pathPending) return;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > fleeDistance * 2f && _agent.remainingDistance > 0.5f) return;

            // Try a few directions away from the player and take the first reachable point.
            Vector3 away = transform.position - _player.position;
            away.y = 0f;
            away = away.sqrMagnitude < 0.01f ? transform.forward : away.normalized;
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, (i % 2 == 0 ? 1 : -1) * 25f * ((i + 1) / 2), 0f) * away;
                Vector3 target = transform.position + dir * fleeDistance;
                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        void EnsurePlayer()
        {
            if (_player != null) return;
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) _player = pc.transform;
        }

        void PickNewPatrolPoint()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
            Vector3 center = usePatrolOverride ? patrolCenter : (zone != null ? zone.Center : transform.position);
            float radius = usePatrolOverride ? patrolRadius : (zone != null ? zone.Radius * 0.85f : 6f);

            for (int i = 0; i < 12; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 candidate = new Vector3(center.x + r.x, 0f, center.z + r.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    var path = new NavMeshPath();
                    if (_agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete
                        && (!usePatrolOverride || PathStaysInArea(path, center, radius + 0.5f)))
                    {
                        _agent.SetPath(path);
                        return;
                    }
                }
            }
        }

        static bool PathStaysInArea(NavMeshPath path, Vector3 center, float radius)
        {
            foreach (var c in path.corners)
            {
                Vector2 d = new Vector2(c.x - center.x, c.z - center.z);
                if (d.magnitude > radius) return false;
            }
            return true;
        }

        void TryShoot()
        {
            _nextShoot = Time.time + shootCooldown * Random.Range(0.8f, 1.2f);
            if (_player == null) return;

            Vector3 origin = muzzle.position;
            Vector3 aimPoint = _player.position + Vector3.up * 1.2f;
            bool onTarget = Random.value < hitChance;
            if (!onTarget)
                aimPoint += Random.onUnitSphere * 1.5f; // miss

            Vector3 dir = (aimPoint - origin).normalized;
            Vector3 end = origin + dir * shootRange;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, shootRange, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(bulletDamage);
            }

            GameEvents.RaiseGuardFired(origin);
            if (_tracer != null) StartCoroutine(Flash(origin, end));
        }

        IEnumerator Flash(Vector3 a, Vector3 b)
        {
            _tracer.SetPosition(0, a);
            _tracer.SetPosition(1, b);
            _tracer.enabled = true;
            yield return new WaitForSeconds(0.06f);
            if (_tracer != null) _tracer.enabled = false;
        }
    }
}
