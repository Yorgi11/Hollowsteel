using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
    public enum State
    {
        Wander,
        Seek,
        Attack
    }
    private State m_currentState = State.Wander;

    [SerializeField] private float m_rotationSpeed;
    [SerializeField] private float m_angleMax;
    [SerializeField] private float m_seekDistance;
    [SerializeField] private float m_seekAngle;
    [SerializeField] private float m_attackDistance;
    [SerializeField] private float m_distanceBetweenCharacters;

    [SerializeField] private int[] m_indexes;
    public int m_activeIndex = 0;

    private float m_averageSpeed;
    private Vector3 m_wanderTarget;

    private bool m_wandering = false;
    private int m_currentCharacterIndex = 0;
    private List<Character> m_enemyCharacters = new();
    public List<Character> EnemyList { get { return m_enemyCharacters; } }
    public Character CurrentCharacter { get { return m_enemyCharacters[m_currentCharacterIndex]; } }

    private GameManager m_gameManager;
    void Start()
    {
        m_gameManager = GameManager.Instance();
        SpawnCharacters();
        SetAverageSpeed();
        m_averageSpeed /= m_enemyCharacters.Count;
        m_wanderTarget = transform.position + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));

        m_currentState = State.Wander;

        m_gameManager.SpawnCharacters(ref m_enemyCharacters, m_indexes);
    }
    void Update()
    {
        transform.position = m_gameManager.AverageEnemyPosition;

        if (m_currentState != State.Attack) CheckForTarget();
        if (m_currentState == State.Wander && !m_wandering) StartCoroutine(PickRandomWanderTarget());

        switch (m_currentState)
        {
            case State.Wander:
                Wander();
                break;
            case State.Seek:
                SeekNearestPlayer();
                break;
            case State.Attack:
                HandleAttacking();
                break;
        }

        CheckForEndOfBattle();
    }
    private void SetAverageSpeed()
    {
        foreach (Character character in m_enemyCharacters)
        {
            m_averageSpeed += character.Speed;
        }
    }
    private void SpawnCharacters()
    {
        for (int i = 0; i < m_indexes.Length; i++)
        {
            m_enemyCharacters.Add(Instantiate(m_gameManager.AllCharacterPrefabs[m_indexes[i]], transform.position, Quaternion.identity));
        }
    }
    private void CheckForEndOfBattle()
    {
        int count = 0;
        foreach (Character c in m_enemyCharacters)
        {
            if (c.Health <= 0) count++;
        }
        if (count >= m_enemyCharacters.Count) m_gameManager.ExitArena();
    }
    private void CheckForTarget()
    {
        Vector3 dist = m_gameManager.AveragePlayerPosition - transform.position;
        if (dist.magnitude <= m_attackDistance/* && Vector3.Dot(dist.normalized, transform.forward) < m_seekAngle*/) Engage();
        else if (dist.magnitude <= m_seekDistance/* && Vector3.Dot(dist.normalized, transform.forward) < m_seekAngle*/) m_currentState = State.Seek;
        else m_currentState = State.Wander;
    }
    private IEnumerator PickRandomWanderTarget()
    {
        while (m_currentState == State.Wander)
        {
            m_wandering = true;
            Vector3 offset = new(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            yield return new WaitForSeconds(offset.magnitude / m_averageSpeed);
            m_wanderTarget = transform.position + offset;
        }
        m_wandering = false;
    }
    private void Wander()
    {
        if (m_enemyCharacters.Count > 1) m_gameManager.MoveAll(m_enemyCharacters, m_wanderTarget);
        else m_gameManager.MoveOne(CurrentCharacter, m_wanderTarget);
    }
    private void SeekNearestPlayer()
    {
        m_gameManager.MoveAll(m_enemyCharacters, m_gameManager.GetNearestPlayerCharacter(transform.position).transform.position);
    }
    private void Engage()
    {
        m_currentState = State.Attack;
        m_gameManager.EnterArena(this);
    }
    private void HandleAttacking()
    {
        if (!m_gameManager.CurrentTurn)
        {
            CurrentCharacter.UseAttack(Random.Range(0, CurrentCharacter.m_attacks.Count), false);
        }
    }
}