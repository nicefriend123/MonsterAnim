using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class MonsterController : MonoBehaviour, IDamagable
{
    public enum State
    {
        IDLE, BATTLEIDLE, ATTACK, DIE, WALKING, RUNNING
    }

    // 현재 몬스터의 상태
    public State state = State.IDLE;
    // 추적 사정거리
    [SerializeField] private float traceDist = 10.0f;
    // 공격 사정거리
    [SerializeField] private float attackDist = 2.0f;
    public bool isDie = false;

    private Transform playerTr;
    private Transform monsterTr;
    private NavMeshAgent agent;
    private Animator animator;

    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashIsDetected = Animator.StringToHash("IsDetected");
    private readonly int hashIsRunning = Animator.StringToHash("IsRunning");
    private readonly int hashIsAttack = Animator.StringToHash("IsAttack");
    private readonly int hashHit = Animator.StringToHash("Hit");
    private readonly int hashDie = Animator.StringToHash("Die");

    private readonly int hashBattleIdle = Animator.StringToHash("battleidle");

    private float distance;

    private float hp = 100.0f;
    private Vector3 targetPosition; // 몬스터가 이동할 랜덤한 좌표
    private bool battleMode = false;

    void OnEnable()
    {
        StartCoroutine(CheckMonsterState());
        StartCoroutine(MonsterAction());
    }

    void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("PLAYER");
        playerTr = playerObj.GetComponent<Transform>();

        monsterTr = transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    IEnumerator CheckMonsterState()
    {
        while (!isDie)
        {
            if (state == State.DIE) yield break;

            distance = Vector3.Distance(monsterTr.position, playerTr.position);

            if (distance <= attackDist)
            {
                state = State.ATTACK;
            }
            else if (distance <= traceDist)
            {
                if (state != State.RUNNING && state != State.BATTLEIDLE)
                {
                    state = State.BATTLEIDLE;
                    yield return new WaitForSeconds(2.0f);
                }

                if (state == State.BATTLEIDLE)
                {
                    state = State.RUNNING;
                }
            }
            else
            {
                //state = State.IDLE;
            }
            Debug.Log("CheckMonsterState" + state);
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator MonsterAction()
    {
        while (!isDie)
        {
            switch (state)
            {
                case State.IDLE:
                    agent.isStopped = true; // 이동 정지

                    animator.SetBool(hashIsWalking, true);  // Idle 상태 유지
                    targetPosition = GenerateRandomPosition(); // **목적지 생성**
                    state = State.WALKING;  // **Walking 상태로 전환**
                    break;

                case State.WALKING:
                    agent.isStopped = false;
                    agent.SetDestination(targetPosition);
                    //animator.SetBool(hashIsWalking, true);  // **걷기 애니메이션 트리거**
                    if (agent.remainingDistance <= agent.stoppingDistance)
                    {
                        state = State.IDLE;
                        animator.SetBool(hashIsWalking, false);  // Idle로 전환
                    }
                    break;

                case State.BATTLEIDLE:
                    agent.isStopped = true;
                    animator.SetBool(hashIsDetected, true);
                    break;

                case State.RUNNING:
                    agent.isStopped = false;
                    animator.SetBool(hashIsRunning, true);
                    agent.SetDestination(playerTr.position); // **플레이어를 추적**
                    break;

                case State.ATTACK:
                    agent.isStopped = true;
                    animator.SetBool(hashIsRunning, false);
                    animator.SetTrigger(hashIsAttack);  // 공격 애니메이션 트리거
                    break;

                case State.DIE:
                    isDie = true;
                    agent.isStopped = true;
                    animator.SetTrigger(hashDie);  // 죽음 애니메이션 트리거
                    GetComponent<CapsuleCollider>().enabled = false;
                    Invoke(nameof(ReturnPool), 3.0f);
                    break;
            }
            yield return new WaitForSeconds(0.3f); // 상태 전환 주기
        }
    }


    // 랜덤한 좌표를 생성하는 메서드 (IDLE 상태일 때 호출)
    Vector3 GenerateRandomPosition()
    {
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized;

        float randomDistance = Random.Range(1f, 3f); // 1~3미터 사이의 거리

        return monsterTr.position + randomDirection * randomDistance; // 새로운 목적지 반환
    }

    void LookAtPlayer()
    {
        Vector3 directionToPlayer = playerTr.position - monsterTr.position;
        directionToPlayer.y = 0; // Y축 회전을 방지하기 위해 Y값을 0으로 설정
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            monsterTr.rotation = Quaternion.Slerp(monsterTr.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void ReturnPool()
    {
        hp = 100.0f;
        isDie = false;
        state = State.IDLE;
        GetComponent<CapsuleCollider>().enabled = true;
        this.gameObject.SetActive(false);
    }

    public void OnDamaged()
    {
        animator.SetTrigger(hashHit);
        hp -= 20.0f;
        if (hp <= 0.0f)
        {
            state = State.DIE;
        }
    }
}
