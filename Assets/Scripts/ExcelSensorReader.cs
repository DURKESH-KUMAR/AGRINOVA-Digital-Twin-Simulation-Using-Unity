using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExcelSensorReader : MonoBehaviour
{
    [Header("=== CSV LINKS PER SENSOR ===")]
    public string sensorData1Url;
    public string sensorData2Url;

    [Header("=== SENSOR SELECTION UI ===")]
    public Dropdown sensorDropdown;

    [Header("=== FETCH SETTINGS ===")]
    public float fetchInterval = 2f;

    [Header("=== UI TEXT ELEMENTS ===")]
    public Text temperatureText;
    public Text humidityText;
    public Text gasText;
    public Text containerWeightText;
    public Text fanSpeedText;
    public Text connectionStatusText;

    [Header("=== UI SLIDERS (OPTIONAL) ===")]
    public Slider temperatureSlider;
    public Slider humiditySlider;
    
    public Slider gasSlider;

    [Header("=== FAN SETTINGS ===")]
    public GameObject fanBlade;
    public float maxFanSpeed = 800f;
    public float fanSmoothTime = 0.2f;

    [Header("=== SENSOR RANGES ===")]
    public float maxTemperature = 50f;
    public float maxGasValue = 1023f;

    // Sensor values
    private float temperature;
    private float humidity;
    private float gasValue;
    private float containerWeight;

    // Fan control
    private float currentFanSpeed;
    private float targetFanSpeed;
    private float fanVelocity;

    private string activeCsvUrl;
    private Coroutine fetchCoroutine;

    void Start()
    {
        if (sensorDropdown != null)
        {
            sensorDropdown.onValueChanged.AddListener(OnSensorSelectionChanged);
        }

        UpdateConnectionStatus("Connecting to Excel...", true);
        SetActiveCsvUrl();
        StartFetching();
    }

    void OnSensorSelectionChanged(int index)
    {
        SetActiveCsvUrl();
        StartFetching(); // restart immediately
    }

    void SetActiveCsvUrl()
    {
        switch (sensorDropdown.value)
        {
            case 0:
                activeCsvUrl = sensorData1Url;
                break;
            case 1:
                activeCsvUrl = sensorData2Url;
                break;
        }
    }

    void StartFetching()
    {
        if (fetchCoroutine != null)
            StopCoroutine(fetchCoroutine);

        fetchCoroutine = StartCoroutine(FetchExcelLoop());
    }

    IEnumerator FetchExcelLoop()
    {
        while (true)
        {
            yield return FetchExcelData();
            yield return new WaitForSeconds(fetchInterval);
        }
    }

    IEnumerator FetchExcelData()
    {
        if (string.IsNullOrEmpty(activeCsvUrl))
            yield break;

        UnityWebRequest request = UnityWebRequest.Get(activeCsvUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("CSV Fetch Failed: " + request.error);
            UpdateConnectionStatus("Excel Fetch Failed", false);
        }
        else
        {
            ParseCSV(request.downloadHandler.text);
            UpdateConnectionStatus("Excel Connected", true);
        }
    }

    void ParseCSV(string csvData)
    {
        string[] rows = csvData.Split('\n');

        if (rows.Length < 2) return;

        string lastRow = rows[rows.Length - 2].Trim();
        string[] values = lastRow.Split(',');

        if (values.Length < 5) return;

        float.TryParse(values[1], out temperature);
        float.TryParse(values[2], out humidity);
        float.TryParse(values[3], out gasValue);
        float.TryParse(values[4], out containerWeight);

        temperature = Mathf.Clamp(temperature, 0f, maxTemperature);
        humidity = Mathf.Clamp(humidity, 0f, 100f);
        gasValue = Mathf.Clamp(gasValue, 0f, maxGasValue);
        containerWeight = Mathf.Max(0f, containerWeight);

        UpdateUI();
    }

    void Update()
    {
        UpdateFanSpeed();
        RotateFan();
    }

    void UpdateUI()
    {
        if (temperatureText)
            temperatureText.text = $"{temperature:F1} °C";

        if (humidityText)
            humidityText.text = $"{humidity:F1} %";

        if (gasText)
            gasText.text = $"{gasValue:F0}";

        if (containerWeightText)
            containerWeightText.text = $"{containerWeight:F2} kg";

        if (fanSpeedText)
            fanSpeedText.text = $"{currentFanSpeed:F0} RPM";

        if (temperatureSlider)
            temperatureSlider.value = temperature / maxTemperature;

        if (humiditySlider)
            humiditySlider.value = humidity / 100f;

        if (gasSlider)
            gasSlider.value = gasValue / maxGasValue;
    }

    void UpdateFanSpeed()
    {
        float t = temperature / maxTemperature;
        float h = humidity / 100f;
        float g = gasValue / maxGasValue;

        float avg = (t + h + g) / 3f;
        targetFanSpeed = avg * maxFanSpeed;

        currentFanSpeed = Mathf.SmoothDamp(
            currentFanSpeed,
            targetFanSpeed,
            ref fanVelocity,
            fanSmoothTime
        );
    }

    void RotateFan()
    {
        if (!fanBlade) return;
        fanBlade.transform.Rotate(0f, currentFanSpeed * 6f * Time.deltaTime, 0f);
    }

    void UpdateConnectionStatus(string message, bool connected)
    {
        if (!connectionStatusText) return;

        connectionStatusText.text = message;
        connectionStatusText.color = connected ? Color.green : Color.red;
    }
}
