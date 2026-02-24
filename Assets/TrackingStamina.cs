using UnityEngine;
using UnityEngine.UI;

public class TrackingStamina : MonoBehaviour
{
    [Header("References")]
    public RayCursorToBoard2D cursorSource;
    public BouncingTarget2D targetSource;

    [Header("UI")]
    public Image cursorImage;

    [Tooltip("Use this if your stamina bar is an Image with Type=Filled.")]
    public Image staminaFillImage;

    [Tooltip("Use this if your stamina bar is a Slider.")]
    public Slider staminaSlider;

    [Header("Tracking contact rule")]
    public float contactRadiusPx = 30f;

    [Header("Stamina dynamics")]
    [Range(0f, 1f)]
    public float stamina = 1.0f;

    public float decayPerSecondWhenLost = 0.12f;
    public float recoverPerSecondWhenOn = 0.04f;

    [Header("Colors")]
    public Color onColor = Color.green;
    public Color offColor = Color.red;

    [Header("Debug")]
    public bool debugLive = false;

    public bool IsContact { get; private set; }

    void Start()
    {
        ApplyStaminaUI();
    }

    void Update()
    {
        if (cursorSource == null || targetSource == null) return;

        bool onBoard = cursorSource.OnBoard;
        Vector2 cursor = cursorSource.CursorPosPx;
        Vector2 target = targetSource.CurrentTargetPosPx();

        float dist = Vector2.Distance(cursor, target);
        IsContact = onBoard && (dist <= contactRadiusPx);

        if (cursorImage != null)
            cursorImage.color = IsContact ? onColor : offColor;

        float dt = Time.deltaTime;
        if (IsContact)
            stamina += recoverPerSecondWhenOn * dt;
        else
            stamina -= decayPerSecondWhenLost * dt;

        stamina = Mathf.Clamp01(stamina);

        ApplyStaminaUI();

        if (debugLive)
        {
            Debug.Log($"[Stamina] stamina={stamina:0.00}, onBoard={onBoard}, dist={dist:0.0}, contact={IsContact}");
        }
    }

    private void ApplyStaminaUI()
    {
        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = stamina;
        }

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value = stamina;
        }
    }
}
