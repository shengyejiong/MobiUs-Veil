using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>第一幕窗光试作：只调整视觉，读取现有的关窗状态。</summary>
public sealed class Act01WindowLighting : MonoBehaviour
{
    [Header("灯光与光影层")]
    [SerializeField] private Light2D ambientLight;
    [SerializeField] private Light2D windowLight;
    [SerializeField] private SpriteRenderer floorGlow;
    [SerializeField] private SpriteRenderer airGlow;

    [Header("夕阳氛围")]
    [SerializeField] private Color ambientColor = new Color(1f, 0.96f, 0.88f);
    [SerializeField, Min(0f)] private float openAmbientIntensity = 0.62f;
    [SerializeField, Min(0f)] private float closedAmbientIntensity = 0.54f;
    [SerializeField, Min(0f)] private float windowIntensity = 0.55f;
    [Tooltip("关窗后保留少量光缝；0 表示完全消失")]
    [SerializeField, Range(0f, 1f)] private float closedWindowFraction = 0.06f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.8f;

    private Color originalAmbientColor;
    private float originalAmbientIntensity;
    private Color floorColor;
    private Color airColor;
    private float curtainBlend;
    private bool hasAmbient;

    private void OnEnable()
    {
        hasAmbient = ambientLight != null;
        if (hasAmbient)
        {
            originalAmbientColor = ambientLight.color;
            originalAmbientIntensity = ambientLight.intensity;
        }
        if (floorGlow != null)
        {
            floorColor = floorGlow.color;
            floorGlow.enabled = true;
        }
        if (airGlow != null)
        {
            airColor = airGlow.color;
            airGlow.enabled = true;
        }
        if (windowLight != null) windowLight.enabled = true;
        curtainBlend = Act01Story.WindowClosed ? 1f : 0f;
        ApplyLighting();
    }

    private void Update()
    {
        // 使用游戏时间；Esc 暂停时，关窗的灯光渐变也会暂停。
        float target = Act01Story.WindowClosed ? 1f : 0f;
        curtainBlend = Mathf.MoveTowards(curtainBlend, target, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
        ApplyLighting();
    }

    private void ApplyLighting()
    {
        float easedBlend = Mathf.SmoothStep(0f, 1f, curtainBlend);
        float windowFraction = Mathf.Lerp(1f, closedWindowFraction, easedBlend);
        if (ambientLight != null)
        {
            ambientLight.color = ambientColor;
            ambientLight.intensity = Mathf.Lerp(openAmbientIntensity, closedAmbientIntensity, easedBlend);
        }
        if (windowLight != null) windowLight.intensity = windowIntensity * windowFraction;
        if (floorGlow != null)
            floorGlow.color = new Color(floorColor.r, floorColor.g, floorColor.b, floorColor.a * windowFraction);
        if (airGlow != null)
            airGlow.color = new Color(airColor.r, airColor.g, airColor.b, airColor.a * windowFraction);
    }

    private void OnDisable()
    {
        // Play 中关闭整个试作对象，即可对比原房间的灯光。
        if (hasAmbient && ambientLight != null)
        {
            ambientLight.color = originalAmbientColor;
            ambientLight.intensity = originalAmbientIntensity;
        }
        if (windowLight != null) windowLight.enabled = false;
        if (floorGlow != null)
        {
            floorGlow.color = floorColor;
            floorGlow.enabled = false;
        }
        if (airGlow != null)
        {
            airGlow.color = airColor;
            airGlow.enabled = false;
        }
        hasAmbient = false;
    }
}
