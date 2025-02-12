using System.Collections.Generic;
using UnityEngine;
public class QuestSystem : MonoBehaviour
{
    [SerializeField] private Quest[] m_questArchive;
    private List<Quest> m_activeQuests;
    [System.Serializable]
    private struct Quest
    {
        public bool m_isCompleted;
        public string m_questDescription;
        public QuestTask[] m_tasks;
    }
    [System.Serializable]
    private struct QuestTask
    {
        public bool m_isCompleted;
        public string m_taskDescription;
    }
}