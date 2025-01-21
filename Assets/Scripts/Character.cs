using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MGUtilities;
[System.Serializable]
public class Character : MonoBehaviour
{
    [SerializeField] private float m_maxHp;
    [SerializeField] private float m_speed;
    [SerializeField] private float m_resistance;
    [SerializeField] private AudioSource m_source;

    public List<Attack> m_attacks = new();

    public Sprite m_ArenaSprite;

    public LayerMask m_enemyLayers;

    private float m_currentHp = 0f;
    private float m_vel = 0f;

    private Attack m_currentAttack = null;

    private Animator m_animator;
    private NavMeshAgent m_agent;

    private GameManager m_gameManager;

    public bool IsMoving
    {
        get
        {
            return m_agent.velocity.sqrMagnitude > 0.01f && m_agent.remainingDistance > m_agent.stoppingDistance;
        }
    }
    public float Health { get { return m_currentHp; } set { m_currentHp -= value; } }
    public float Speed { get { return m_speed; } }
    public float Resistance { get { return m_resistance; } }
    void Start()
    {
        m_gameManager = GameManager.Instance();
        m_agent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();

        m_agent.speed = m_speed;
        m_agent.acceleration = m_speed;

        m_currentHp = m_maxHp;

        StartCoroutine(RandomizeIdle());
    }
    public void SetAgentTarget(Vector3 pos)
    {
        m_agent.SetDestination(pos);
    }
    public void WarpAgentPosition(Vector3 pos)
    {
        m_agent.Warp(pos);
    }
    public void UseAttack(int i, bool isPlayer)
    {
        if (m_currentAttack == null)
            StartCoroutine(RunAttack(m_attacks[i], i, isPlayer));
    }
    void Update()
    {
        m_vel = m_agent.velocity.magnitude;
        if (m_vel > 0.025f)
        {
            m_animator.SetBool("IsMoving", true);
            if (m_vel > 0.5f * m_speed) m_animator.SetFloat("Speed", 2f);
            else if (m_vel <= 0.5f * m_speed) m_animator.SetFloat("Speed", 1f);
            else m_animator.SetFloat("Speed", 0f);
        }
        else m_animator.SetBool("IsMoving", false);
        //float v = Mathf.Lerp(m_animator.GetFloat("Z"), m_vel, 10f * Time.deltaTime);
        //m_animator.SetFloat("Z", v);
    }
    private IEnumerator RandomizeIdle()
    {
        while (true)
        {
            int randomIdle = GetWeightedRandomIdle();

            m_animator.SetInteger("Idle", randomIdle);

            float animationDuration = GetIdleAnimationDuration(randomIdle);

            yield return new WaitForSeconds(animationDuration);
        }
    }

    private int GetWeightedRandomIdle()
    {
        int weightForIdle0 = 48; // Most common
        int weightForIdle1 = 6;  // 1/8 as common as Idle0
        int weightForIdle2 = 1;  // 1/6 as common as Idle1

        int totalWeight = weightForIdle0 + weightForIdle1 + weightForIdle2;

        int randomWeight = Random.Range(0, totalWeight);

        if (randomWeight < weightForIdle0) 
            return 0;
        else if (randomWeight < weightForIdle0 + weightForIdle1) 
            return 1;
        else 
            return 2;
    }
    private float GetIdleAnimationDuration(int idle)
    {
        return idle switch
        {
            0 => 2.533f,// Duration of Idle0
            1 => 3.667f,// Duration of Idle1
            2 => 7.533f,// Duration of Idle2
            _ => 2.533f,// Default to Idle0 duration if something goes wrong
        };
    }
    private IEnumerator RunAttack(Attack a, int i, bool isPlayer)
    {
        if (a.m_currentUses < a.m_maxUses)
        {
            m_currentAttack = a;
            m_animator.SetFloat("AttackIndex", i);
            a.m_currentUses++;

            AudioClip attackClip = a.m_attackSounds[Random.Range(0, a.m_attackSounds.Length)];
            m_source.clip = attackClip;
            m_source.Play();

            GameObject temp = new();
            //float totalWaitTime = attackClip.length / a.m_speedMulti;
            if (a.m_VFX)
            {
                if (a.m_moveVFX)
                {
                    //StartCoroutine(DelayMoveVFX(a, totalWaitTime - a.m_timeOffset - 0.1f));
                }
                else temp = Instantiate(a.m_VFX, transform.position + a.m_vfxOffset, transform.rotation);
            }

            if (a.m_currentUses >= a.m_maxUses) StartCoroutine(a.Reset());

            yield return new WaitForSeconds(a.m_clip.length);

            if (isPlayer) m_gameManager.GetNearestEnemyCharacter(transform.position).TakeDamage(a.m_damage);
            else m_gameManager.GetNearestPlayerCharacter(transform.position).TakeDamage(a.m_damage);

            if (a.m_attackHitSounds != null)
            {
                m_source.clip = a.m_attackHitSounds[Random.Range(0, a.m_attackHitSounds.Length)];
                m_source.Play();
            }

            m_currentAttack = null;
            m_animator.SetFloat("AttackIndex", -1);
            m_gameManager.EndTurn();
            Destroy(temp);
        }
    }
    private IEnumerator DelayMoveVFX(Attack a, float totalTime)
    {
        //Debug.Log(totalTime);
        yield return new WaitForSeconds(a.m_timeOffset);
        GameObject temp = Instantiate(a.m_VFX, transform.position + a.m_vfxOffset, transform.rotation);
        StartCoroutine(Coroutines.LerpVector3OverTime(temp.transform.position + a.m_vfxOffset, 
            m_gameManager.CurrentEnemy.CurrentCharacter.transform.position + a.m_vfxOffset, totalTime, value => temp.transform.position = value));
        Destroy(temp, totalTime + 0.1f);
    }
    private void TakeDamage(float d)
    {
        m_currentHp -= d;
        if (m_currentHp <= 0) Die();
        m_animator.SetInteger("HitIndex", Random.Range(0, 2));
        StartCoroutine(ResetHit());
        //Debug.Log(gameObject.name + ":\nTook: " + d + " Damage " + "Hp: " + m_currentHp);
    }
    private void Die()
    {
        m_currentHp = 0;
        //m_gameManager.ReturnToMainGame();
    }
    private IEnumerator ResetHit()
    {
        yield return new WaitForSeconds(1f);
        m_animator.SetInteger("HitIndex", -1);
    }
}
[System.Serializable]
public class Attack
{
    public float m_damage;
    public float m_speedMulti;
    public float m_resetTime;

    public float m_maxUses;
    public float m_currentUses;
    private int m_resetCount = 0;

    public bool m_moveVFX;
    public float m_timeOffset;
    public Vector3 m_vfxOffset;
    public GameObject m_VFX;
    public AnimationClip m_clip;
    public AudioClip[] m_attackSounds;
    public AudioClip[] m_attackHitSounds;
    public void UpdateAfterUse()
    {
        if (m_currentUses == m_maxUses)
        {
            m_resetCount++;
            if (m_resetCount == m_resetTime)
            {
                m_resetCount = 0;
                m_currentUses = 0;
            }
        }
    }
    public IEnumerator Reset()
    {
        yield return new WaitForSeconds(m_clip.length);
        m_currentUses = 0;
    }
}