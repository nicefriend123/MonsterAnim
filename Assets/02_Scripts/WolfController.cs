using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class MonsterController : MonoBehaviour, IDamagable
{
    public enum State
    {
        IDLE, BATTLEIDLE, TRACE, ATTACK, DIE, WALKING, RUNNING
    }

    // 현재 몬스터의 상태
    public State state = State.IDLE;
    // 추적 사정거리
    [SerializeField] private float traceDist = 5.0f;
    // 공격 사정거리
    [SerializeField] private float attackDist = 2.0f;
    //[SerializeField] private float walkDist = 5.1f;

    private Transform playerTr;
    private Transform monsterTr;
    private NavMeshAgent agent;
    private Animator animator;

    private readonly int hashIsTrace = Animator.StringToHash("IsTrace");
    private readonly int hashIsAttack = Animator.StringToHash("IsAttack");
    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashIsDetected = Animator.StringToHash("IsDetected");
    private readonly int hashIsRunning = Animator.StringToHash("IsRunning");
    private readonly int hashHit = Animator.StringToHash("Hit");
    private readonly int hashDie = Animator.StringToHash("Die");
    private readonly int hashPlayerDie = Animator.StringToHash("PlayerDie");

    public bool isDie = false;

    private float distance;
    private float hp = 100.0f;

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
        while (isDie == false)
        {
            distance = Vector3.Distance(monsterTr.position, playerTr.position);

            // 몬스터의 상태가 DIE 일 경우 해당 코루틴을 정지
            if (state == State.DIE) yield break;

            //상태값을 측정
            if (distance <= attackDist)
            {
                state = State.ATTACK;
            }
            else if (distance == traceDist)
            {
                state = State.BATTLEIDLE;
            }
            else if (distance < traceDist)
            {
                state = State.RUNNING;
            }
            else if (distance > traceDist)
            {
                state = State.WALKING;
            }
            else
            {
                state = State.IDLE;
            }
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
                    agent.isStopped = false; // 이동 가능
                    break;

                case State.WALKING:
                    agent.isStopped = false;
                    animator.SetBool(hashIsWalking, true);
                    Walking();
                    Debug.Log("WALKING");
                    break;

                case State.BATTLEIDLE:
                    agent.isStopped = true;
                    animator.SetBool(hashIsDetected, true);
                    Debug.Log("Detected");
                    // 플레이어 방향을 계산
                    Vector3 directionToPlayer = playerTr.position - monsterTr.position;
                    directionToPlayer.y = 0; // Y축 회전을 방지하기 위해 Y값을 0으로 설정

                    // Quaternion을 사용하여 몬스터가 플레이어를 바라보도록 회전
                    if (directionToPlayer != Vector3.zero) // 방향이 유효한 경우
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                        monsterTr.rotation = Quaternion.Slerp(monsterTr.rotation, lookRotation, Time.deltaTime * 5f); // 회전 속도 조절
                    }
                    break;

                case State.RUNNING:
                    agent.isStopped = false;
                    animator.SetBool(hashIsRunning, true);
                    Debug.Log("Running");
                    break;

                // case State.TRACE:
                //     agent.SetDestination(playerTr.position);
                //     agent.isStopped = false; // 추적,이동 상태
                //     animator.SetBool(hashIsAttack, false);
                //     animator.SetBool(hashIsTrace, true);
                //     Debug.Log("TRACE");
                //     break;

                case State.ATTACK:
                    agent.isStopped = true;
                    animator.SetBool(hashIsAttack, true);
                    Debug.Log("ATTACK");
                    break;

                case State.DIE:
                    isDie = true;
                    agent.isStopped = true;
                    animator.SetTrigger(hashDie);
                    GetComponent<CapsuleCollider>().enabled = false;
                    Invoke(nameof(ReturnPool), 3.0f);
                    break;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }
    void Walking()
    {
        // if (state == State.IDLE)
        // {
        //     return;
        // }
        // 현재 위치에서 랜덤한 위치 생성
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),  // X축
            0,                      // Y축 (높이는 유지)
            Random.Range(-1f, 1f)   // Z축
        ).normalized; // 정규화하여 방향만 유지

        // 이동할 거리 설정
        float randomDistance = Random.Range(1f, 3f); // 1m에서 3m 사이의 거리

        // 랜덤 위치 계산
        Vector3 destination = monsterTr.position + randomDirection * randomDistance;

        // NavMeshAgent에게 랜덤 위치로 이동하라고 설정
        agent.SetDestination(destination);
        state = State.IDLE;
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


    public void YouWin()
    {
        animator.SetTrigger(hashPlayerDie);
        StopAllCoroutines();
        agent.isStopped = true;
    }
}