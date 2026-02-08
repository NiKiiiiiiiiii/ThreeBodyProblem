using UnityEngine;
using UnityEngine.UI;

public class SimpleStartMenu : MonoBehaviour
{
    // Ссылки на UI элементы (назначим в инспекторе)
    public GameObject menuPanel;
    public Button blackHoleButton;
    public Button solarSystemButton;
    public Button customSimulationButton;
    public CustomSimulationCreator customSimulationCreator;
    // Ссылка на симуляцию
    public GravitySimulation gravitySimulation;

    void Start()
    {
        // 1. Показываем меню
        if (menuPanel != null)
            menuPanel.SetActive(true);

        // 2. Настраиваем кнопки
        if (blackHoleButton != null)
            blackHoleButton.onClick.AddListener(() => StartSimulation("BlackHole"));

        if (solarSystemButton != null)
            solarSystemButton.onClick.AddListener(() => StartSimulation("SolarSystem"));
        
        if (customSimulationButton != null)
            customSimulationButton.onClick.AddListener(OpenCustomSimulationMenu);

        // 3. Останавливаем симуляцию
        if (gravitySimulation != null)
        {
            gravitySimulation.enabled = false; // Отключаем полностью
            gravitySimulation.HideAllVisuals(); // Скрываем тела
        }

        // 4. Показываем курсор
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (customSimulationCreator != null && customSimulationCreator.customSimulationPanel != null)
            customSimulationCreator.customSimulationPanel.SetActive(false);
        Debug.Log("Меню загружено. Выберите симуляцию.");
        if (Input.GetKeyDown(KeyCode.Mouse1))
            {
            if (blackHoleButton.didStart)
            {
                StartSimulation("BlackHole");
            }
            else if (solarSystemButton.didStart)
            {
                StartSimulation("SolarSystem");
            }
            else if (customSimulationButton.didStart) 
            {
                OpenCustomSimulationMenu();
            }
        }
    }

    public void OpenCustomSimulationMenu()
    {
        Debug.Log("Открытие редактора кастомной симуляции");

        // 1. Скрываем главное меню
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (gravitySimulation != null)
        {
            gravitySimulation.enabled = false;
            gravitySimulation.HideAllVisuals();
        }
        // 2. Показываем панель кастомной симуляции
        if (customSimulationCreator != null)
        {
            customSimulationCreator.ShowCustomSimulationPanel();
            Debug.Log("Попытка запуска метода");
        }


        // 3. Останавливаем текущую симуляцию
        
    }
    void StartSimulation(string simulationType)
    {
        Debug.Log("Запускаем: " + simulationType);

        // 1. Скрываем меню
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 2. Включаем и настраиваем симуляцию
        if (gravitySimulation != null)
        {
            gravitySimulation.enabled = true;
            gravitySimulation.LoadPreset(simulationType);
            gravitySimulation.ShowAllVisuals();
        }

        if (customSimulationCreator != null && customSimulationCreator.customSimulationPanel != null)
            customSimulationCreator.customSimulationPanel.SetActive(false);

        // 3. Прячем курсор (для управления камерой)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Симуляция запущена. ESC - вернуться в меню.");
    }
    public void StartBlackHoleSimulation()
    {
        StartSimulation("BlackHole");
    }

    public void StartSolarSystemSimulation()
    {
        StartSimulation("SolarSystem");
    }
    void Update()
    {
        // Возврат в меню по ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }

    }

    public void ReturnToMenu()
    {
        // 1. Показываем меню
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        if (customSimulationCreator != null && customSimulationCreator.customSimulationPanel != null)
            customSimulationCreator.customSimulationPanel.SetActive(false);
        // 2. Останавливаем симуляцию
        if (gravitySimulation != null)
        {
            gravitySimulation.enabled = false;
            gravitySimulation.HideAllVisuals();
        }

        // 3. Показываем курсор
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Возврат в меню.");
    }

    void OnGUI()
    {
        // Простая подсказка в углу экрана
        if (menuPanel != null && !menuPanel.activeSelf)
        {
            GUI.Label(new Rect(10, 10, 300, 30), "ESC - вернуться в меню");
        }
    }
}