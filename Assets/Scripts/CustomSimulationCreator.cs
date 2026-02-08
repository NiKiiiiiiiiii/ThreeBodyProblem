using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomSimulationCreator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject customSimulationPanel;
    public InputField bodyNameInput;
    public InputField massInput;
    public InputField radiusInput;
    public InputField posXInput, posYInput, posZInput;
    public InputField velXInput, velYInput, velZInput;
    public Image colorPreview;
    public Button colorPickerButton;
    public Slider redSlider, greenSlider, blueSlider;

    [Header("Other UI")]
    public Text bodiesCountText;
    public InputField gravConstantInput;
    public InputField timeScaleInput;
    public Button addBodyButton;
    public Button removeLastBodyButton;
    public Button startSimulationButton;
    public Button backButton;
    public Transform bodiesListContent;
    public GameObject bodyListItemPrefab;

    [Header("References")]
    public GravitySimulation gravitySimulation;
    public SimpleStartMenu startMenu;

    private List<BodyData> customBodies = new List<BodyData>();
    private Color currentColor = Color.white;

    void Start()
    {
        
        // Инициализация UI
        if (colorPreview != null)
            colorPreview.color = currentColor;

        // Подписка на события
        if (addBodyButton != null)
            addBodyButton.onClick.AddListener(AddBodyFromUI);

        if (removeLastBodyButton != null)
            removeLastBodyButton.onClick.AddListener(RemoveLastBody);

        if (startSimulationButton != null)
            startSimulationButton.onClick.AddListener(StartCustomSimulation);

        if (backButton != null)
            backButton.onClick.AddListener(BackToMainMenu);

        if (colorPickerButton != null)
            colorPickerButton.onClick.AddListener(OpenColorPicker);

        // Слайдеры цвета
        if (redSlider != null)
            redSlider.onValueChanged.AddListener(OnColorSliderChanged);
        if (greenSlider != null)
            greenSlider.onValueChanged.AddListener(OnColorSliderChanged);
        if (blueSlider != null)
            blueSlider.onValueChanged.AddListener(OnColorSliderChanged);

        // Начальные значения
        SetDefaultValues();
        customSimulationPanel.SetActive(false);
    }

    void SetDefaultValues()
    {
        if (bodyNameInput != null) bodyNameInput.text = "Body1";
        if (massInput != null) massInput.text = "1000";
        if (radiusInput != null) radiusInput.text = "1";
        if (posXInput != null) posXInput.text = "0";
        if (posYInput != null) posYInput.text = "0";
        if (posZInput != null) posZInput.text = "0";
        if (velXInput != null) velXInput.text = "0";
        if (velYInput != null) velYInput.text = "0";
        if (velZInput != null) velZInput.text = "0";
        if (gravConstantInput != null) gravConstantInput.text = "2";
        if (timeScaleInput != null) timeScaleInput.text = "1";

        // Цвет по умолчанию
        currentColor = new Color(0.5f, 0.8f, 1f);
        UpdateColorSliders();
        if (colorPreview != null)
            colorPreview.color = currentColor;
    }

    void OnColorSliderChanged(float value)
    {
        currentColor = new Color(redSlider.value, greenSlider.value, blueSlider.value);
        if (colorPreview != null)
            colorPreview.color = currentColor;
    }

    void UpdateColorSliders()
    {
        if (redSlider != null) redSlider.value = currentColor.r;
        if (greenSlider != null) greenSlider.value = currentColor.g;
        if (blueSlider != null) blueSlider.value = currentColor.b;
    }

    void OpenColorPicker()
    {
        // Можно добавить более продвинутый ColorPicker
        // Пока просто случайный цвет
        currentColor = new Color(Random.value, Random.value, Random.value);
        UpdateColorSliders();
        if (colorPreview != null)
            colorPreview.color = currentColor;
    }

    public void AddBodyFromUI()
    {
        // ОТЛАДКА: Проверяем, что поля не пустые
        Debug.Log("=== НАЧАЛО ДОБАВЛЕНИЯ ТЕЛА ===");
        Debug.Log($"Имя: {bodyNameInput?.text}");
        Debug.Log($"Масса: {massInput?.text}");

        // Проверяем все поля ввода
        if (bodyNameInput == null)
        {
            Debug.LogError("bodyNameInput не назначен!");
            return;
        }

        if (string.IsNullOrEmpty(bodyNameInput.text))
        {
            Debug.LogWarning("Введите имя тела!");
            return;
        }

        // Считываем значения ИЗ КОМПОНЕНТОВ InputField
        BodyData newBody = new BodyData
        {
            bodyName = bodyNameInput.text, // Текст из InputField

            // ОЧЕНЬ ВАЖНО: используем .text и парсим в float
            mass = ParseInputFieldToFloat(massInput, 1000f),
            radius = ParseInputFieldToFloat(radiusInput, 1f),

            position = new Vector3(
                ParseInputFieldToFloat(posXInput, 0f),
                ParseInputFieldToFloat(posYInput, 0f),
                ParseInputFieldToFloat(posZInput, 0f)
            ),

            velocity = new Vector3(
                ParseInputFieldToFloat(velXInput, 0f),
                ParseInputFieldToFloat(velYInput, 0f),
                ParseInputFieldToFloat(velZInput, 0f)
            ),

            color = currentColor
        };

        customBodies.Add(newBody);
        UpdateBodiesListUI();

        // Очищаем поля для следующего тела (кроме имени)
        // bodyNameInput.text = $"Body{customBodies.Count + 1}";

        Debug.Log($"Добавлено тело: {newBody.bodyName}");
        Debug.Log($"Масса: {newBody.mass}, Радиус: {newBody.radius}");
        Debug.Log($"Позиция: {newBody.position}");
        Debug.Log($"Скорость: {newBody.velocity}");
        Debug.Log("=== КОНЕЦ ДОБАВЛЕНИЯ ТЕЛА ===");
    }

    float ParseInputFieldToFloat(InputField inputField, float defaultValue)
    {
        if (inputField == null)
        {
            Debug.LogWarning($"InputField не назначен! Используется значение по умолчанию: {defaultValue}");
            return defaultValue;
        }

        if (string.IsNullOrEmpty(inputField.text))
        {
            Debug.Log($"Поле '{inputField.name}' пустое. Используется значение по умолчанию: {defaultValue}");
            return defaultValue;
        }

        if (float.TryParse(inputField.text, out float result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"Не удалось преобразовать '{inputField.text}' в число. Используется значение по умолчанию: {defaultValue}");
            return defaultValue;
        }
    }

    void UpdateBodiesListUI()
    {
        // Очищаем
        foreach (Transform child in bodiesListContent)
        {
            Destroy(child.gameObject);
        }

        // Заполняем
        for (int i = 0; i < customBodies.Count; i++)
        {
            BodyData body = customBodies[i];

            GameObject listItem = Instantiate(bodyListItemPrefab, bodiesListContent);

            // ПРОСТОЙ ПОИСК: Ищем любой TextMeshProUGUI в префабе
            TextMeshProUGUI tmpText = listItem.GetComponentInChildren<TextMeshProUGUI>();

            if (tmpText != null)
            {
                // Форматируем в одну строку для компактности
                string bodyInfo = $"{i + 1}. {body.bodyName} | " +
                                 $"M: {body.mass:F0} | " +
                                 $"Pos: ({body.position.x:F0},{body.position.y:F0},{body.position.z:F0})";

                tmpText.text = bodyInfo;
                tmpText.color = body.color;
            }

            // Настраиваем кнопку
            Button btn = listItem.GetComponentInChildren<Button>();
            if (btn != null)
            {
                int idx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => RemoveBodyAtIndex(idx));
            }
        }

        if (bodiesCountText != null)
            bodiesCountText.text = $"Тела: {customBodies.Count}";
    }

    void RemoveLastBody()
    {
        if (customBodies.Count > 0)
        {
            customBodies.RemoveAt(customBodies.Count - 1);
            UpdateBodiesListUI();
        }
    }

    void RemoveBodyAtIndex(int index)
    {
        if (index >= 0 && index < customBodies.Count)
        {
            customBodies.RemoveAt(index);
            UpdateBodiesListUI();
        }
    }

    void StartCustomSimulation()
    {
        if (customBodies.Count < 1)
        {
            Debug.LogWarning("Добавьте хотя бы одно тело!");
            return;
        }

        // Получаем параметры симуляции
        float gravConst = 5f;
        float timeScale = 10f;

        // Запускаем симуляцию
        if (gravitySimulation != null)
        {
            gravitySimulation.StartCustomSimulation(customBodies, gravConst, timeScale);

            // Скрываем панель
            if (customSimulationPanel != null)
                customSimulationPanel.SetActive(false);

            // Скрываем курсор
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

        }
    }

    void BackToMainMenu()
    {
        if (startMenu != null)
        {
            // Возвращаемся в главное меню
            startMenu.ReturnToMenu();

            // Скрываем панель кастомной симуляции
            if (customSimulationPanel != null)
                customSimulationPanel.SetActive(false);
        }
    }

    public void ShowCustomSimulationPanel()
    {
        if (customSimulationPanel != null)
        {
            customSimulationPanel.SetActive(true);

            // Сбрасываем данные
            customBodies.Clear();
            SetDefaultValues();
            UpdateBodiesListUI();

            // Показываем курсор
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Открыт редактор кастомной симуляции");
        }
    }

    float ParseFloat(string text, float defaultValue)
    {
        if (float.TryParse(text, out float result))
            return result;
        return defaultValue;
    }

    void Update()
    {
        // Горячие клавиши
        if (customSimulationPanel != null && customSimulationPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                AddBodyFromUI();
            else if (Input.GetKeyDown(KeyCode.Escape))
                BackToMainMenu();
        }
    }
}