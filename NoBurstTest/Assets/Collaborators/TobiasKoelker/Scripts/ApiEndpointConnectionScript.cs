using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class ApiEndpointConnectionScript : MonoBehaviour
{
    private string apiUrl = "https://api.sunoapi.org/api/v1/generate/upload-cover";
    private string apiKey = "fac0ec2c71ebf28feef676dd6d84fc15";
    // Privates Feld, um das heruntergeladene Audio zu speichern 
    private string savedAudioPath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SendSunoRequest());
    }

    private IEnumerator SendSunoRequest() 
    {
        string jsonBody = @"{ 
            ""uploadUrl"": ""https://github.com/DCP-INS/Groove/raw/refs/heads/main/Stimuli/-wav/Danno.wav"", 
            ""prompt"": ""Heavy Metal e-guitar music"",
            ""style"": ""Heavy Metal"",
            ""title"": ""Peaceful Piano Meditation"",
            ""customMode"": false, 
            ""instrumental"": true, 
            ""personaId"": ""persona_123"", 
            ""model"": ""V4"", 
            ""negativeTags"": ""Upbeat Drums"", 
            ""vocalGender"": ""m"", 
            ""styleWeight"": 0.65, 
            ""weirdnessConstraint"": 0.65, 
            ""audioWeight"": 0.65, 
            ""callBackUrl"": ""https://webhook.site/5a065819-8531-40cf-9595-49f86ea0252d"" }"; 
    
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody); 
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"); 
        request.uploadHandler = new UploadHandlerRaw(bodyRaw); 
        request.downloadHandler = new DownloadHandlerBuffer(); 
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Response: " + responseText);
            TaskResponse responseData = JsonUtility.FromJson<TaskResponse>(responseText);

            if (!string.IsNullOrEmpty(responseData.data.taskId))
            {
                string taskId = responseData.data.taskId;
                Debug.Log("Extracted taskId: " + taskId);

                // Polling starten
                //StartCoroutine(PollTaskStatus(taskId));
            }
        }
        else 
        { 
            Debug.LogError("Error: " + request.error + "\n" + request.downloadHandler.text); 
        }
    }

    private void PollTaskStatus(string taskId)
    {
        // Get Music Generation details:
        // https://api.sunoapi.org/api/v1/generate/record-info
        // in curl the request looks like this:
        // curl --request GET \
            // --url 'https://api.sunoapi.org/api/v1/generate/record-info?taskId=7d9e4d9ed34ba8096fcdd5782ff94dfe' \ // hier die task id
            // --header 'Authorization: Bearer fac0ec2c71ebf28feef676dd6d84fc15' // hier muss der token rein
        string statusUrl = $"https://api.sunoapi.org/api/v1/generate/status/{taskId}";

        // while (true)
        // {
        //     UnityWebRequest request = UnityWebRequest.Get(statusUrl);
        //     request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        //     yield return request.SendWebRequest();

            // this is how a Success response looks like:
            // {"code":200,"msg":"success","data":{"taskId":"7d9e4d9ed34ba8096fcdd5782ff94dfe","parentMusicId":"","param":"{\"audioWeight\":0.65,\"callBackUrl\":\"https://webhook.site/5a065819-8531-40cf-9595-49f86ea0252d\",\"customMode\":false,\"instrumental\":true,\"model\":\"V4\",\"negativeTags\":\"Upbeat Drums\",\"personaId\":\"persona_123\",\"prompt\":\"Heavy Metal e-guitar music\",\"style\":\"Classical\",\"styleWeight\":0.65,\"title\":\"Peaceful Piano Meditation\",\"uploadUrl\":\"https://github.com/DCP-INS/Groove/raw/refs/heads/main/Stimuli/-wav/Danno.wav\",\"vocalGender\":\"m\",\"weirdnessConstraint\":0.65}","response":{"taskId":"7d9e4d9ed34ba8096fcdd5782ff94dfe","sunoData":[{"id":"6471dc8b-0883-47bc-bc5e-11fa0c4316af","audioUrl":"https://musicfile.api.box/NjQ3MWRjOGItMDg4My00N2JjLWJjNWUtMTFmYTBjNDMxNmFm.mp3","sourceAudioUrl":"https://cdn1.suno.ai/6471dc8b-0883-47bc-bc5e-11fa0c4316af.mp3","streamAudioUrl":"https://musicfile.api.box/NjQ3MWRjOGItMDg4My00N2JjLWJjNWUtMTFmYTBjNDMxNmFm","sourceStreamAudioUrl":"https://cdn1.suno.ai/6471dc8b-0883-47bc-bc5e-11fa0c4316af.mp3","imageUrl":"https://musicfile.api.box/NjQ3MWRjOGItMDg4My00N2JjLWJjNWUtMTFmYTBjNDMxNmFm.jpeg","sourceImageUrl":"https://cdn2.suno.ai/image_6471dc8b-0883-47bc-bc5e-11fa0c4316af.jpeg","prompt":"[Instrumental]","modelName":"chirp-v4","title":"Infernal Reign","tags":"metal, distorted e-guitars, aggressive, growling male vocals, heavy metal, thunderous drums","createTime":1764690794449,"duration":47.76},{"id":"11945c94-bdf0-4c83-8672-0a24ad309a11","audioUrl":"https://musicfile.api.box/MTE5NDVjOTQtYmRmMC00YzgzLTg2NzItMGEyNGFkMzA5YTEx.mp3","sourceAudioUrl":"https://cdn1.suno.ai/11945c94-bdf0-4c83-8672-0a24ad309a11.mp3","streamAudioUrl":"https://musicfile.api.box/MTE5NDVjOTQtYmRmMC00YzgzLTg2NzItMGEyNGFkMzA5YTEx","sourceStreamAudioUrl":"https://cdn1.suno.ai/11945c94-bdf0-4c83-8672-0a24ad309a11.mp3","imageUrl":"https://musicfile.api.box/MTE5NDVjOTQtYmRmMC00YzgzLTg2NzItMGEyNGFkMzA5YTEx.jpeg","sourceImageUrl":"https://cdn2.suno.ai/image_11945c94-bdf0-4c83-8672-0a24ad309a11.jpeg","prompt":"[Instrumental]","modelName":"chirp-v4","title":"Infernal Reign","tags":"metal, distorted e-guitars, aggressive, growling male vocals, heavy metal, thunderous drums","createTime":1764690794449,"duration":217.4}]},"status":"SUCCESS","type":"chirp-v4","operationType":"upload_cover","errorCode":null,"errorMessage":null,"createTime":1764690657000}}

            //if (request.result == UnityWebRequest.Result.Success)
            //{
                // Status aus JSON parsen
                //var json = request.downloadHandler.text;


                // if (statusData.status == "completed") // or status == "first")
                // {
                // }
            //     else
            //     {
            //         //Debug.Log("Task noch nicht fertig, Status: " + statusData.status);
            //     }
            // }
            // else
            // {
            //     Debug.LogError("Fehler beim Polling: " + request.error);
            // }

            // // 5 Sekunden warten, bevor erneut gefragt wird
            // yield return new WaitForSeconds(5f);
        //}
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    // Hilfsklassen für JSON Parsing
[System.Serializable]
public class TaskResponse
{
    public int code;
    public string msg;
    public TaskData data;
}

[System.Serializable]
public class TaskData
{
    public string taskId;
}
}
