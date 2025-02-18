using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Patrol, Chase, BaseAttack, UltimateAttack, Dead }
public class EnemyController : MonoBehaviour
{
    #region Variables
    [Header("Enemy Attributes")]
    public Transform enemyTransform;
    public EnemyState enemyState;
    public NavMeshAgent navMeshAgent;
    public float life, maxLife, walkSpeed, runSpeed;
    public List<Transform> patrolPoints = new();
    public Animator enemyAnimations;

    [Header("Combat Attributes")]
    public bool hasBeenProvocated;
    public int probabilityOfUltimateAttack;
    public float damagePerSecond, areaSplashDamage;
    public ParticleSystem baseAttackParticle, ultimateAttackParticle;

    [Header("Sensor Attributes")]
    public Transform rayOriginPoint;
    public float noOfRays, visionRange, rayAngle, combatRange;
    public LayerMask playerMask;

    bool PlayerInsideCombatRange()
    {
        return Physics.CheckSphere(rayOriginPoint.position, combatRange, playerMask);
    }
    #endregion


    void Start()
    {
        Reset();
    }

    void Update()
    {
        SetBehaviour();
    }

    #region Behaviours
    void SetBehaviour()
    {
        OpenEyeSensors();
        if (hasBeenProvocated && !PlayerInsideCombatRange())
        {
            AssignAction(EnemyState.Chase);
        }
        if (hasBeenProvocated && PlayerInsideCombatRange())
        {
            AssignAction(EnemyState.BaseAttack);
        }
    }

    void AssignAction(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                if (ChaseRoutine != null)
                    StopCoroutine(ChaseRoutine);
                if (CombatRoutine != null)
                    StopCoroutine(CombatRoutine);

                PatrolRoutine = StartCoroutine(StartPatroling());
                break;
            case EnemyState.Chase:
                if (PatrolRoutine != null)
                    StopCoroutine(PatrolRoutine);
                if (CombatRoutine != null)
                    StopCoroutine(CombatRoutine);

                ChaseRoutine = StartCoroutine(ChasePlayer());
                break;
            case EnemyState.BaseAttack:
                if (PatrolRoutine != null)
                    StopCoroutine(PatrolRoutine);
                if (ChaseRoutine != null)
                    StopCoroutine(ChaseRoutine);

                CombatRoutine = StartCoroutine(StartCombat());
                break;
            default:
                break;
        }
    }

    Coroutine PatrolRoutine;
    IEnumerator StartPatroling()
    {
        SetState(EnemyState.Patrol);
        while (true)
        {
            yield return null;
            Transform destination = patrolPoints[Random.Range(0, patrolPoints.Count)];
            yield return StartCoroutine(MoveAgent(destination));
        }

    }

    Coroutine ChaseRoutine;
    IEnumerator ChasePlayer()
    {
        MainController.instance.uIController.EnemyHealthAppear();
        SetState(EnemyState.Chase);
        yield return StartCoroutine(MoveAgent(MainController.instance.playerController.playerTransform));
    }

    IEnumerator MoveAgent(Transform destination)
    {
        navMeshAgent.SetDestination(destination.position);

        navMeshAgent.stoppingDistance = enemyState == EnemyState.Chase ? combatRange : 0;
        navMeshAgent.speed = enemyState == EnemyState.Chase ? runSpeed : walkSpeed;

        while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
        {
            yield return null;
        }
    }

    void SetState(EnemyState state)
    {
        ParticleControl(state);
        switch (state)
        {
            case EnemyState.Patrol:
                enemyState = EnemyState.Patrol;
                SetAnimatorState(false, false, false);
                break;
            case EnemyState.Chase:
                enemyState = EnemyState.Chase;
                SetAnimatorState(true, false, false);
                break;
            case EnemyState.BaseAttack:
                enemyState = EnemyState.BaseAttack;
                SetAnimatorState(false, true, false);
                break;
            case EnemyState.UltimateAttack:
                enemyState = EnemyState.UltimateAttack;
                SetAnimatorState(false, false, true);
                break;
            case EnemyState.Dead:
                enemyState = EnemyState.Dead;
                break;
        }
    }

    void SetAnimatorState(bool run, bool baseAttack, bool ultimate)
    {
        enemyAnimations.SetBool("Base", baseAttack);
        enemyAnimations.SetBool("Run", run);
        enemyAnimations.SetBool("Ultimate", ultimate);
    }

    void OpenEyeSensors()
    {
        if (!hasBeenProvocated)
        {
            float step = rayAngle / (noOfRays - 1);

            for (int i = 0; i < noOfRays; i++)
            {
                float angle = -rayAngle / 2 + i * step;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

                if (Physics.Raycast(rayOriginPoint.position, direction, out RaycastHit hit, visionRange, playerMask))
                {
                    hasBeenProvocated = true;
                    break;
                }

                Vector3 endPoint = rayOriginPoint.position + direction * visionRange;
                Debug.DrawLine(rayOriginPoint.position, endPoint, Color.red);
            }
        }
    }
    #endregion

    #region Combat
    Coroutine CombatRoutine;
    bool isAttacking = false;
    IEnumerator StartCombat()
    {
        if (MainController.instance.playerController.life >= 1)
        {
            MainController.instance.uIController.EnemyHealthAppear();
            enemyTransform.LookAt(MainController.instance.playerController.playerTransform);
            if (isAttacking) yield break;
            isAttacking = true;

            while (PlayerInsideCombatRange())
            {
                yield return null;

                int prob = Random.Range(0, 100);
                if (prob <= probabilityOfUltimateAttack)
                {
                    SetState(EnemyState.UltimateAttack);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitWhile(() => enemyAnimations.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f);
                    MainController.instance.playerController.LifeDepletion(areaSplashDamage);
                }
                else
                {
                    SetState(EnemyState.BaseAttack);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitWhile(() => enemyAnimations.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f);
                    MainController.instance.playerController.LifeDepletion(damagePerSecond);
                }

                yield return new WaitForSeconds(1);
            }

            isAttacking = false;
        }
        else
        {
            yield break;
        }
    }

    void ParticleControl(EnemyState state)
    {
        if (state == EnemyState.UltimateAttack)
        {
            ultimateAttackParticle.Play();
            baseAttackParticle.Stop();
        }
        else if (state == EnemyState.BaseAttack)
        {
            ultimateAttackParticle.Stop();
            if (!baseAttackParticle.isPlaying)
                baseAttackParticle.Play();
        }
        else
        {
            ultimateAttackParticle.Stop();
            baseAttackParticle.Stop();
        }
    }

    public void LifeDepletion(float amount)
    {
        life -= amount; 
        life = Mathf.Clamp(life, 0, maxLife);
        MainController.instance.uIController.CalculateEnemyFillbar(life);

        if (life <= 0)
        {
            MainController.instance.uIController.EndScreen("WON!");
        }
    }
    #endregion

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(rayOriginPoint.position, combatRange);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Spell"))
        {
            MainController.instance.uIController.EnemyHealthAppear();
            collision.gameObject.SetActive(false);
            LifeDepletion(MainController.instance.playerController.combatController.damagePerSpell);
        }
    }

    void Reset()
    {
        if (PatrolRoutine == null)
            PatrolRoutine = StartCoroutine(StartPatroling());
        life = maxLife;
       MainController.instance.uIController.CalculateEnemyFillbar(life); 
    }
}
