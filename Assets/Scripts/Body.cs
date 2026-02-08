using UnityEngine;

[System.Serializable]
public class BodyData
{
    public string bodyName = "Body";
    public float mass = 100f;
    public Vector3 position = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
    public Color color = Color.white;
    public float radius = 1f;
    public float schwarzschildRadius = 0f; // Только для чёрной дыры
    public bool isCaptured = false; // Флаг захвата
    public float timeToLive = -1f; // Время до уничтожения при захвате
    public Vector3 capturePoint;
    // НОВЫЕ ПОЛЯ ДЛЯ ЗАХВАТА:
    public float captureStartTime = 0f;
    public float initialCaptureDistance = 0f;
    public Vector3 captureTangential = Vector3.zero;
    public float spiralPhase = 0f;

    // НОВЫЕ ПОЛЯ ДЛЯ МАНЁВРА:
    public bool hasGravityAssist = false;
    public float closestApproach = float.MaxValue;
    public Vector3 initialVelocity;

    [HideInInspector] public Vector3 acceleration = Vector3.zero;
    [HideInInspector] public Vector3 force = Vector3.zero;
}