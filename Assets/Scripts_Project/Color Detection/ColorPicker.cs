using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ColorPicker : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider redSlider;
    [SerializeField] private Slider greenSlider;
    [SerializeField] private Slider blueSlider;
    [SerializeField] private Slider toleranceSlider;

    [SerializeField] private TMP_InputField redInput;
    [SerializeField] private TMP_InputField greenInput;
    [SerializeField] private TMP_InputField blueInput;
    [SerializeField] private TMP_InputField toleranceInput;

    [SerializeField] private Image colorPreview;
    [SerializeField] private Button confirmButton;

    [Header("Color Settings")]
    [SerializeField] private float defaultTolerance = 0.2f;

    private Color selectedColor;
    private float colorTolerance;

    public event Action<Color, float> OnColorSelected;

    void Start()
    {
        // Inicializar valores predeterminados
        colorTolerance = defaultTolerance;
        selectedColor = Color.red; // Color inicial

        // Configurar valores máximos de los sliders (0-255 para RGB)
        redSlider.maxValue = 255;
        greenSlider.maxValue = 255;
        blueSlider.maxValue = 255;
        toleranceSlider.maxValue = 0.75f;


        // Establecer valores iniciales
        redSlider.value = 255;
        greenSlider.value = 0;
        blueSlider.value = 0;
        toleranceSlider.value = defaultTolerance;

        UpdateInputFields();
        UpdateColorPreview();

        // Asignar listeners a los sliders
        redSlider.onValueChanged.AddListener(OnRedSliderChanged);
        greenSlider.onValueChanged.AddListener(OnGreenSliderChanged);
        blueSlider.onValueChanged.AddListener(OnBlueSliderChanged);
        toleranceSlider.onValueChanged.AddListener(OnToleranceSliderChanged);

        // Asignar listeners a los campos de entrada
        redInput.onEndEdit.AddListener(OnRedInputChanged);
        greenInput.onEndEdit.AddListener(OnGreenInputChanged);
        blueInput.onEndEdit.AddListener(OnBlueInputChanged);
        toleranceInput.onEndEdit.AddListener(OnToleranceInputChanged);

        // El botón de confirmación ya no es necesario, pero lo mantenemos por si acaso
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmColorSelection);
            // Opcional: ocultar el botón ya que no es necesario
            confirmButton.gameObject.SetActive(false);
        }

        // Notificar el color inicial
        NotifyColorChanged();
    }

    private void OnRedSliderChanged(float value)
    {
        selectedColor.r = value / 255f;
        redInput.text = Mathf.RoundToInt(value).ToString();
        UpdateColorPreview();
        NotifyColorChanged(); // Actualizar en tiempo real
    }

    private void OnGreenSliderChanged(float value)
    {
        selectedColor.g = value / 255f;
        greenInput.text = Mathf.RoundToInt(value).ToString();
        UpdateColorPreview();
        NotifyColorChanged(); // Actualizar en tiempo real
    }

    private void OnBlueSliderChanged(float value)
    {
        selectedColor.b = value / 255f;
        blueInput.text = Mathf.RoundToInt(value).ToString();
        UpdateColorPreview();
        NotifyColorChanged(); // Actualizar en tiempo real
    }

    private void OnToleranceSliderChanged(float value)
    {
        colorTolerance = value;

        // Mostrar descripción textual de la tolerancia
        string descripcion = "Muy estricta";
        if (value > 0.4f) descripcion = "Muy permisiva";
        else if (value > 0.3f) descripcion = "Permisiva";
        else if (value > 0.2f) descripcion = "Normal";
        else if (value > 0.1f) descripcion = "Estricta";

        // Si estás usando TMP_Text, puedes añadir esta descripción al texto
        toleranceInput.text = value.ToString("F2") + " - " + descripcion;

        NotifyColorChanged();
    }

    private void OnRedInputChanged(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 0, 255);
            redSlider.value = value;
            // El slider ya actualizará el color, el preview y notificará el cambio
        }
        else
        {
            redInput.text = Mathf.RoundToInt(redSlider.value).ToString();
        }
    }

    private void OnGreenInputChanged(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 0, 255);
            greenSlider.value = value;
            // El slider ya actualizará el color, el preview y notificará el cambio
        }
        else
        {
            greenInput.text = Mathf.RoundToInt(greenSlider.value).ToString();
        }
    }

    private void OnBlueInputChanged(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 0, 255);
            blueSlider.value = value;
            // El slider ya actualizará el color, el preview y notificará el cambio
        }
        else
        {
            blueInput.text = Mathf.RoundToInt(blueSlider.value).ToString();
        }
    }

    private void OnToleranceInputChanged(string input)
    {
        if (float.TryParse(input, out float value))
        {
            value = Mathf.Clamp(value, 0f, 1f);
            toleranceSlider.value = value;
            // El slider ya actualizará la tolerancia y notificará el cambio
        }
        else
        {
            toleranceInput.text = toleranceSlider.value.ToString("F2");
        }
    }

    private void UpdateInputFields()
    {
        redInput.text = Mathf.RoundToInt(redSlider.value).ToString();
        greenInput.text = Mathf.RoundToInt(greenSlider.value).ToString();
        blueInput.text = Mathf.RoundToInt(blueSlider.value).ToString();
        toleranceInput.text = toleranceSlider.value.ToString("F2");
    }

    private void UpdateColorPreview()
    {
        colorPreview.color = selectedColor;
    }

    private void NotifyColorChanged()
    {
        // Notificar a otros componentes sobre el color seleccionado
        OnColorSelected?.Invoke(selectedColor, colorTolerance);
        Debug.Log($"Color actualizado: R:{selectedColor.r * 255}, G:{selectedColor.g * 255}, B:{selectedColor.b * 255}, Tolerancia: {colorTolerance}");
    }

    private void ConfirmColorSelection()
    {
        // Mantener el método por compatibilidad, pero ahora solo llama a NotifyColorChanged
        NotifyColorChanged();
    }

    // Método público para obtener el color actual
    public Color GetSelectedColor()
    {
        return selectedColor;
    }

    // Método público para obtener la tolerancia actual
    public float GetColorTolerance()
    {
        return colorTolerance;
    }
}