using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>第三幕月光试作；只调整视觉，不改变追逐和循环状态。</summary>
public sealed class Act03RoomLighting : MonoBehaviour
{
    [Header("灯光与光影层")]
    [SerializeField] private Light2D ambientLight;
    [SerializeField] private Light2D windowLight;
    [SerializeField] private Light2D doorwayFill;
    [SerializeField] private SpriteRenderer floorGlow;
    [SerializeField] private SpriteRenderer airGlow;

    [Header("冷月光氛围")]
    [SerializeField] private Color ambientColor = new Color(0.75f, 0.82f, 1f);
    [SerializeField, Min(0f)] private float ambientIntensity = 0.52f;
    [SerializeField, Min(0f)] private float windowIntensity = 0.43f;
    [Tooltip("门口少量补光，方便追逐时辨认出口；黑色门外仍保持黑色")]
    [SerializeField, Min(0f)] private float doorwayIntensity = 0.12f;

    private Color originalAmbientColor;
    private float originalAmbientIntensity;
    private bool hasAmbient;

    private void OnEnable()
    {
        hasAmbient = ambientLight != null;
        if (hasAmbient)
        {
            originalAmbientColor = ambientLight.color;
            originalAmbientIntensity = ambientLight.intensity;
        }
        if (windowLight != null) windowLight.enabled = true;
        if (doorwayFill != null) doorwayFill.enabled = true;
        if (floorGlow != null) floorGlow.enabled = true;
        if (airGlow != null) airGlow.enabled = true;
        ApplyLighting();
    }

    private void Update()
    {
        // 允许在 Play 中调节亮度，找出兼顾氛围和追逐可读性的数值。
        ApplyLighting();
    }

    private void ApplyLighting()
    {
        if (ambientLight != null)
        {
            ambientLight.color = ambientColor;
            ambientLight.intensity = ambientIntensity;
        }
        if (windowLight != null) windowLight.intensity = windowIntensity;
        if (doorwayFill != null) doorwayFill.intensity = doorwayIntensity;
    }

    private void OnDisable()
    {
        // 关闭整个试作对象，可以对比原本的房间灯光。
        if (hasAmbient && ambientLight != null)
        {
            ambientLight.color = originalAmbientColor;
            ambientLight.intensity = originalAmbientIntensity;
        }
        if (windowLight != null) windowLight.enabled = false;
        if (doorwayFill != null) doorwayFill.enabled = false;
        if (floorGlow != null) floorGlow.enabled = false;
        if (airGlow != null) airGlow.enabled = false;
        hasAmbient = false;
    }
}
