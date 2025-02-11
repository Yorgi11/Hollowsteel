using UnityEngine;
public class Compass : MonoBehaviour
{
    [SerializeField] private Transform m_main;
    [SerializeField] private float m_mainSpeed;
    [SerializeField] private Transform m_N;
    [SerializeField] private float m_NSpeed;
    [SerializeField] private Transform m_S;
    [SerializeField] private float m_SSpeed;
    [SerializeField] private Transform m_E;
    [SerializeField] private float m_ESpeed;
    [SerializeField] private Transform m_W;
    [SerializeField] private float m_WSpeed;
    private Transform m_camPivot;
    private Transform m_cam;
    void Start()
    {
        m_camPivot = GameManager.Instance().m_cameraCtrl.Parent.transform;
        m_cam = m_camPivot.GetChild(0);
    }
    void Update()
    {
        UpdateRotation(m_main, m_mainSpeed, Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up)));
        UpdateRotation(m_N, m_NSpeed, Quaternion.LookRotation((m_cam.position - m_N.position).normalized));
        UpdateRotation(m_S, m_SSpeed, Quaternion.LookRotation((m_cam.position - m_S.position).normalized));
        UpdateRotation(m_E, m_ESpeed, Quaternion.LookRotation((m_cam.position - m_E.position).normalized));
        UpdateRotation(m_W, m_WSpeed, Quaternion.LookRotation((m_cam.position - m_W.position).normalized));
    }
    private void UpdateRotation(Transform t, float s, Quaternion q)
    {
        t.rotation = Quaternion.Slerp(t.rotation, q, s * Time.deltaTime);
    }
}