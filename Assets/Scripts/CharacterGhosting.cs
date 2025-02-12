using UnityEngine;
public class CharacterGhosting : MonoBehaviour
{
    [SerializeField] private float m_speed;
    [SerializeField] private Transform[] m_targetBones;
    [SerializeField] private Transform[] m_trailBones;
    void Update()
    {
        for (int i = 0; i < m_targetBones.Length; i++)
        {
            m_trailBones[i].SetPositionAndRotation(Vector3.Slerp(m_trailBones[i].position, m_targetBones[i].position, m_speed * Time.deltaTime),
            Quaternion.Slerp(m_trailBones[i].rotation, m_targetBones[i].rotation, m_speed * Time.deltaTime));
        }
    }
}