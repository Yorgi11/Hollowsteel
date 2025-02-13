using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class QuestSystem : MonoBehaviour
{
    [SerializeField] private int m_maxActiveQuests;
    [SerializeField] private TextMeshProUGUI m_questText;
    [SerializeField] private TextMeshProUGUI m_questUITextPrefab;
    [SerializeField] private Transform m_questUIparent;
    [SerializeField] private Quest[] m_questArchive;
    private List<Quest> m_activeQuests = new();
    private List<TextMeshProUGUI> m_questUITexts = new();
    [System.Serializable]
    public struct Quest
    {
        public bool m_isCompleted;
        public string m_questDescription;
        public QuestTask[] m_tasks;
    }
    [System.Serializable]
    public struct QuestTask
    {
        public bool m_isCompleted;
        public string m_taskDescription;
    }
    private void Start()
    {
        UpdateQuestUI();
    }
    private void UpdateQuestUI()
    {
        if (m_activeQuests.Count > m_maxActiveQuests) return;
        int c = m_questUIparent.childCount;
        if (c < m_activeQuests.Count)
        {
            for (int i = 0; i < m_activeQuests.Count - c; i++)
            {
                m_questUITexts.Add(Instantiate(m_questUITextPrefab, m_questUIparent));
            }
        }
        for (int i = 0; i < m_activeQuests.Count; i++)
        {
            m_questUITexts[i].text = m_activeQuests[i].m_questDescription;
        }
        m_questText.text = $"Quests                {m_activeQuests.Count}/5";
    }
    public void AddQuest(Quest q)
    {
        m_activeQuests.Add(q);
        UpdateQuestUI();
    }
    public void RemoveQuest(Quest q)
    {
        int i = m_activeQuests.IndexOf(q);
        if (i < 0)
        {
            Debug.LogWarning("Attempted to remove a quest that doesn't exist in the active list.");
            return;
        }
        if (i < m_questUITexts.Count)
        {
            Destroy(m_questUITexts[i].gameObject);
            m_questUITexts.RemoveAt(i);
        }
        m_activeQuests.RemoveAt(i);
        UpdateQuestUI();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L)) AddQuest(m_questArchive[0]);
        if (Input.GetKeyDown(KeyCode.P)) AddQuest(m_questArchive[1]);
        if (Input.GetKeyDown(KeyCode.K)) RemoveQuest(m_questArchive[0]);
        if (Input.GetKeyDown(KeyCode.O)) RemoveQuest(m_questArchive[1]);
    }
}