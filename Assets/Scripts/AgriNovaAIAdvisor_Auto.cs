using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class AgriNovaAIAdvisor_Auto : MonoBehaviour
{
    [Header("REFERENCE")]
    public AgriNovaSensorSystem sensorSystem;

    [Header("AI SETTINGS")]
    [TextArea] public string apiKey = "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";
    public string apiURL = "https://api.groq.com/openai/v1/chat/completions";
    public string modelName = "openai/gpt-oss-20b";

    [Header("UI")]
    public Text statusText;
    public Text predictionText;

    [Header("AUTO SETTINGS")]
    public float checkInterval = 10f;
    public float changeThreshold = 2f;

    float lastTemp = -999f;
    float lastHum = -999f;
    float lastGas = -999f;

    string lastStatus = "";

    AgriNotificationManager notificationManager;

    void Start()
    {
        notificationManager = GetComponent<AgriNotificationManager>();
        StartCoroutine(AutoAIWatcher());
    }

    IEnumerator AutoAIWatcher()
    {
        while (true)
        {
            float temp = sensorSystem.GetTemperature();
            float hum = sensorSystem.GetHumidity();
            float gas = sensorSystem.GetGas();

            if (Mathf.Abs(temp - lastTemp) > changeThreshold ||
                Mathf.Abs(hum - lastHum) > changeThreshold ||
                Mathf.Abs(gas - lastGas) > changeThreshold)
            {
                lastTemp = temp;
                lastHum = hum;
                lastGas = gas;

                // ✅ CORE LOGIC (NO AI)
                string status = GetStatus(temp, hum, gas);
                int days = GetSproutDays(temp, hum, gas);

                lastStatus = status;

                // ✅ Instant UI update
                UpdateStatusUI(status);
                predictionText.text = "🌱 Sprout in: " + days + " days\n\n⏳ Getting advice...";

                // 🤖 AI only for advice
                string prompt = GeneratePrompt(temp, hum, gas, status);
                yield return StartCoroutine(CallAI(prompt));
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    // ✅ STATUS LOGIC (100% deterministic)
    string GetStatus(float t, float h, float g)
    {
        bool tempBad = (t < 25f || t > 30f);
        bool humBad = (h < 65f || h > 75f);

        if (g > 250f)
            return "WARNING";

        if (g >= 200f || tempBad || humBad)
            return "CAUTION";

        return "OK";
    }

    // ✅ SPROUT DAYS LOGIC
    int GetSproutDays(float t, float h, float g)
    {
        if (g > 250f) return 1;
        if (g >= 200f) return 3;
        if (t < 25f || t > 30f || h < 65f || h > 75f) return 5;

        return 10;
    }

    // 🤖 AI PROMPT (MESSAGE ONLY)
    string GeneratePrompt(float t, float h, float g, string status)
    {
        return "You are an agriculture assistant.\n\n" +

               "Sensor Data:\n" +
               "Temperature: " + t + " C\n" +
               "Humidity: " + h + " %\n" +
               "Gas Level: " + g + "\n\n" +

               "Current STATUS: " + status + "\n\n" +

               "Give only one short practical advice to improve onion storage.\n" +
               "Do NOT explain.\n" +
               "One line only.";
    }

    IEnumerator CallAI(string prompt)
    {
        string safePrompt = EscapeJson(prompt);

        string jsonBody = "{ " +
            "\"model\": \"" + modelName + "\", " +
            "\"messages\": [" +
            "{ \"role\": \"user\", \"content\": \"" + safePrompt + "\" }" +
            "] }";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(apiURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            predictionText.text += "\n\n❌ AI Error: " + request.error;
        }
        else
        {
            string raw = request.downloadHandler.text;
            string aiText = ExtractText(raw);

            ApplyAIMessage(aiText);
        }
    }

    // ✅ SIMPLE TEXT EXTRACTION
    string ExtractText(string json)
    {
        string marker = "\"content\":\"";
        int startIndex = json.IndexOf(marker);

        if (startIndex >= 0)
        {
            startIndex += marker.Length;
            int endIndex = json.IndexOf("\"", startIndex);

            if (endIndex > startIndex)
            {
                return json.Substring(startIndex, endIndex - startIndex)
                           .Replace("\\n", "\n")
                           .Replace("\\\"", "\"");
            }
        }

        return "Keep conditions stable and monitor regularly.";
    }

    // ✅ APPLY AI MESSAGE
    void ApplyAIMessage(string message)
    {
        predictionText.text += "\n\n💡 " + message;

        if (notificationManager != null)
        {
            notificationManager.SendNotification(lastStatus, message);
        }
    }

    // 🎨 UI UPDATE
    void UpdateStatusUI(string status)
    {
        switch (status)
        {
            case "OK":
                statusText.text = "✅ OK";
                statusText.color = Color.green;
                break;

            case "CAUTION":
                statusText.text = "⚠ CAUTION";
                statusText.color = Color.yellow;
                break;

            case "WARNING":
                statusText.text = "❌ WARNING";
                statusText.color = Color.red;
                break;

            default:
                statusText.text = "❓ UNKNOWN";
                statusText.color = Color.white;
                break;
        }
    }

    // 🔐 JSON SAFE
    string EscapeJson(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r");
    }
}