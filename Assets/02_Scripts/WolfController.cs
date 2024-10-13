using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class MonsterController : MonoBehaviour, IDamagable
{
    public enum State
    {
        IDLE, TRACE, ATTACK, DIE
    }

    // 현재 몬스터의 상태
    public State state = State.IDLE;
    // 추적 사정거리
    [SerializeField] private float traceDist = 10.0f;
    // 공격 사정거리
    [SerializeField] private float attackDist = 2.0f;

    private Transform playerTr;
    private Transform monsterTr;
    private NavMeshAgent agent;
    private Animator animator;

    private readonly int hashIsTrace = Animator.StringToHash("IsTrace");
    private readonly int hashIsAttack = Animator.StringToHash("IsAttack");
    private readonly int hashHit = Animator.StringToHash("Hit");
    private readonly int hashDie = Animator.StringToHash("Die");
    private readonly int hashPlayerDie = Animator.StringToHash("PlayerDie");
    private readonly int hashDanceSpeed = Animator.StringToHash("DanceSpeed");

    public bool isDie = false;

    private float hp = 100.0f;

    void OnEnable()
    {

        StartCoroutine(CheckMonsterState());
        StartCoroutine(MonsterAction());
    }

    void OnDisable()
    {
    }

    void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("PLAYER");
        playerTr = playerObj?.GetComponent<Transform>();

        monsterTr = transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();


    }

    IEnumerator CheckMonsterState()
    {
        while (isDie == false)
        {
            // 몬스터의 상태가 DIE 일 경우 해당 코루틴을 정지
            if (state == State.DIE) yield break;

            //상태값을 측정
            float distance = Vector3.Distance(monsterTr.position, playerTr.position);

            if (distance <= attackDist)
            {
                state = State.ATTACK;
            }
            else if (distance <= traceDist)
            {
                state = State.TRACE;
            }
            else
            {
                state = State.IDLE;
            }
            Debug.Log("is?" + isDie);
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
                    agent.isStopped = true;
                    animator.SetBool(hashIsTrace, false);
                    Debug.Log("idle");
                    break;

                case State.TRACE:
                    agent.SetDestination(playerTr.position);
                    agent.isStopped = false; // 추적,이동 상태
                    animator.SetBool(hashIsAttack, false);
                    animator.SetBool(hashIsTrace, true);
                    break;

                case State.ATTACK:
                    agent.isStopped = true;
                    animator.SetBool(hashIsAttack, true);
                    break;

                case State.DIE:
                    isDie = true;
                    agent.isStopped = true;
                    animator.SetTrigger(hashDie);
                    GetComponent<CapsuleCollider>().enabled = false;
                    Invoke(nameof(ReturnPool), 3.0f);
                    break;
            }
            Debug.Log(state);
            yield return new WaitForSeconds(0.3f);
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


    public void YouWin()
    {
        animator.SetFloat(hashDanceSpeed, Random.Range(0.8f, 1.5f));
        animator.SetTrigger(hashPlayerDie);
        StopAllCoroutines();
        agent.isStopped = true;
    }
}