using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathMovement : MonoBehaviour
{
    public List<Transform> pathPoints;
    public List<Transform> textPositions;
    public bool lookAtCamera = true;
    public float mouseSensitivity = 2f;
    public float speedFactor = 5f;
    public float rotationSpeed = 5f;
    public bool useCustomTextOrder = false;
    public List<int> customTextOrder;
    public Color textColor = Color.white;
    public float glowIntensity = 1f;
    public float textFadeInTime = 1f;
    public float textDuration = 5f;
    public float textFadeOutTime = 1f;
    public float waypointStopDuration = 5f;
    public float waypointTriggerRadius = 0.5f;
    public FinalScreenManager finalScreenManager; 

    [Header("Camera & Parent")]
    public Transform cameraYawPivot;
    public Transform cameraPitchPivot;

    private Animator animator;
    private bool isLastWaypoint = false;
    private int currentPointIndex = 0;
    private float t = 0f;
    private bool isMoving = false;
    private bool isReversing = false;
    private bool canMove = true;
    private float headPitch = 0f;
    private Transform headBone;
    private Quaternion headBindRot = Quaternion.identity;
    private bool[] waypointVisited;
    private List<string> wordList;
    private GameObject currentWordDisplay;
    private Dictionary<int, GameObject> waypointTexts = new Dictionary<int, GameObject>();
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private int savedIndex = 0;

    void Start()
    {
        savedIndex = ConsentManager.listIndex; //interscenes

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            return;
        }

        headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        if (headBone != null)
            headBindRot = headBone.localRotation;

        if (cameraYawPivot == null)
        {
            cameraYawPivot = new GameObject("CameraYawPivot").transform;
            cameraYawPivot.SetParent(transform);
            cameraYawPivot.localPosition = Vector3.zero;
            cameraYawPivot.localRotation = Quaternion.identity;
        }
        if (cameraPitchPivot == null)
        {
            cameraPitchPivot = new GameObject("CameraPitchPivot").transform;
            cameraPitchPivot.SetParent(cameraYawPivot);
            cameraPitchPivot.localPosition = Vector3.zero;
            cameraPitchPivot.localRotation = Quaternion.identity;
        }
        if (Camera.main != null)
            Camera.main.transform.SetParent(cameraPitchPivot);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pathPoints != null && pathPoints.Count > 0)
            waypointVisited = new bool[pathPoints.Count];
        else
            Debug.LogWarning("Waypoints not defined.");


        Debug.Log("Index from ConsentManager : " + savedIndex + " and indeed actually "+ ConsentManager.listIndex);

        Permutations.CurrentIndex = savedIndex;
        wordList = Permutations.List[Permutations.CurrentIndex];

             /*   if (Permutations.List != null && Permutations.List.Count > 0)
                {
                    wordList = Permutations.List[Permutations.CurrentIndex];
                }
                else
                {
                    wordList = new List<string>();
                }
             */

        FindAndConfigureTexts();
    }

    void Update()
    {
        HandleCameraRotation();

        if (canMove)
            HandleInput();
        else
            isMoving = false;

        animator.SetBool(IsMovingHash, isMoving);

        if (isMoving)
        {
            MoveAlongPath();
            CheckWaypointReached();
        }
        if (lookAtCamera && currentWordDisplay != null && Camera.main != null)
        {
            currentWordDisplay.transform.LookAt(Camera.main.transform);
            currentWordDisplay.transform.Rotate(0f, 180f, 0f);
        }
    }

    void LateUpdate()
    {
        if (headBone == null)
            return;

        float yaw = cameraYawPivot.localEulerAngles.y;
        headBone.localRotation = headBindRot * Quaternion.Euler(headPitch, yaw, 0f);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isMoving = true;
            isReversing = false;
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isMoving = true;
            isReversing = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.LeftShift))
        {
            isMoving = false;
        }
    }
    /*
    void HandleCameraRotation()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        cameraYawPivot.Rotate(Vector3.up * mx, Space.Self);
        headPitch = Mathf.Clamp(headPitch - my, -90f, 90f);
        cameraPitchPivot.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
    }
    */

    void HandleCameraRotation()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        float currentYaw = cameraYawPivot.localEulerAngles.y;
        if (currentYaw > 180f)
            currentYaw -= 360f;

        float newYaw = Mathf.Clamp(currentYaw + mx, -90f, 90f);

        cameraYawPivot.localRotation = Quaternion.Euler(0f, newYaw, 0f);
        headPitch = Mathf.Clamp(headPitch - my, -90f, 90f);
        cameraPitchPivot.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
    }

    void MoveAlongPath()
    {
        if (pathPoints == null || pathPoints.Count < 2)
            return;

        Transform A = pathPoints[currentPointIndex];
        Transform B = pathPoints[Mathf.Min(currentPointIndex + 1, pathPoints.Count - 1)];
        float segLen = Vector3.Distance(A.position, B.position);
        t = Mathf.Clamp01(t + Time.deltaTime * speedFactor / segLen);
        Vector3 pos = Vector3.Lerp(A.position, B.position, t);
        pos.y = transform.position.y;
        transform.position = pos;

        Vector3 dir = (B.position - A.position).normalized;
        if (isReversing)
            dir = -dir;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion trg = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, trg, rotationSpeed * Time.deltaTime);
        }

        if (!isReversing)
        {
            if (t >= 1f)
            {
                t = 0f;
                currentPointIndex++;
                if (currentPointIndex >= pathPoints.Count)
                {
                    currentPointIndex = pathPoints.Count - 1;
                    isMoving = false;
                    StopAtWaypoint(currentPointIndex);
                }
            }
        }
        else
        {
            if (t <= 0f)
            {
                t = 1f;
                currentPointIndex--;
                if (currentPointIndex < 0)
                {
                    currentPointIndex = 0;
                    isMoving = false;
                }
            }
        }
    }

    void CheckWaypointReached()
    {
        float d = Vector3.Distance(transform.position, pathPoints[currentPointIndex].position);
        if (d <= waypointTriggerRadius && !waypointVisited[currentPointIndex])
        {
            waypointVisited[currentPointIndex] = true;
            StopAtWaypoint(currentPointIndex);
        }
    }

    void StopAtWaypoint(int i)
    {
        isLastWaypoint = (i == pathPoints.Count - 1);
        isMoving = false;
        canMove = false;
        if (waypointTexts.ContainsKey(i))
        {
            currentWordDisplay = waypointTexts[i];
            currentWordDisplay.SetActive(true);
            TMP_Text tmp = currentWordDisplay.GetComponent<TMP_Text>();
            if (tmp != null)
                StartCoroutine(FadeTextIn(tmp));
        }
        StartCoroutine(ResumeAfterDelay(waypointStopDuration));
    }

    void FindAndConfigureTexts()
    {
        waypointTexts.Clear();
        if (textPositions != null && textPositions.Count > 0)
        {
            for (int i = 0; i < textPositions.Count; i++)
            {
                if (textPositions[i] == null || i >= pathPoints.Count)
                    continue;
                int wi = useCustomTextOrder && i < customTextOrder.Count ? customTextOrder[i] : i;
                if (wi >= wordList.Count)
                    continue;
                TMP_Text tmp = textPositions[i].GetComponent<TMP_Text>();
                if (tmp == null)
                    continue;
                tmp.text = wordList[wi];
                tmp.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                if (glowIntensity > 0f && tmp.fontMaterial.HasProperty("_GlowPower"))
                {
                    tmp.fontMaterial.SetFloat("_GlowPower", glowIntensity);
                    tmp.fontMaterial.SetColor("_GlowColor", textColor);
                }
                tmp.gameObject.SetActive(false);
                waypointTexts[i] = tmp.gameObject;
            }
        }
        else
        {
            for (int i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i] == null)
                    continue;
                int wi = useCustomTextOrder && i < customTextOrder.Count ? customTextOrder[i] : i;
                if (wi >= wordList.Count)
                    continue;
                TMP_Text tmp = pathPoints[i].GetComponentInChildren<TMP_Text>(true);
                if (tmp == null)
                    continue;
                tmp.text = wordList[wi];
                tmp.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                if (glowIntensity > 0f && tmp.fontMaterial.HasProperty("_GlowPower"))
                {
                    tmp.fontMaterial.SetFloat("_GlowPower", glowIntensity);
                    tmp.fontMaterial.SetColor("_GlowColor", textColor);
                }
                tmp.gameObject.SetActive(false);
                waypointTexts[i] = tmp.gameObject;
            }
        }
    }

    IEnumerator FadeTextIn(TMP_Text tmesh)
    {
        float e = 0f;
        while (e < textFadeInTime)
        {
            float a = Mathf.Lerp(0f, 1f, e / textFadeInTime);
            tmesh.color = new Color(textColor.r, textColor.g, textColor.b, a);
            e += Time.deltaTime;
            yield return null;
        }
        tmesh.color = textColor;
        yield return new WaitForSeconds(textDuration);

    }

    IEnumerator FadeTextOut(TMP_Text tmesh)
    {
        float e = 0f;
        while (e < textFadeOutTime)
        {
            float a = Mathf.Lerp(1f, 0f, e / textFadeOutTime);
            tmesh.color = new Color(textColor.r, textColor.g, textColor.b, a);
            e += Time.deltaTime;
            yield return null;
        }
        tmesh.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
    }

    IEnumerator ResumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(textDuration, delay - textFadeOutTime));
        if (currentWordDisplay != null)
        {
            TMP_Text tmp = currentWordDisplay.GetComponent<TMP_Text>();
            if (tmp != null)
                yield return StartCoroutine(FadeTextOut(tmp));
            currentWordDisplay.SetActive(false);
            currentWordDisplay = null;
        }
        if (isLastWaypoint && finalScreenManager != null)
        {
            finalScreenManager.ShowFinalScreen();
        }
        else
        {
            canMove = true;
        }
    }

    void OnDrawGizmos()
    {
        if (pathPoints == null)
            return;
        Gizmos.color = Color.yellow;
        foreach (var p in pathPoints)
        {
            if (p != null)
                Gizmos.DrawWireSphere(p.position, waypointTriggerRadius);
        }
        if (textPositions == null)
            return;
        Gizmos.color = Color.cyan;
        foreach (var t in textPositions)
        {
            if (t != null)
                Gizmos.DrawWireCube(t.position, Vector3.one * 0.2f);
        }
    }
}
