using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlackHoleSettings
{
    public float eventHorizonScale = 1.0f; // Масштаб горизонта событий
    public Color accretionDiskColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
    public float diskRadius = 5.0f;
    public float rotationSpeed = 20.0f;
    public bool drawSchwarzschildRadius = true;
}
public class GravitySimulation : MonoBehaviour
{
    [Header("Preset Systems")]
    public bool usePresetOnStart = true;
    public string startPreset = "BlackHole";

    [Header("Start Settings")]
    public bool startPaused = true;
    public bool hideVisualsOnStart = true;
    
   

    [Header("Simulation Parameters")]
    public List<BodyData> bodies = new List<BodyData>();
    public float gravitationalConstant = 2f;
    public float timeScale = 0.5f;
    public float fixedDeltaTime = 0.01f;

    [Header("Visualization")]
    public GameObject bodyPrefab;

    [Header("Black Hole Settings")]
    public BlackHoleSettings blackHoleSettings = new BlackHoleSettings();
    public GameObject accretionDiskPrefab; // Префаб для аккреционного диска
    private GameObject accretionDiskInstance;
    private LineRenderer eventHorizonRenderer;

    [Header("Initial Conditions: Solar System or Black hole")]
    public bool usePreset = false;
    public string[] presetNames = new string[] { "Solar System", "Black hole" };

    private SmartCamera cameraController;

    public List<GameObject> bodyVisuals = new List<GameObject>();
    private List<int> bodiesToRemove = new List<int>();

    private float simulationTime = 0f;

    private float physicsAccumulator = 0f;
    private float physicsTimeStep = 0.02f;

    private bool isPaused = false;
    private bool visualsHidden = false;

    public void StartCustomSimulation(List<BodyData> bodiesList, float customGravitationalConstant = 5f, float customTimeScale = 0.25f)
    {
        if (cameraController != null)
        {
            cameraController.SetCameraForSimulationType("Custom");
        }
        // Очищаем текущую симуляцию
        bodies.Clear();
        ClearVisuals();

        // Устанавливаем параметры
        gravitationalConstant = customGravitationalConstant;
        timeScale = customTimeScale;
        fixedDeltaTime = 0.016f;

        this.enabled = true;
        gameObject.SetActive(true);

        // Добавляем тела
        foreach (BodyData bodyData in bodiesList)
        {
            bodies.Add(new BodyData()
            {
                bodyName = bodyData.bodyName,
                mass = bodyData.mass,
                position = bodyData.position,
                velocity = bodyData.velocity,
                color = bodyData.color,
                radius = bodyData.radius,
                acceleration = Vector3.zero,
                force = Vector3.zero
            });
        }

        // Создаем визуалы
        CreateVisuals();
        ShowAllVisuals();

        // Удаляем эффекты черной дыры (если были)
        if (accretionDiskInstance != null)
        {
            Destroy(accretionDiskInstance);
            accretionDiskInstance = null;
        }

        if (eventHorizonRenderer != null)
        {
            Destroy(eventHorizonRenderer.gameObject);
            eventHorizonRenderer = null;
        }

    }
    public void LoadPreset(string presetName)
    {
        bodies.Clear();
        ClearVisuals();
        if (cameraController != null)
        {
            if (presetName == "BlackHole")
            {
                cameraController.SetCameraPresetByName("Black Hole - Wide View");
            }
            else if (presetName == "SolarSystem")
            {
                cameraController.SetCameraPresetByName("Solar System - Sun View");
            }
        }

        if (accretionDiskInstance != null)
        {
            Destroy(accretionDiskInstance);
            accretionDiskInstance = null;
        }

        switch (presetName)
        {
            case "BlackHole":
                InitializeBlackHoleSystem();
                break;

            case "SolarSystem":
                InitializeSolarSystem();
                break;

            default:
                Debug.LogWarning($"Неизвестный пресет: {presetName}");
                InitializeBlackHoleSystem(); // По умолчанию
                break;
        }

        CreateVisuals();
        if (presetName == "BlackHole")
        {
            CreateBlackHoleEffects();
        }
        ShowAllVisuals();
        Debug.Log($"Загружен пресет: {presetName}");
    }
    void InitializeBlackHoleSystem()
    {
        gravitationalConstant = 10.0f;
        timeScale = 1f;
        fixedDeltaTime = 0.016f;

        // 1. ЧЁРНАЯ ДЫРА
        float bhMass = 10000f;
        bodies.Add(new BodyData()
        {
            bodyName = "BlackHole",
            mass = bhMass,
            position = Vector3.zero,
            velocity = Vector3.zero,
            color = Color.black,
            radius = 3f,
            schwarzschildRadius = 5f
        });

        // 2. ЗВЕЗДА
        bodies.Add(new BodyData()
        {
            bodyName = "Star",
            mass = 3000f,
            position = new Vector3(25f, 0, 0),
            velocity = new Vector3(0, 0, 8f),
            color = Color.yellow,
            radius = 2f,
            initialVelocity = new Vector3(0, 0, 8f)
        });

        // 3. КОМЕТА
        bodies.Add(new BodyData()
        {
            bodyName = "Comet",
            mass = 50f,
            position = new Vector3(70f, 20f, -25f),
            velocity = new Vector3(-22f, 0, 25f),
            color = new Color(0.3f, 0.7f, 1f),
            radius = 1f,
            initialVelocity = new Vector3(-18f, 0, 22f)
        });

        CreateBlackHoleEffects();
        if (accretionDiskInstance != null)
            accretionDiskInstance.SetActive(true);
    }

    void InitializeSolarSystem()
    {
        gravitationalConstant = 5f;
        timeScale = 1f;
        fixedDeltaTime = 0.016f;

        // 1. СОЛНЦЕ
        float sunMass = 200000f;
        bodies.Add(new BodyData()
        {
            bodyName = "Sun",
            mass = sunMass,
            position = new Vector3(0, 0, 0),
            velocity = new Vector3(0, 0, 0),
            color = new Color(1f, 0.9f, 0.3f, 1f),
            radius = 8f
        });

        // 2. ЗЕМЛЯ
        float earthOrbitRadius = 150f;
        float earthOrbitalSpeed = Mathf.Sqrt(gravitationalConstant * sunMass / earthOrbitRadius);

        bodies.Add(new BodyData()
        {
            bodyName = "Earth",
            mass = 500f,
            position = new Vector3(earthOrbitRadius, 0, 0),
            velocity = new Vector3(0, 0, earthOrbitalSpeed),
            color = new Color(0.2f, 0.4f, 0.9f, 1f),
            radius = 3f
        });

        // 3. ЛУНА
        float moonOrbitRadius = 2f;
        float moonOrbitalSpeedAroundEarth = Mathf.Sqrt(gravitationalConstant * 500f / moonOrbitRadius);

        Vector3 moonPosition = new Vector3(earthOrbitRadius + moonOrbitRadius, 0, 0);
        Vector3 moonVelocity = new Vector3(0, 0, earthOrbitalSpeed + moonOrbitalSpeedAroundEarth);

        bodies.Add(new BodyData()
        {
            bodyName = "Moon",
            mass = 1f,
            position = moonPosition,
            velocity = moonVelocity,
            color = new Color(0.8f, 0.8f, 0.8f, 1f),
            radius = 1f
        });

        // Удаляем эффекты чёрной дыры если они есть
        if (accretionDiskInstance != null)
        {
            Destroy(accretionDiskInstance);
            accretionDiskInstance = null;
        }

        if (eventHorizonRenderer != null)
        {
            Destroy(eventHorizonRenderer.gameObject);
            eventHorizonRenderer = null;
        }
    }

    void ClearVisuals()
    {
        // Очищаем визуальные объекты
        foreach (var visual in bodyVisuals)
        {
            if (visual != null) Destroy(visual);
        }
        bodyVisuals.Clear();

        // Очищаем эффекты чёрной дыры
        if (accretionDiskInstance != null)
        {
            Destroy(accretionDiskInstance);
            accretionDiskInstance = null;
        }

        if (eventHorizonRenderer != null)
        {
            Destroy(eventHorizonRenderer.gameObject);
            eventHorizonRenderer = null;
        }
    }

    public void ResetSimulation()
    {
        // Перезагружаем текущую систему
        string currentSystem = bodies.Count > 0 && bodies[0].bodyName == "BlackHole"
            ? "BlackHole"
            : "SolarSystem";
        LoadPreset(currentSystem);
    }
    void Start()
    {
        return;
 
    }

    public void PauseSimulation()
    {
        isPaused = true;
        timeScale = 0f;
        Debug.Log("Симуляция на паузе");
    }

    public void ResumeSimulation()
    {
        isPaused = false;
        timeScale = 1f; // или ваше значение по умолчанию
        Debug.Log("Симуляция возобновлена");
    }
    public void HideAllVisuals()
    {
        visualsHidden = true;
        foreach (var visual in bodyVisuals)
        {
            if (visual != null)
                visual.SetActive(false);
        }

        if (accretionDiskInstance != null)
            accretionDiskInstance.SetActive(false);

        if (eventHorizonRenderer != null)
            eventHorizonRenderer.gameObject.SetActive(false);

        Debug.Log("Визуальные элементы скрыты");
    }

    public void ShowAllVisuals()
    {
        visualsHidden = false;
        foreach (var visual in bodyVisuals)
        {
            if (visual != null)
                visual.SetActive(true);
        }

        if (accretionDiskInstance != null)
            accretionDiskInstance.SetActive(true);
        if (eventHorizonRenderer != null)
            eventHorizonRenderer.gameObject.SetActive(true);


        Debug.Log("Визуальные элементы показаны");
    }

    void FixedUpdate()
    {
        Debug.Log($"FixedUpdate вызван. Время: {Time.time}, Тела: {bodies.Count}, enabled: {enabled}, activeInHierarchy: {gameObject.activeInHierarchy}");
        if (!enabled)
        {
            Debug.LogError("Компонент GravitySimulation отключен!");
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("Объект GravitySimulation неактивен в иерархии!");
            return;
        }
        if (bodies.Count == 0) return;
        if (bodies.Count < 2) return;
        Debug.Log($"FixedUpdate: Запущена симуляция с {bodies.Count} телами, G={gravitationalConstant}");
        physicsAccumulator += Time.fixedDeltaTime * timeScale;
        

        while (physicsAccumulator >= physicsTimeStep)
        {
            // Шаг 1: Вычисление сил
            CalculateForces();
            // Шаг 2: Обновление состояний тел
            UpdateBodies(physicsTimeStep); // Всегда одинаковый шаг!

            physicsAccumulator -= physicsTimeStep;
            simulationTime += physicsTimeStep; // Симуляционное время тоже фиксированно
        }
        

        // Шаг 3: Обновление визуализации
        UpdateVisuals();


    }

    
    void CalculateForces()
    {
        // Обнуляем силы для всех тел
        foreach (var body in bodies)
        {
            body.force = Vector3.zero;
        }


        // ОТДЕЛЬНАЯ ОБРАБОТКА ЧЁРНОЙ ДЫРЫ И ТЕЛ
        if (bodies.Count > 0 && bodies[0].bodyName == "BlackHole")
        {
            BodyData blackHole = bodies[0];

            for (int i = 1; i < bodies.Count; i++) // Начинаем с 1, пропускаем ЧД
            {
                BodyData body = bodies[i];

                // Пропускаем уже захваченные тела
                if (body.isCaptured) continue;

                Vector3 direction = blackHole.position - body.position;
                float distance = direction.magnitude;

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Проверяем ЗАХВАТ по другому условию
                // Тело захватывается только если его энергия недостаточна для побега
                float escapeVelocity = Mathf.Sqrt(2f * gravitationalConstant * blackHole.mass / distance);
                float currentVelocity = body.velocity.magnitude;

                // УСЛОВИЕ ЗАХВАТА: скорость меньше 70% от скорости побега + близко к ЧД
                if (currentVelocity < escapeVelocity * 0.7f && distance < 30f)
                {
                    if (!body.isCaptured)
                    {
                        Debug.Log($"{body.bodyName} ЗАХВАЧЕНА! V={currentVelocity:F1}, V_escape={escapeVelocity:F1}");
                        body.isCaptured = true;
                        body.captureStartTime = simulationTime;
                        body.initialCaptureDistance = distance;

                        // Сохраняем начальный момент импульса для спирали
                        Vector3 radial = direction.normalized;
                        Vector3 tangential = Vector3.Cross(radial, Vector3.up).normalized;
                        body.captureTangential = tangential * body.velocity.magnitude * 0.3f;
                    }
                }

                // СИЛА ПРИТЯЖЕНИЯ (даже для захваченных тел, но с модификацией)
                float softening = 2.0f;
                float distanceSqr = distance * distance + softening * softening;
                float forceMagnitude = gravitationalConstant * (body.mass * blackHole.mass) / distanceSqr;

                // Для захваченных тел сила увеличивается (эффект "затягивания")
                if (body.isCaptured)
                {
                    float captureFactor = 1f + (1f - Mathf.Clamp01(distance / body.initialCaptureDistance)) * 3f;
                    forceMagnitude *= captureFactor;
                }

                Vector3 force = direction.normalized * forceMagnitude;
                body.force += force;
            }
        }

        // Расчет гравитации между ВСЕМИ парами тел
        for (int i = 0; i < bodies.Count; i++)
        {
            for (int j = i + 1; j < bodies.Count; j++)
            {
                // Пропускаем захваченные тела
                if (bodies[i].isCaptured || bodies[j].isCaptured) continue;

                // Особый случай: черная дыра уже обработана отдельно
                if (i == 0 && bodies[0].bodyName == "BlackHole")
                {
                    // Черная дыра неподвижна, сила на нее не действует
                    // Но другие тела к ней притягиваются (уже обработано)
                    continue;
                }

                Vector3 direction = bodies[j].position - bodies[i].position;
                float distance = direction.magnitude;
                if (distance < 0.1f) continue;

                float forceMagnitude = gravitationalConstant *
                    (bodies[i].mass * bodies[j].mass) / (distance * distance + 4f);

                Vector3 force = direction.normalized * forceMagnitude;
                bodies[i].force += force;
                bodies[j].force -= force;
            }
        }

    }

    void UpdateBodies(float dt)
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            BodyData body = bodies[i];

            // ЧЁРНАЯ ДЫРА - НЕПОДВИЖНА
            if (body.bodyName == "BlackHole")
            {
                body.position = Vector3.zero;
                body.velocity = Vector3.zero;
                continue;
            }

            // 1. ЗАХВАЧЕННЫЕ ТЕЛА (спиральное падение)
            if (body.isCaptured)
            {
                UpdateCapturedBody(body, i, dt);
                continue;
            }

            // 2. НЕЗАХВАЧЕННЫЕ ТЕЛА (обычная физика + отслеживание манёвра)
            body.acceleration = body.force / body.mass;
            body.velocity += body.acceleration * dt;
            body.position += body.velocity * dt;
        }

        RemoveCapturedBodies();
    }

    void UpdateCapturedBody(BodyData body, int index, float dt)
    {
        BodyData blackHole = bodies[0];
        Vector3 toBlackHole = blackHole.position - body.position;
        float distance = toBlackHole.magnitude;

        // Записываем минимальное расстояние для статистики
        if (distance < body.closestApproach)
            body.closestApproach = distance;

        // ПАРАМЕТРЫ СПИРАЛЬНОГО ПАДЕНИЯ
        float captureDuration = 5f; // Секунд до полного падения
        float timeSinceCapture = simulationTime - body.captureStartTime;
        float t = Mathf.Clamp01(timeSinceCapture / captureDuration);

        // Радиальное движение к центру (ускоряется со временем)
        float radialSpeed = 10f * Mathf.Pow(t, 2f) + 2f;
        Vector3 radialVelocity = toBlackHole.normalized * radialSpeed;

        // Тангенциальное движение (спираль, замедляется со временем)
        body.spiralPhase += 180f * dt * (1f - t * 0.8f); // Градусы в секунду
        Vector3 tangent = Quaternion.Euler(0, body.spiralPhase, 0) *
                         Vector3.Cross(toBlackHole, Vector3.up).normalized;
        float tangentialSpeed = 8f * (1f - t * 0.9f);
        Vector3 tangentialVelocity = tangent * tangentialSpeed;

        // Комбинированная скорость
        body.velocity = radialVelocity + tangentialVelocity;
        body.position += body.velocity * dt;

        // УМЕНЬШЕНИЕ РАЗМЕРА
        if (index < bodyVisuals.Count)
        {
            // Размер уменьшается по экспоненте
            float scaleFactor = Mathf.Exp(-t * 3f) * 0.5f + 0.5f * (1f - t);
            bodyVisuals[index].transform.localScale =
                Vector3.one * body.radius * 2f * Mathf.Max(0.1f, scaleFactor);

            // Эффект "раскаления" при приближении
            Renderer rend = bodyVisuals[index].GetComponent<Renderer>();
            if (rend != null)
            {
                float heat = 1f - distance / body.initialCaptureDistance;
                Color hotColor = Color.Lerp(body.color, Color.red, heat * 0.7f);
                rend.material.color = Color.Lerp(hotColor, Color.white, Mathf.PingPong(Time.time * 5f, 0.3f));
                rend.material.SetColor("_EmissionColor", hotColor * (0.5f + heat * 0.5f));
            }
        }

        // АВТОМАТИЧЕСКОЕ УДАЛЕНИЕ при очень близком расстоянии
        if (distance < 1f && timeSinceCapture > 2f)
        {
            body.timeToLive = 0f; // Помечаем для удаления
            Debug.Log($"{body.bodyName} достигла центра ЧД!");
        }
    }

    void UpdateFreeBody(BodyData body, int index, float dt)
    {
        // ОБЫЧНАЯ ФИЗИКА
        body.acceleration = body.force / body.mass;
        body.velocity += body.acceleration * dt;
        body.position += body.velocity * dt;

        // ОТСЛЕЖИВАЕМ БЛИЖАЙШЕЕ СБЛИЖЕНИЕ С ЧД
        if (bodies.Count > 0 && bodies[0].bodyName == "BlackHole")
        {
            float distanceToBH = Vector3.Distance(body.position, bodies[0].position);
            if (distanceToBH < body.closestApproach)
            {
                body.closestApproach = distanceToBH;

                // ДЕТЕКТИРУЕМ ГРАВИТАЦИОННЫЙ МАНЁВР
                if (distanceToBH < 20f && !body.hasGravityAssist)
                {
                    body.hasGravityAssist = true;
                    Debug.Log($"{body.bodyName} совершает гравитационный манёвр на расстоянии {distanceToBH:F1}!");

                    // Визуальный эффект для манёвра
                    if (index < bodyVisuals.Count)
                    {
                        StartCoroutine(GravityAssistEffect(bodyVisuals[index]));
                    }
                }
            }
        }

        // ОТЛАДКА: траектория тела
        if (Time.frameCount % 30 == 0)
        {
            Debug.DrawLine(body.position, body.position + body.velocity.normalized * 5f,
                          body.bodyName == "Comet" ? Color.cyan : Color.yellow, 1f);
        }
    }

    IEnumerator GravityAssistEffect(GameObject visual)
    {
        Renderer rend = visual.GetComponent<Renderer>();
        if (rend == null) yield break;

        Material mat = rend.material;
        Color originalEmission = mat.GetColor("_EmissionColor");

        // Мигание при манёвре
        for (int i = 0; i < 6; i++)
        {
            mat.SetColor("_EmissionColor", Color.blue * 2f);
            yield return new WaitForSeconds(0.1f);
            mat.SetColor("_EmissionColor", originalEmission);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void RemoveCapturedBodies()
    {
        // Удаляем в обратном порядке, чтобы не сбить индексы
        bodiesToRemove.Sort((a, b) => b.CompareTo(a));

        foreach (int index in bodiesToRemove)
        {
            if (index < bodies.Count && bodies[index].timeToLive <= 0)
            {
                // Визуальный эффект при удалении
                if (index < bodyVisuals.Count)
                {
                    StartCoroutine(PlayBlackHoleAbsorption(bodyVisuals[index]));
                }

                bodies.RemoveAt(index);
                if (index < bodyVisuals.Count)
                {
                    bodyVisuals.RemoveAt(index);
                }
            }
        }

        bodiesToRemove.Clear();

        IEnumerator PlayBlackHoleAbsorption(GameObject visual)
        {
            // Эффект "вспышки" при поглощении
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = rend.material;
                Color originalColor = mat.color;
                float duration = 0.5f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;

                    // Мерцание и уменьшение
                    mat.color = Color.Lerp(originalColor, Color.white, t);
                    mat.SetColor("_EmissionColor", Color.white * Mathf.Sin(t * Mathf.PI * 4f));
                    visual.transform.localScale *= 0.95f;

                    yield return null;
                }
            }

            Destroy(visual);
        }
    }

    void CreateVisuals()
    {
        // Очищаем старые визуальные объекты
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        bodyVisuals.Clear();



        // Создаём визуальные представления для каждого тела
        for (int i = 0; i < bodies.Count; i++)
        {
            BodyData body = bodies[i];
            GameObject visual;

            // Если назначен префаб, используем его
            if (bodyPrefab != null)
            {
                visual = Instantiate(bodyPrefab, transform);
                visual.name = body.bodyName;

                // Важно: удаляем коллайдеры, если они есть в префабе
                // чтобы они не мешали нашей собственной физике
                Collider col = visual.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Ищем все рендереры в префабе (включая вложенные)
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
                foreach (Renderer rend in renderers)
                {

                    // Создаём новый материал или меняем существующий
                    Material newMaterial = new Material(rend.material);
                    newMaterial.color = body.color;
                    newMaterial.EnableKeyword("_EMISSION");
                    newMaterial.SetColor("_EmissionColor", body.color * 0.3f);
                    rend.material = newMaterial;
                }

                // Если в префабе вообще нет рендереров, добавляем
                if (renderers.Length == 0)
                {
                    Renderer rend = visual.AddComponent<Renderer>();
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = body.color;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", body.color * 0.3f);
                    rend.material = mat;
                }
            }
            else
            {
                // Создаём простую сферу, если префаб не назначен
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = body.bodyName;
                visual.transform.parent = transform;
                Destroy(visual.GetComponent<Collider>());

                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (visual.name == "Earth")
                    {
                        Material emat = new Material(Shader.Find("EarthMat"));
                        //emat.color = body.color;
                        emat.EnableKeyword("_EMISSION");
                        emat.SetColor("_EmissionColor", body.color * 0.3f);
                        renderer.material = emat;
                    }
                    else { 
                        Material mat = new Material(Shader.Find("Standard"));
                        mat.color = body.color;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", body.color * 0.3f);
                        renderer.material = mat;
                    }
                }
            }

            // Устанавливаем позицию и масштаб
            visual.transform.position = body.position;
            visual.transform.localScale = Vector3.one * body.radius * 2;

            bodyVisuals.Add(visual);


            
        }
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            if (i < bodyVisuals.Count)
            {
                bodyVisuals[i].transform.position = bodies[i].position;

                // Динамическое изменение цвета при приближении к чёрной дыре
                if (i > 0 && bodies.Count > 0) // Не для самой чёрной дыры
                {
                    float distToBH = Vector3.Distance(bodies[i].position, bodies[0].position);
                    float dangerLevel = Mathf.Clamp01(1f - distToBH / 30f);

                    Renderer rend = bodyVisuals[i].GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Color originalColor = bodies[i].color;
                        Color dangerColor = Color.Lerp(originalColor, Color.red, dangerLevel);
                        rend.material.color = dangerColor;

                        // Эффект свечения в опасности
                        rend.material.SetColor("_EmissionColor", dangerColor * (0.3f + dangerLevel * 0.7f));
                    }
                }
            }
        }

        // Обновление аккреционного диска
        if (accretionDiskInstance != null && bodies.Count > 0)
        {
            accretionDiskInstance.transform.position = bodies[0].position;
            accretionDiskInstance.transform.Rotate(0, blackHoleSettings.rotationSpeed * Time.deltaTime, 0);
        }

        // Обновление горизонта событий
        if (eventHorizonRenderer != null && bodies.Count > 0)
        {
            eventHorizonRenderer.transform.position = bodies[0].position;
        }
    }
   
    void CreateBlackHoleEffects()
    {
        if (bodies.Count == 0) return;

        // 1. АККРЕЦИОННЫЙ ДИСК (вращающийся диск материи)
        if (accretionDiskPrefab != null)
        {
            accretionDiskInstance = Instantiate(accretionDiskPrefab, transform);
            accretionDiskInstance.name = "AccretionDisk";
            accretionDiskInstance.transform.position = bodies[0].position;
            accretionDiskInstance.SetActive(true);
            Debug.Log($"Аккреционный диск создан. Префаб: {accretionDiskPrefab.name}");

            // Настраиваем материал диска
            Renderer diskRenderer = accretionDiskInstance.GetComponent<Renderer>();
            if (diskRenderer != null)
            {
                Material mat = diskRenderer.material;
                mat.color = blackHoleSettings.accretionDiskColor;
                mat.SetFloat("_RotationSpeed", blackHoleSettings.rotationSpeed);
            }

            // Масштабируем диск
            float diskScale = blackHoleSettings.diskRadius * 2f;
            accretionDiskInstance.transform.localScale = new Vector3(diskScale, diskScale * 0.1f, diskScale);
        }

        // 2. ГОРИЗОНТ СОБЫТИЙ (сфера Шварцшильда)
        GameObject eventHorizonObj = new GameObject("EventHorizon");
        eventHorizonObj.transform.parent = transform;
        eventHorizonObj.transform.position = bodies[0].position;

        eventHorizonRenderer = eventHorizonObj.AddComponent<LineRenderer>();
        eventHorizonRenderer.startWidth = 0.05f;
        eventHorizonRenderer.endWidth = 0.05f;
        eventHorizonRenderer.material = new Material(Shader.Find("Unlit/Color"));
        eventHorizonRenderer.material.color = Color.red;
        eventHorizonRenderer.useWorldSpace = false;

        // Рисуем сферу (круг в 3D)
        DrawEventHorizonSphere(eventHorizonRenderer, blackHoleSettings.eventHorizonScale);

        // 3. ИСКАЖАЮЩИЙ ШЕЙДЕР (опционально, для эффекта гравитационной линзы)
        // Можно добавить пост-обработку или отдельный шейдер на камеру
    }

    void DrawEventHorizonSphere(LineRenderer lr, float radius)
    {
        int segments = 64;
        lr.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            lr.SetPosition(i, new Vector3(x, 0, z));
            angle += 360f / segments;
        }
    }

    void OnValidate()
    {
        // При изменении параметров в редакторе обновляем визуализацию
        if (Application.isPlaying)
        {
            CreateVisuals();
        }
    }

    void OnGUI()
    {
        
    }
}