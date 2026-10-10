using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>第二幕黄昏窗光试作；只控制本场景的环境光和局部光源。</summary>
public sealed class Act02RoomLighting : MonoBehaviour
{
    [Header("灯光与光影层")]
    [SerializeField] private Light2D ambientLight;
    [SerializeField] private Light2D windowLight;
    [SerializeField] private Light2D phoneLight;
    [SerializeField] private SpriteRenderer floorGlow;
    [SerializeField] private SpriteRenderer airGlow;
    [SerializeField] private SpriteRenderer phoneRenderer;

    [Header("黄昏氛围")]
    [SerializeField] private Color ambientColor = new Color(1f, 0.88f, 0.78f);
    [SerializeField, Min(0f)] private float ambientIntensity = 0.55f;
    [SerializeField, Min(0f)] private float windowIntensity = 0.38f;
    [SerializeField, Min(0f)] private float phoneIntensity = 0.1f;

    private Color originalAmbientColor;
    private float originalAmbientIntensity;
    private Color originalPhoneColor;
    private bool hasAmbient;
    private bool hasPhone;

    private void OnEnable()
    {
        hasAmbient = ambientLight != null;
        if (hasAmbient)
        {
            originalAmbientColor = ambientLight.color;
            originalAmbientIntensity = ambientLight.intensity;
        }
        hasPhone = phoneRenderer != null;
        if (hasPhone)
        {
            originalPhoneColor = phoneRenderer.color;
            // 去掉原来的黄色占位染色，让手机能正常响应冷色光。
            phoneRenderer.color = new Color(1f, 1f, 1f, originalPhoneColor.a);
        }
        if (windowLight != null) windowLight.enabled = true;
        if (phoneLight != null) phoneLight.enabled = true;
        if (floorGlow != null) floorGlow.enabled = true;
        if (airGlow != null) airGlow.enabled = true;
        ApplyLighting();
    }

    private void Update()
    {
        // 保持 Inspector 参数可在 Play 中即时调节；不改变剧情状态。
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
        if (phoneLight != null) phoneLight.intensity = phoneIntensity;
    }

    private void OnDisable()
    {
        // Play 中关闭试作对象，可恢复原有环境灯光和手机染色作比较。
        if (hasAmbient && ambientLight != null)
        {
            ambientLight.color = originalAmbientColor;
            ambientLight.intensity = originalAmbientIntensity;
        }
        if (hasPhone && phoneRenderer != null) phoneRenderer.color = originalPhoneColor;
        if (windowLight != null) windowLight.enabled = false;
        if (phoneLight != null) phoneLight.enabled = false;
        if (floorGlow != null) floorGlow.enabled = false;
        if (airGlow != null) airGlow.enabled = false;
        hasAmbient = false;
        hasPhone = false;
    }
}
