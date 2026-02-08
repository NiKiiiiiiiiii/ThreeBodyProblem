using UnityEngine;

public class SmartCamera : MonoBehaviour
{
    [Header("Camera Presets")]
    public CameraPreset[] cameraPresets;
    public int startPresetIndex = 0;

    [Header("Flight Controls")]
    public float flySpeed = 50f;
    public float sprintSpeed = 150f;
    public float rotationSpeed = 2f;
    public float zoomSpeed = 500f;
    public float smoothTransitionTime = 1.5f;

    [Header("Auto-framing Settings")]
    public float framingPadding = 1.5f; // Отступ при автофрейминге
    public float minFramingDistance = 20f;
    public float maxFramingDistance = 500f;

    [Header("Camera Settings")]
    public float minFieldOfView = 20f;
    public float maxFieldOfView = 100f;
    public float farClipPlane = 2000f;

    [System.Serializable]
    public class CameraPreset
    {
        public string presetName = "New Preset";
        public Vector3 position = new Vector3(0, 60, -80);
        public Vector3 rotation = new Vector3(25, 0, 0);
        public float fieldOfView = 60f;
        public string description = "";
    }

    // Private variables
    private Camera cam;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private bool cursorLocked = true;
    private bool isTransitioning = false;
    private Vector3 transitionStartPos;
    private Quaternion transitionStartRot;
    private float transitionStartFOV;
    private float transitionTimer = 0f;
    private CameraPreset targetPreset;

    // Режимы камеры
    public enum CameraMode { FreeFlight, Preset, Tracking }
    private CameraMode currentMode = CameraMode.FreeFlight;

    // Для отслеживания объектов
    private GravitySimulation gravitySimulation;
    private Transform trackingTarget;
    private Vector3 trackingOffset = new Vector3(0, 10, -20);

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = gameObject.AddComponent<Camera>();

        // Найти GravitySimulation в сцене
        gravitySimulation = FindObjectOfType<GravitySimulation>();

        // Initialize presets if none assigned
        if (cameraPresets == null || cameraPresets.Length == 0)
        {
            CreateDefaultPresets();
        }

        // Apply camera settings
        cam.farClipPlane = farClipPlane;

        // Set starting preset
        if (startPresetIndex >= 0 && startPresetIndex < cameraPresets.Length)
        {
            SetCameraPreset(startPresetIndex, false);
        }

        // Initialize rotation variables
        currentRotationX = transform.rotation.eulerAngles.x;
        currentRotationY = transform.rotation.eulerAngles.y;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"Камера инициализирована. Режим: {currentMode}");
    }

    void CreateDefaultPresets()
    {
        // Базовые пресеты для разных симуляций
        cameraPresets = new CameraPreset[]
        {
            // Чёрная дыра
            new CameraPreset()
            {
                presetName = "Black Hole Overview",
                position = new Vector3(0f, 80f, -120f),
                rotation = new Vector3(20f, 0f, 0f),
                fieldOfView = 65f,
                description = "Общий вид системы чёрной дыры"
            },
            new CameraPreset()
            {
                presetName = "Black Hole Close",
                position = new Vector3(0f, 30f, -40f),
                rotation = new Vector3(30f, 0f, 0f),
                fieldOfView = 50f,
                description = "Крупный план чёрной дыры"
            },
            
            // Солнечная система
            new CameraPreset()
            {
                presetName = "Solar System Wide",
                position = new Vector3(0f, 100f, -150f),
                rotation = new Vector3(25f, 0f, 0f),
                fieldOfView = 70f,
                description = "Широкий обзор солнечной системы"
            },
            new CameraPreset()
            {
                presetName = "Solar System Close",
                position = new Vector3(100f, 40f, -60f),
                rotation = new Vector3(20f, -45f, 0f),
                fieldOfView = 55f,
                description = "Вид на Землю и Луну"
            },
            
            // Общие пресеты
            new CameraPreset()
            {
                presetName = "Top Down",
                position = new Vector3(0f, 150f, 0f),
                rotation = new Vector3(90f, 0f, 0f),
                fieldOfView = 80f,
                description = "Вид сверху"
            },
            new CameraPreset()
            {
                presetName = "Free Flight Start",
                position = new Vector3(0f, 50f, -70f),
                rotation = new Vector3(25f, 0f, 0f),
                fieldOfView = 60f,
                description = "Стартовая позиция свободного полёта"
            }
        };
    }

    void Update()
    {
        // Переключение режима камеры
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SwitchToMode(CameraMode.FreeFlight);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            SwitchToMode(CameraMode.Preset);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            SwitchToMode(CameraMode.Tracking);
        }

        // Handle transitions
        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        // Toggle cursor lock
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleCursorLock();
        }

        // Quick preset selection with number keys (только в режиме пресетов)
        if (currentMode == CameraMode.Preset)
        {
            for (int i = 0; i < Mathf.Min(10, cameraPresets.Length); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    SetCameraPreset(i, true);
                }
            }
        }

        // Handle manual controls based on mode
        if (cursorLocked)
        {
            switch (currentMode)
            {
                case CameraMode.FreeFlight:
                    HandleFreeFlight();
                    break;
                case CameraMode.Preset:
                    HandlePresetMode();
                    break;
                case CameraMode.Tracking:
                    HandleTrackingMode();
                    break;
            }
        }

        // Быстрые действия
        if (Input.GetKeyDown(KeyCode.F))
        {
            FrameAllBodies();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetToCurrentPreset();
        }
    }

    void HandleFreeFlight()
    {
        // Полёт в направлении взгляда
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : flySpeed;
        speed *= Time.deltaTime;

        Vector3 move = Vector3.zero;

        // WASD - движение относительно взгляда
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;

        // Up/Down - глобальные оси
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * speed;
        }

        // Вращение камеры
        HandleRotation();

        // Зум
        HandleZoom();
    }

    void HandlePresetMode()
    {
        // В режиме пресетов можно только вращать и зумировать с позиции пресета
        HandleRotation();
        HandleZoom();

        // Можно добавить небольшую свободу движения
        float speed = flySpeed * 0.2f * Time.deltaTime;
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * speed;
        }
    }

    void HandleTrackingMode()
    {
        if (trackingTarget != null)
        {
            // Следим за целью
            Vector3 targetPosition = trackingTarget.position + trackingOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);
            transform.LookAt(trackingTarget.position);

            // Обновляем углы вращения
            currentRotationX = transform.rotation.eulerAngles.x;
            currentRotationY = transform.rotation.eulerAngles.y;
        }

        // Вращение и зум всё ещё доступны
        HandleRotation();
        HandleZoom();
    }

    void HandleRotation()
    {
        // Вращение при зажатой правой кнопке мыши
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            currentRotationY += mouseX;
            currentRotationX -= mouseY;
            currentRotationX = Mathf.Clamp(currentRotationX, -90f, 90f);

            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // Зум движением камеры
            float zoomAmount = scroll * zoomSpeed * Time.deltaTime;
            transform.position += transform.forward * zoomAmount;
        }
    }

    void SwitchToMode(CameraMode newMode)
    {
        if (currentMode == newMode) return;

        currentMode = newMode;

        switch (newMode)
        {
            case CameraMode.FreeFlight:
                Debug.Log("Режим: Свободный полёт (F1)");
                break;
            case CameraMode.Preset:
                Debug.Log("Режим: Пресеты (F2)");
                SetCameraPreset(5, true); // Переход к стартовой позиции
                break;
            case CameraMode.Tracking:
                Debug.Log("Режим: Слежение (F3)");
                // Автоматически выбрать первое тело для слежения
                AutoSelectTrackingTarget();
                break;
        }
    }

    void AutoSelectTrackingTarget()
    {
        if (gravitySimulation != null && gravitySimulation.bodies.Count > 0)
        {
            // Выбираем самое массивное тело для слежения
            BodyData largestBody = gravitySimulation.bodies[0];
            for (int i = 1; i < gravitySimulation.bodies.Count; i++)
            {
                if (gravitySimulation.bodies[i].mass > largestBody.mass)
                {
                    largestBody = gravitySimulation.bodies[i];
                }
            }

            // Находим визуальный объект этого тела
            GameObject targetVisual = GameObject.Find(largestBody.bodyName);
            if (targetVisual != null)
            {
                trackingTarget = targetVisual.transform;
                Debug.Log($"Слежение за: {largestBody.bodyName}");
            }
        }
    }

    // Автофрейминг - показать все тела
    public void FrameAllBodies()
    {
        if (gravitySimulation == null || gravitySimulation.bodies.Count == 0)
        {
            Debug.LogWarning("Нет тел для фрейминга");
            return;
        }

        // Находим границы всех тел
        Bounds bounds = new Bounds();
        bool first = true;

        foreach (var body in gravitySimulation.bodies)
        {
            Vector3 bodyPos = body.position;
            float bodyRadius = body.radius;

            // Расширяем bounds с учетом радиуса тела
            Bounds bodyBounds = new Bounds(bodyPos, Vector3.one * bodyRadius * 2);

            if (first)
            {
                bounds = bodyBounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(bodyBounds);
            }
        }

        // Вычисляем позицию камеры
        float distance = CalculateFramingDistance(bounds);
        Vector3 cameraDirection = new Vector3(-0.3f, 0.5f, -1f).normalized;
        Vector3 cameraPosition = bounds.center + cameraDirection * distance;

        // Плавный переход к новой позиции
        StartTransition(cameraPosition, Quaternion.LookRotation(bounds.center - cameraPosition), cam.fieldOfView);

        Debug.Log($"Автофрейминг: охвачено {gravitySimulation.bodies.Count} тел");
    }

    float CalculateFramingDistance(Bounds bounds)
    {
        // Вычисляем необходимую дистанцию для охвата bounds
        float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = objectSize * framingPadding / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

        // Ограничиваем дистанцию
        return Mathf.Clamp(distance, minFramingDistance, maxFramingDistance);
    }

    public void SetCameraForSimulationType(string simulationType)
    {
        switch (simulationType)
        {
            case "BlackHole":
                SetCameraPresetByName("Black Hole Overview");
                Debug.Log("Камера настроена для чёрной дыры");
                break;

            case "SolarSystem":
                SetCameraPresetByName("Solar System Wide");
                Debug.Log("Камера настроена для солнечной системы");
                break;

            case "Custom":
                // Для кастомной симуляции делаем автофрейминг
                FrameAllBodies();
                Debug.Log("Камера настроена для кастомной симуляции");
                break;

            default:
                SetCameraPresetByName("Free Flight Start");
                break;
        }
    }

    // Существующие методы с небольшими улучшениями
    public void SetCameraPreset(int presetIndex, bool smoothTransition = true)
    {
        if (presetIndex < 0 || presetIndex >= cameraPresets.Length)
        {
            Debug.LogWarning($"Неверный индекс пресета: {presetIndex}");
            return;
        }

        targetPreset = cameraPresets[presetIndex];

        if (smoothTransition && smoothTransitionTime > 0)
        {
            StartTransition(targetPreset.position,
                          Quaternion.Euler(targetPreset.rotation),
                          targetPreset.fieldOfView);
        }
        else
        {
            transform.position = targetPreset.position;
            transform.rotation = Quaternion.Euler(targetPreset.rotation);
            cam.fieldOfView = targetPreset.fieldOfView;

            currentRotationX = targetPreset.rotation.x;
            currentRotationY = targetPreset.rotation.y;
        }

        Debug.Log($"Установлен пресет: {targetPreset.presetName}");
    }

    void StartTransition(Vector3 targetPos, Quaternion targetRot, float targetFOV)
    {
        transitionStartPos = transform.position;
        transitionStartRot = transform.rotation;
        transitionStartFOV = cam.fieldOfView;
        transitionTimer = 0f;
        isTransitioning = true;

        // Сохраняем цели перехода
        this.targetPosition = targetPos;
        this.targetRotation = targetRot;
        this.targetFOV = targetFOV;
    }

    void UpdateTransition()
    {
        transitionTimer += Time.deltaTime / smoothTransitionTime;
        float t = Mathf.SmoothStep(0f, 1f, transitionTimer);

        transform.position = Vector3.Lerp(transitionStartPos, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transitionStartRot, targetRotation, t);
        cam.fieldOfView = Mathf.Lerp(transitionStartFOV, targetFOV, t);

        if (t >= 1f)
        {
            isTransitioning = false;
            currentRotationX = transform.rotation.eulerAngles.x;
            currentRotationY = transform.rotation.eulerAngles.y;
        }
    }

    // Добавляем эти поля для плавных переходов
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetFOV;

    void ToggleCursorLock()
    {
        cursorLocked = !cursorLocked;
        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !cursorLocked;
    }

    void ResetToCurrentPreset()
    {
        if (targetPreset != null)
        {
            SetCameraPreset(GetCurrentPresetIndex(), true);
        }
        else
        {
            // Если нет пресета, возвращаемся к виду свободного полёта
            SetCameraPresetByName("Free Flight Start");
        }
    }

    int GetCurrentPresetIndex()
    {
        if (targetPreset == null) return 0;

        for (int i = 0; i < cameraPresets.Length; i++)
        {
            if (cameraPresets[i].presetName == targetPreset.presetName)
                return i;
        }
        return 0;
    }

 

    // Public methods for UI
    public void SetPresetByIndex(int index) => SetCameraPreset(index, true);

    public void SetCameraPresetByName(string name)
    {
        for (int i = 0; i < cameraPresets.Length; i++)
        {
            if (cameraPresets[i].presetName == name)
            {
                SetCameraPreset(i, true);
                return;
            }
        }
        Debug.LogWarning($"Пресет '{name}' не найден");
    }

    public CameraPreset[] GetAvailablePresets() => cameraPresets;

    public string GetCurrentPresetInfo()
    {
        if (targetPreset == null) return "Свободный полёт";
        return $"{targetPreset.presetName}\n{targetPreset.description}";
    }

    void OnGUI()
    {
        // Компактная версия в правом нижнем углу
        float boxWidth = 250;
        float boxHeight = 150;
        float boxX = Screen.width - boxWidth - 10;
        float boxY = Screen.height - boxHeight - 10;

        GUILayout.BeginArea(new Rect(boxX, boxY, boxWidth, boxHeight));

        // Фон
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(0, 0, boxWidth, boxHeight), "");
        GUI.color = Color.white;

        // Содержимое
        GUILayout.BeginArea(new Rect(5, 5, boxWidth - 10, boxHeight - 10));

        // Заголовок
        GUILayout.Label("КАМЕРА", GetLabelStyle(Color.white, FontStyle.Bold, 12));

        // Режим
        string modeText = currentMode.ToString();
        Color modeColor = GetModeColor(currentMode);
        GUILayout.Label($"Режим: <color=#{ColorUtility.ToHtmlStringRGB(modeColor)}>{modeText}</color>",
                        GetRichTextStyle(11));

        if (currentMode == CameraMode.Tracking && trackingTarget != null)
        {
            GUILayout.Label($"Слежу: {trackingTarget.name}", GetLabelStyle(Color.yellow, FontStyle.Normal, 10));
        }

        GUILayout.Space(5);

        // Быстрые подсказки
        GUILayout.Label("Быстрые клавиши:", GetLabelStyle(Color.cyan, FontStyle.Bold, 10));
        GUILayout.Label("F1/F2/F3 - режимы", GetLabelStyle(Color.gray, FontStyle.Normal, 9));
        GUILayout.Label("F - показать все", GetLabelStyle(Color.gray, FontStyle.Normal, 9));
        GUILayout.Label("R - сброс камеры", GetLabelStyle(Color.gray, FontStyle.Normal, 9));

        GUILayout.EndArea();
        GUILayout.EndArea();
    }

    // Вспомогательные методы для стилей
    GUIStyle GetLabelStyle(Color color, FontStyle fontStyle, int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = color;
        style.fontStyle = fontStyle;
        style.fontSize = fontSize;
        return style;
    }

    GUIStyle GetRichTextStyle(int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        style.fontSize = fontSize;
        return style;
    }

    Color GetModeColor(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.FreeFlight: return Color.cyan;
            case CameraMode.Preset: return Color.yellow;
            case CameraMode.Tracking: return Color.green;
            default: return Color.white;
        }
    }
}