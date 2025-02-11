using UnityEngine;
public class Compass : MonoBehaviour
{
    [SerializeField] private Transform[] m_mainTransforms;
    [SerializeField] private float m_mainSpeed;
    [SerializeField] private TrailLayer[] m_trailLayers;

    [SerializeField] private Transform m_main;
    [SerializeField] private float m_speed;

    private Transform m_camPivot;

    [System.Serializable]
    private struct TrailLayer
    {
        public Transform[] m_trailTransforms;
        public float m_trailSpeed;
    }
    void Start()
    {
        m_camPivot = GameManager.Instance().m_cameraCtrl.Parent.transform;
    }
    void Update()
    {
        UpdateRotation(m_main, m_speed, Quaternion.LookRotation(Vector3.ProjectOnPlane(m_camPivot.forward, Vector3.up)));
        for (int i = 0; i < m_mainTransforms.Length; i++)
        {
            UpdateRotation(m_mainTransforms[i], m_mainSpeed, Quaternion.LookRotation(Vector3.up, Vector3.forward));
            for (int j = 0; j < m_trailLayers.Length; j++)
            {
                UpdateTransform(m_trailLayers[j].m_trailTransforms[i], m_trailLayers[j].m_trailSpeed, m_mainTransforms[i].position, m_mainTransforms[i].rotation);
            }
        }
    }
    private void UpdateRotation(Transform t, float s, Quaternion q)
    {
        t.rotation = Quaternion.Slerp(t.rotation, q, s * Time.deltaTime);
    }
    private void UpdateTransform(Transform t, float s, Vector3 p, Quaternion q)
    {
        t.SetPositionAndRotation(Vector3.Slerp(t.position, p, s * Time.deltaTime),
            Quaternion.Slerp(t.rotation, q, s * Time.deltaTime));
    }
}