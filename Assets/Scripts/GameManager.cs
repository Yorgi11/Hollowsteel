using MGUtilities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameManager : Singleton_template<GameManager>
{
    [SerializeField] private bool m_enableAutoSave;
    [SerializeField] private float m_autoSaveTime;
    [SerializeField] private float m_distanceBetweenCharacters;
    [SerializeField] private float m_lookAtEnemySpeed;
    [Space]
    [SerializeField] private Transform m_moveTarget;
    [Space]
    [SerializeField] private List<Character> m_allCharacterPrefabs;
    [Space]
    [SerializeField] private GameObject m_moveMenu;
    [SerializeField] private GameObject m_combatMenu;
    [SerializeField] private GameObject m_buttonsParent;
    [SerializeField] private Arena m_currentArena;

    //[SerializeField] private string m_saveData = "";
    [Space]
    [SerializeField] private GameObject m_turnsDisplay;
    public enum PlayerState
    {
        Explore,
        Select,
        Wait,
        Move,
        Fight
    }
    public PlayerState m_currentPlayerState = PlayerState.Explore;
    private bool[] m_turnOrder;
    private int m_turnIndex;
    public bool CurrentTurn { get { return m_turnOrder[m_turnIndex]; } }

    private int m_activePlayerCharacterIndex = 0;
    private List<Character> m_playerCharacters = new();
    public Character ActivePlayerCharacter { get { return m_playerCharacters[m_activePlayerCharacterIndex]; } }

    private int m_currentEnemyIndex = 0;
    [SerializeField] private List<Enemy> m_enemyList = new();
    public Enemy CurrentEnemy { get { return m_enemyList[m_currentEnemyIndex]; } }

    public List<Character> AllCharacterPrefabs { get { return m_allCharacterPrefabs; } }

    // Inputs //
    public bool Mouse0 { get; private set; }
    public bool Mouse0Down { get; private set; }
    public bool Mouse0Up { get; private set; }

    public bool Mouse1 { get; private set; }
    public bool Mouse1Down { get; private set; }
    public bool Mouse1Up { get; private set; }

    public float MouseX { get; private set; }
    public float MouseY { get; private set; }
    public float MouseZ { get; private set; }

    private bool BackToSelect;
    private bool EndMovePhase;
    // Inputs //

    public CameraCTRL m_cameraCtrl;
    
    void Start()
    {
        m_cameraCtrl.InitCamera();

        m_turnOrder = new bool [100];

        // if saveData is empty
        InitTurnOrder();
        // else LoadSaveData()

        //SaveDataToFile(new Data(), "SaveGame1.json");
        if (m_enableAutoSave) StartCoroutine(AutoSave(m_autoSaveTime));
    }
    // Public Methods // vv
    public void AddEnemyToList(Enemy e)
    {
        m_enemyList.Add(e);
    }
    public Vector3 AveragePlayerPosition { get { return AveragePosition(m_playerCharacters); } }
    public Vector3 AverageEnemyPosition { get { return AveragePosition(CurrentEnemy.EnemyList); } }
    public void ChangeState(PlayerState state)
    {
        m_currentPlayerState = state;
    }
    public Character GetNearestEnemyCharacter(Vector3 activePosition)
    {
        return GetNearestCharacter(m_enemyList[m_currentEnemyIndex].EnemyList, activePosition);
    }
    public Character GetNearestPlayerCharacter(Vector3 activePosition)
    {
        return GetNearestCharacter(m_playerCharacters, activePosition);
    }
    public void EndTurn()
    {
        m_turnIndex++;
        if (m_turnIndex >= 100) m_turnIndex = 0;
        m_currentPlayerState = PlayerState.Select;
    }
    public void EnterArena(Enemy e)
    {
        m_currentPlayerState = PlayerState.Select;
        //m_currentEnemyIndex = m_enemyList.IndexOf(e);
        m_turnsDisplay.SetActive(true);
        ArenaInit(m_currentArena, e);
    }
    public void ExitArena()
    {
        m_turnsDisplay.SetActive(false);
        m_currentArena.gameObject.SetActive(true);
    }
    public void ToggleMenu(GameObject menuToToggle)
    {
        menuToToggle.SetActive(!menuToToggle.activeInHierarchy);
    }
    public void ToggleMenuFancy(GameObject menuToToggle)
    {
        bool state = !menuToToggle.activeInHierarchy;
        if (state) menuToToggle.SetActive(state);
        else StartCoroutine(DelaySetActive(menuToToggle, state, 0.2f));
        StartCoroutine(Coroutines.LerpVector3OverTime(state ? Vector3.zero : Vector3.one, state ? Vector3.one : Vector3.zero, 0.2f, value => menuToToggle.transform.localScale = value));
    }
    // Public Methods // ^^
    void Update()
    {
        // Inputs // vv
        Mouse0 = Input.GetKey(KeyCode.Mouse0);
        Mouse0Down = Input.GetKeyDown(KeyCode.Mouse0);
        Mouse0Up = Input.GetKeyUp(KeyCode.Mouse0);

        Mouse1 = Input.GetKey(KeyCode.Mouse1);
        Mouse1Down = Input.GetKeyDown(KeyCode.Mouse1);
        Mouse1Up = Input.GetKeyUp(KeyCode.Mouse1);

        MouseX = Input.GetAxisRaw("Mouse X");
        MouseY = Input.GetAxisRaw("Mouse Y");

        MouseZ = Input.GetAxisRaw("Mouse ScrollWheel");

        BackToSelect = Input.GetKey(KeyCode.E);
        EndMovePhase = Input.GetKey(KeyCode.R);
        // Inputs // ^^

        // UI // vv
        if (m_turnOrder.Length > 0 && m_turnsDisplay)
            UpdateTurnsDisplay();
        UpdateArena(m_currentArena);
        // UI // ^^

        m_cameraCtrl.UpdateCamera(Instance());

        if (Mouse0 && (m_currentPlayerState == PlayerState.Move || m_currentPlayerState == PlayerState.Explore))
        {
            MoveOnClick();
            if (m_currentPlayerState == PlayerState.Move)
            {
                m_currentPlayerState = PlayerState.Wait;
                StartCoroutine(WaitUntilCharacterStops());
            }
        }
    }
    // Private Methods // vv
    private IEnumerator AutoSave(float time)
    {
        while (m_enableAutoSave)
        {
            yield return new WaitForSeconds(time);

            // Update player Data
            if (ActivePlayerCharacter != null)
            {
                m_saveData.m_playerPos = ActivePlayerCharacter.transform.position;
            }

            // Update character data
            List<CharacterData> cdata = new();
            foreach (Character c in m_playerCharacters)
            {
                cdata.Add(new CharacterData
                {
                    m_currentHp = c.Health
                });
            }
            m_saveData.m_characters = cdata.ToArray();

            SaveDataToFile(m_saveData, "SaveGame1.json");
        }
    }
    private IEnumerator DelaySetActive(GameObject g, bool state, float time)
    {
        yield return new WaitForSeconds(time);
        g.SetActive(state);
    }
    private IEnumerator WaitUntilCharacterStops()
    {
        var t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        while (ActivePlayerCharacter.IsMoving)
        {
            yield return null;
        }

        m_currentPlayerState = PlayerState.Fight;
    }
    private void InitTurnOrder()
    {
        m_playerCharacters.Add(Instantiate(m_allCharacterPrefabs[0]));
        //m_playerCharacters.Add(Instantiate(m_allCharacterPrefabs[1]));
        //m_playerCharacters.Add(Instantiate(m_allCharacterPrefabs[2]));
        for (int i = 0; i < m_turnOrder.Length; i++)
        {
            m_turnOrder[i] = Random.Range(0f, 1f) > 0.5f;
        }
    }
    public void SpawnCharacters(ref List<Character> holdingList, int[] indexes)
    {
        for (int i = 0; i < indexes.Length - 1; i++)
        {
            if (!holdingList.Contains(m_allCharacterPrefabs[indexes[i]]))
            {
                holdingList.Add(Instantiate(m_allCharacterPrefabs[indexes[i]]));
            }
        }
    }
    public void UseAttack(int i)
    {
        ActivePlayerCharacter.UseAttack(i, true);
    }
    private void UpdateTurnsDisplay()
    {
        Image[] images = m_turnsDisplay.GetComponentsInChildren<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            images[i].color = m_turnOrder[m_turnIndex + i] ? new Color(0f, 0f, 500f, 1f) : new Color(500f, 0f, 0f, 1f);
        }
    }
    private Vector3 AveragePosition(List<Character> list)
    {
        Vector3 temp = Vector3.zero;
        foreach (Character c in list)
        {
            temp += c.transform.position;
        }
        return temp / list.Count;
    }
    private Character GetNearestCharacter(List<Character> clist, Vector3 activePosition)
    {
        float temp = Mathf.Infinity;
        Character closet = null;
        foreach (Character c in clist)
        {
            float d = Vector3.Distance(activePosition, c.transform.position);
            if (d < temp)
            {
                temp = d;
                closet = c;
            }
        }
        return closet;
    }
    #region Arena
    private void ArenaInit(Arena arena, Enemy e)
    {
        arena.transform.position = AveragePlayerPosition + 500f * Vector3.up;

        foreach (Character c in m_playerCharacters)
        {
            var temp = arena.m_playerSpawns[Random.Range(0, arena.m_playerSpawns.Count)];
            arena.m_playerSpawns.Remove(temp);
            c.transform.SetPositionAndRotation(temp.position, Quaternion.identity);
            c.WarpAgentPosition(c.transform.position);
            GameObject g = new()
            {
                name = c.name + "_Button"
            };
            g.transform.parent = m_buttonsParent.transform;
            g.AddComponent<CanvasRenderer>();
            Image im = g.AddComponent<Image>();
            im.sprite = c.m_ArenaSprite;
            Button b = g.AddComponent<Button>();
            b.targetGraphic = im;
            b.onClick.AddListener(() => SetActiveCharacter(c));
        }
        foreach (Character c in e.EnemyList)
        {
            var temp = arena.m_enemySpawns[Random.Range(0, arena.m_enemySpawns.Count)];
            arena.m_enemySpawns.Remove(temp);
            c.transform.SetPositionAndRotation(temp.position, Quaternion.identity);
            c.WarpAgentPosition(c.transform.position);
        }
        arena.gameObject.SetActive(true);
        ActivePlayerCharacter.WarpAgentPosition(arena.transform.position);
    }
    private void UpdateArena(Arena arena)
    {
        m_buttonsParent.SetActive(m_currentPlayerState == PlayerState.Select);
        m_moveMenu.SetActive(m_currentPlayerState == PlayerState.Move);
        m_combatMenu.SetActive(m_currentPlayerState == PlayerState.Fight);
        if (m_currentPlayerState != PlayerState.Fight)
        {
            m_combatMenu.transform.GetChild(0).gameObject.SetActive(true);
            m_combatMenu.transform.GetChild(1).gameObject.SetActive(true);
            m_combatMenu.transform.GetChild(2).gameObject.SetActive(false);
            m_combatMenu.transform.GetChild(3).gameObject.SetActive(false);
        }
    }
    private void SetActiveCharacter(Character character)
    {
        if (m_turnOrder[m_turnIndex])
        {
            m_activePlayerCharacterIndex = m_playerCharacters.IndexOf(character);
            m_currentPlayerState = PlayerState.Move;
        }
    }
    #endregion
    #region Movement
    private void MoveOnClick()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            if (m_playerCharacters.Count > 1) MoveAll(m_playerCharacters, hit.point);
            else m_playerCharacters[0].SetAgentTarget(hit.point);
            m_moveTarget.position = hit.point;
        }
    }
    public void MoveOnClick(List<Character> characters, Vector3 pos)
    {
        MoveAll(characters, pos);
    }
    public void MoveOne(Character character, Vector3 pos)
    {
        character.SetAgentTarget(pos);
    }
    public void MoveAll(List<Character> characters, Vector3 pos)
    {
        float angleStep = 360f / characters.Count;
        for (int i = 0; i < characters.Count; i++)
        {
            float angleInRadians = Mathf.Deg2Rad * angleStep * i;
            float x = pos.x + m_distanceBetweenCharacters * characters.Count * Mathf.Cos(angleInRadians);
            float z = pos.z + m_distanceBetweenCharacters * characters.Count * Mathf.Sin(angleInRadians);

            Vector3 targetPosition = new(x, pos.y, z);
            characters[i].SetAgentTarget(targetPosition);
        }
    }

    #endregion
    [System.Serializable]
    public struct Data
    {
        public Vector3 m_playerPos;
        public CharacterData[] m_characters;
    }
    [System.Serializable]
    public struct CharacterData
    {
        public float m_currentHp;
    }
    private Data m_saveData;
    private void SaveDataToFile(Data data, string fileName)
    {
        string jsonData = JsonUtility.ToJson(data, true);

        // Ensure the directory exists
        if (!Directory.Exists("Assets/SaveData"))
            Directory.CreateDirectory("Assets/SaveData");

        string filePath = Path.Combine("Assets/SaveData", "SaveGame1.json");
        if (File.Exists(Path.Combine("Assets/SaveData", fileName)))
            filePath = Path.Combine("Assets/SaveData", fileName);

        File.WriteAllText(filePath, jsonData);
    }
    // Private Methods // ^^
}
[System.Serializable]
public class CameraCTRL
{
    [SerializeField] private float m_camRotOffset;
    [SerializeField] private Vector3 m_camPosOffset;

    [SerializeField] private float m_mouseSensitivity;
    [SerializeField] private float m_mouseScrollSensitivity;
    [SerializeField] private float m_minVerticalAngle;
    [SerializeField] private float m_maxVerticalAngle;
    [SerializeField] private float m_minZoomDistance;
    [SerializeField] private float m_maxZoomDistance;
    [SerializeField] private Transform m_targetPosition;
    [SerializeField] private Transform m_cameraParent;
    [SerializeField] private Camera m_camera;
    private Transform m_transform;

    private float m_xRot = 0f;
    private float m_yRot = 0f;

    private Vector3 m_originalOffset;
    public Transform Parent {  get { return m_cameraParent; } }
    public void InitCamera()
    {
        m_transform = m_camera.transform;
        m_originalOffset = m_transform.localPosition;
    }
    public void UpdateCamera(GameManager gameManager)
    {
        Vector3 averagePosition = gameManager.AveragePlayerPosition;

        m_cameraParent.position = m_targetPosition ? m_targetPosition.position : averagePosition;
        m_transform.LookAt(averagePosition + m_camPosOffset);

        if (gameManager.Mouse1)
        {
            Cursor.visible = false;

            m_xRot -= (gameManager.MouseY * m_mouseSensitivity * Time.fixedDeltaTime);
            m_yRot += (gameManager.MouseX * m_mouseSensitivity * Time.fixedDeltaTime);

            if (m_xRot > m_maxVerticalAngle) m_xRot = m_maxVerticalAngle;
            if (m_xRot < m_minVerticalAngle) m_xRot = m_minVerticalAngle;

            m_transform.parent.rotation = Quaternion.Euler(m_xRot, m_yRot + m_camRotOffset, 0f);
        }
        else Cursor.visible = true;

        if (gameManager.MouseZ != 0f)
        {
            if (Input.GetKey(KeyCode.R)) m_transform.position = m_originalOffset;
            else
            {
                float dist = Vector3.Distance(m_transform.position + gameManager.MouseZ * m_mouseScrollSensitivity * m_transform.forward, averagePosition + m_camPosOffset);
                if (dist >= m_minZoomDistance && dist < m_maxZoomDistance) m_transform.position += gameManager.MouseZ * m_mouseScrollSensitivity * m_transform.forward;
            }
        }

        Vector3 dir = m_transform.position - (averagePosition + m_camPosOffset);
        if (dir.magnitude <= m_minZoomDistance) m_transform.position = m_minZoomDistance * dir.normalized;
        if (dir.magnitude >= m_maxZoomDistance) m_transform.position = m_maxZoomDistance * dir.normalized;
    }
}