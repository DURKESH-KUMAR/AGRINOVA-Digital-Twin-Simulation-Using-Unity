using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AgriNovaSensorSystem : MonoBehaviour
{
    [Header("MASTER DATABASE CSV")]
    public string mobileDatabaseUrl;

    [Header("USER INPUT")]
    public InputField mobileInput;
    public Button analyseButton;

    [Header("USER INFO")]
    public Text nameText;

    [Header("SENSOR SELECTOR")]
    public Dropdown sensorDropdown;

    [Header("CAMERAS")]
    public Camera camera1;
    public Camera camera2;

    [Header("CANVASES")]
    public GameObject canvas1;
    public GameObject canvas2;

    [Header("FETCH SETTINGS")]
    public float fetchInterval = 2f;

    [Header("UI TEXT")]
    public Text temperatureText;
    public Text humidityText;
    public Text gasText;
    public Text fanSpeedText;
    public Text statusText;

    [Header("UI SLIDERS")]
    public Slider temperatureSlider;
    public Slider humiditySlider;
    public Slider gasSlider;

    [Header("FAN")]
    public GameObject fanBlade;
    public float maxFanSpeed = 800f;
    public float fanSmoothTime = 0.25f;

    [Header("LIMITS")]
    public float maxTemperature = 50f;
    public float maxGasValue = 1023f;

    private const string MOBILE_PREF_KEY = "USER_MOBILE";
    private const string NAME_PREF_KEY = "USER_NAME";

    float temperature;
    float humidity;
    float gasValue;

    float currentFanSpeed;
    float targetFanSpeed;
    float fanVelocity;

    string sensorSheetURL;
    string userName;

    Coroutine fetchRoutine;

    void Start()
    {
        analyseButton.onClick.AddListener(OnAnalyseClicked);

        camera2.enabled = false;
        canvas2.SetActive(false);

        if (PlayerPrefs.HasKey(MOBILE_PREF_KEY))
            mobileInput.text = PlayerPrefs.GetString(MOBILE_PREF_KEY);

        if (PlayerPrefs.HasKey(NAME_PREF_KEY))
            nameText.text = PlayerPrefs.GetString(NAME_PREF_KEY);

        // 🔽 Set dropdown labels (IMPORTANT)
        sensorDropdown.ClearOptions();
        sensorDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "📊 Summary",
            "Sensor Data 1",
            "Sensor Data 2",
            "Sensor Data 3"
        });
    }

    void OnAnalyseClicked()
    {
        string mobile = mobileInput.text.Trim();

        if (string.IsNullOrEmpty(mobile))
        {
            UpdateStatus("Enter Mobile Number", false);
            return;
        }

        StartCoroutine(CheckMobileDatabase(mobile));
    }

    IEnumerator CheckMobileDatabase(string mobile)
    {
        UpdateStatus("Checking Mobile...", true);

        UnityWebRequest request = UnityWebRequest.Get(mobileDatabaseUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UpdateStatus("Database Connection Failed", false);
            yield break;
        }

        ParseMobileDatabase(request.downloadHandler.text, mobile);
    }

    void ParseMobileDatabase(string csv, string mobile)
    {
        string[] rows = csv.Split('\n');

        for (int i = 1; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i]))
                continue;

            string[] cols = rows[i].Split(',');

            if (cols.Length < 3)
                continue;

            if (cols[0].Trim() == mobile)
            {
                sensorSheetURL = cols[1].Trim();
                userName = cols[2].Trim();

                PlayerPrefs.SetString(MOBILE_PREF_KEY, mobile);
                PlayerPrefs.SetString(NAME_PREF_KEY, userName);
                PlayerPrefs.Save();

                nameText.text = userName;

                UpdateStatus("Access Granted", true);

                ActivateDashboard();
                StartSensorFetch();
                return;
            }
        }

        UpdateStatus("Mobile Not Registered", false);
    }

    void ActivateDashboard()
    {
        camera1.enabled = false;
        canvas1.SetActive(false);

        camera2.enabled = true;
        canvas2.SetActive(true);
    }

    void StartSensorFetch()
    {
        if (fetchRoutine != null)
            StopCoroutine(fetchRoutine);

        fetchRoutine = StartCoroutine(FetchSensorLoop());
    }

    IEnumerator FetchSensorLoop()
    {
        while (true)
        {
            yield return FetchSensorData();
            yield return new WaitForSeconds(fetchInterval);
        }
    }

    IEnumerator FetchSensorData()
    {
        UnityWebRequest request = UnityWebRequest.Get(sensorSheetURL);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UpdateStatus("Sensor Fetch Failed", false);
        }
        else
        {
            ParseSensorCSV(request.downloadHandler.text);
            UpdateStatus("Sensor Connected", true);
        }
    }

    // 🔥 UPDATED CORE LOGIC
    void ParseSensorCSV(string csv)
    {
        string[] rows = csv.Split('\n');

        if (rows.Length < 2)
            return;

        string lastRow = rows[rows.Length - 2];
        string[] v = lastRow.Split(',');

        if (v.Length < 10)
            return;

        float t1 = SafeParse(v[1]);
        float t2 = SafeParse(v[2]);
        float t3 = SafeParse(v[3]);

        float h1 = SafeParse(v[4]);
        float h2 = SafeParse(v[5]);
        float h3 = SafeParse(v[6]);

        float g1 = SafeParse(v[7]);
        float g2 = SafeParse(v[8]);
        float g3 = SafeParse(v[9]);

        switch (sensorDropdown.value)
        {
            case 0: // 📊 SUMMARY
                temperature = (t1 + t2 + t3) / 3f;
                humidity = (h1 + h2 + h3) / 3f;
                gasValue = (g1 + g2 + g3) / 3f;
                break;

            case 1:
                temperature = t1;
                humidity = h1;
                gasValue = g1;
                break;

            case 2:
                temperature = t2;
                humidity = h2;
                gasValue = g2;
                break;

            case 3:
                temperature = t3;
                humidity = h3;
                gasValue = g3;
                break;
        }

        temperature = Mathf.Clamp(temperature, 0, maxTemperature);
        humidity = Mathf.Clamp(humidity, 0, 100);
        gasValue = Mathf.Clamp(gasValue, 0, maxGasValue);

        UpdateUI();
    }

    float SafeParse(string val)
    {
        float result;
        float.TryParse(val, out result);
        return result;
    }

    void Update()
    {
        UpdateFanSpeed();
        RotateFan();
    }

    void UpdateUI()
    {
        temperatureText.text = temperature.ToString("F1") + " °C";
        humidityText.text = humidity.ToString("F1") + " %";
        gasText.text = gasValue.ToString("F0");

        temperatureSlider.value = temperature / maxTemperature;
        humiditySlider.value = humidity / 100f;
        gasSlider.value = gasValue / maxGasValue;

        fanSpeedText.text = currentFanSpeed.ToString("F0") + " RPM";
    }

    void UpdateFanSpeed()
    {
        float avg = (temperature / maxTemperature + humidity / 100f + gasValue / maxGasValue) / 3f;

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
        if (fanBlade == null) return;

        fanBlade.transform.Rotate(
            0f,
            currentFanSpeed * 6f * Time.deltaTime,
            0f
        );
    }

    void UpdateStatus(string msg, bool success)
    {
        statusText.text = msg;
        statusText.color = success ? Color.green : Color.red;
    }

    public void ClearSavedMobile()
    {
        PlayerPrefs.DeleteKey(MOBILE_PREF_KEY);
        PlayerPrefs.DeleteKey(NAME_PREF_KEY);
        PlayerPrefs.Save();

        nameText.text = "";
        UpdateStatus("User Cleared", false);
    }

    public float GetTemperature() => temperature;
    public float GetHumidity() => humidity;
    public float GetGas() => gasValue;
}