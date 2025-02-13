using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class QuestSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_questUITextPrefab;
    [SerializeField] private Transform m_questUIparent;
    [SerializeField] private Quest[] m_questArchive;
    private List<Quest> m_activeQuests;
    private List<TextMeshProUGUI> m_questUITexts;
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
    private void UpdateQuestUI()
    {
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
    }
    public void AddQuest(Quest q)
    {
        m_activeQuests.Add(q);
        UpdateQuestUI();
    }
    public void RemoveQuest(Quest q)
    {
        m_activeQuests.Remove(q);
        UpdateQuestUI();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L)) AddQuest(m_questArchive[0]);
    }
}