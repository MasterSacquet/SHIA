using ACTA;
using Assets.Scripts;
using Assets.Scripts.Utils;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;
using Whisper;
using Whisper.Utils;
using Application = UnityEngine.Application;
using Button = UnityEngine.UI.Button;
using Debug = UnityEngine.Debug;
using Text = UnityEngine.UI.Text;
using System.Collections.Generic;

public enum EndPoint
{
        OpenWebUI,
        Ollama
};


/*
* La classe LLMDialogManager permet de centraliser les fonctionnalit�s li�s � l'aspect conversationnel de l'agent en Full Audio en utilisant un LLM h�berg� sur un serveur distant. 
* ATTENTION : pour faire fonctionner le plugin Whisper de Macoron, il faut ajouter les mod�les dans le r�pertoire 
* StreamingAssets. Allez voir les pages d�di�es de ces modules pour plus d'explications. Ils ne sont pas fournis par d�faut car ils prennent
* trop de place.
*/
public class AvaturnLLMDialogManager : MonoBehaviour
{

    public AudioSource audioSource;

    public float volume = 0.5f;

    public Transform informationPanel;
    public Transform textPanel;
    public Transform buttonPanel;
    public GameObject ButtonPrefab;
    private GameObject button;
    public FacialExpressionAvaturn faceExpression;
    private Animator anim;

    //dictation
    private DictationRecognizer dictationRecognizer;

    //whisper
    public bool useWhisper = true;
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;
    public bool streamSegments = true;
    public bool printLanguage = false;
    private string _buffer;

    //conversation memory
    public int numberOfTurn = 10;
    private JsonParser jsonParser = new JsonParser();
    private JsonValue conversationList = new JsonValue(JsonType.Array);

    //LLM

    public string urlOllama;
    public EndPoint endPoint = EndPoint.OpenWebUI; // api/chat/completions
    public string modelName;
    public string APIkey;
    [TextArea(15, 20)]
    public string preprompt;
    private string _response;

    //piper
    public bool usePiper = true;
    public int piperPort = 5000;
    public float speakerID = 1;

    public bool usePhonemeGenerator = false;

    //ComputationalModel
    private ComputationalModel computationalModel = new ComputationalModel();












    private string currentEmotion = "{NEUTRAL}";

    // Start is called before the first frame update
    void Start()
    {
        anim = this.gameObject.GetComponent<Animator>();
        InformationDisplay("");
        Text textp = textPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        textp.text = "";
        button = (GameObject)Instantiate(ButtonPrefab);
        button.GetComponentInChildren<Text>().text = "Dictation";

        button.GetComponent<Button>().onClick.AddListener(delegate { OnButtonPressed(); });

        button.GetComponent<RectTransform>().position = new Vector3(0 * 170.0f + 90.0f, 39.0f, 0.0f);
        button.transform.SetParent(buttonPanel);


        //dictation
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.AutoSilenceTimeoutSeconds = 10;
        dictationRecognizer.InitialSilenceTimeoutSeconds = 10;
        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;
        dictationRecognizer.DictationError += DictationRecognizer_DictationError;
        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;


        //whisper
        whisper.OnNewSegment += OnNewSegment;
        microphoneRecord.OnRecordStop += OnRecordStop;

    }

    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        button.GetComponentInChildren<Text>().text = "Dictation";
    }

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        useWhisper = true;
        button.GetComponentInChildren<Text>().text = "Record";

    }

    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        Text textp = textPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        textp.text = text;
        JsonValue userTurn = new JsonValue(JsonType.Object);
        JsonValue userRole = new JsonValue(JsonType.String);
        userRole.StringValue = "user";
        JsonValue userContent = new JsonValue(JsonType.String);
        userContent.StringValue = text;
        userTurn.ObjectValues.Add("role", userRole);
        userTurn.ObjectValues.Add("content", userContent);
        conversationList.ArrayValues.Add(userTurn);
        if (conversationList.ArrayValues.Count > numberOfTurn)
            conversationList.ArrayValues.RemoveAt(0);

        SendToChat(conversationList);
    }

    //whisper


    private void OnButtonPressed()
    {
        if (useWhisper)
        {
            if (!microphoneRecord.IsRecording)
            {
                microphoneRecord.StartRecord();
                button.GetComponentInChildren<Text>().text = "Stop";
            }
            else
            {
                microphoneRecord.StopRecord();
                button.GetComponentInChildren<Text>().text = "Record";
            }
        }
        else
        {
            if (dictationRecognizer.Status != SpeechSystemStatus.Running)
            {
                dictationRecognizer.Start();
                button.GetComponentInChildren<Text>().text = "Stop";
            }
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
                button.GetComponentInChildren<Text>().text = "Dictation";
            }
        }
    }

    private async void OnRecordStop(AudioChunk audioChunk)
    {
        _buffer = "";

        var res = await whisper.GetTextAsync(audioChunk.Data, audioChunk.Frequency, audioChunk.Channels);
        if (res == null)
            return;

        var text = res.Result;
        UserAnalysis(text);
        if (printLanguage)
            text += $"\n\nLanguage: {res.Language}";
        Text textp = textPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        textp.text = text;
        JsonValue userTurn = new JsonValue(JsonType.Object);
        JsonValue userRole = new JsonValue(JsonType.String);
        userRole.StringValue = "user";
        JsonValue userContent = new JsonValue(JsonType.String);
        userContent.StringValue = text;
        userTurn.ObjectValues.Add("role", userRole);
        userTurn.ObjectValues.Add("content", userContent);
        conversationList.ArrayValues.Add(userTurn);
        if (conversationList.ArrayValues.Count > numberOfTurn)
            conversationList.ArrayValues.RemoveAt(0);

        SendToChat(conversationList);
    }





    private void OnNewSegment(WhisperSegment segment)
    {
        if (!streamSegments)
            return;

        _buffer += segment.Text;
        Text textp = textPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        textp.text = _buffer + "...";
    }

    // Update is called once per frame
    void Update()
    {

    }


    /*
     * LLM
     */


    IEnumerator ChatRequest(string url, string json)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", "Bearer " + APIkey);

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            _response = uwr.downloadHandler.text;
            //retrieve response from the JSON
            JsonValue response = jsonParser.Parse(_response);
            String responseString = "";
            if (endPoint == EndPoint.OpenWebUI)
            {
                responseString = response.ObjectValues["choices"].ArrayValues[0].ObjectValues["message"].ObjectValues["content"].StringValue;
            }
            else if (endPoint == EndPoint.Ollama)
            {
                responseString = response.ObjectValues["message"].ObjectValues["content"].StringValue;
            }
            InformationDisplay(responseString);
            _response = ProcessAffectiveContent(responseString);
            //_response = responseString;
            LLMAnalysis(_response);

            JsonValue assistantTurn = new JsonValue(JsonType.Object);
            JsonValue assistantRole = new JsonValue(JsonType.String);
            assistantRole.StringValue = "assistant";
            JsonValue assistantContent = new JsonValue(JsonType.String);
            assistantContent.StringValue = _response;
            assistantTurn.ObjectValues.Add("role", assistantRole);
            assistantTurn.ObjectValues.Add("content", assistantContent);
            conversationList.ArrayValues.Add(assistantTurn);
            if (conversationList.ArrayValues.Count > numberOfTurn)
                conversationList.ArrayValues.RemoveAt(0);
            
            PlayAudio(_response);
        }
    }

    IEnumerator UserRequest(string url, string json)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", "Bearer " + APIkey);

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            _response = uwr.downloadHandler.text;
            //retrieve response from the JSON
            JsonValue response = jsonParser.Parse(_response);
            computationalModel.UserValues(response.StringValue);
        }
    }

    IEnumerator LLMRequest(string url, string json)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", "Bearer " + APIkey);

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            _response = uwr.downloadHandler.text;
            //retrieve response from the JSON
            JsonValue response = jsonParser.Parse(_response);
            computationalModel.LLMValues(response.StringValue);
        }
    }















    [Header("Emotion Settings")]

    [Range(0.5f, 2f)]
    public float speechSpeed = 2.5f;

    [Range(0.5f, 2f)]
    public float emotionOverlap = 0.85f;

    [Range(0.1f, 2f)]
    public float globalEmotionIntensity = 1.0f;

    [Range(0.1f, 1f)]
    public float attackRatio = 0.25f;

    [Range(0.1f, 1f)]
    public float decayExponent = 3f;

    class EmotionSegment
    {
        public string emotion;
        public string text;
        public float duration;
    }



    private string ProcessAffectiveContent(string response)
    {
        StopAllCoroutines(); // 🔥 évite les overlaps

        StartCoroutine(ProcessEmotionSequence(response));

        // Supprime les tags pour le TTS
        return Regex.Replace(response, "{.*?}", "").Trim();
    }

    List<EmotionSegment> ParseEmotionSegments(string text)
    {
        List<EmotionSegment> segments = new List<EmotionSegment>();

        Regex regex = new Regex(@"\{(.*?)\}");
        MatchCollection matches = regex.Matches(text);

        for (int i = 0; i < matches.Count; i++)
        {
            int start = matches[i].Index + matches[i].Length;
            int end = (i < matches.Count - 1) ? matches[i + 1].Index : text.Length;

            string emotion = matches[i].Groups[1].Value;
            string segmentText = text.Substring(start, end - start).Trim();

            segments.Add(new EmotionSegment
            {
                emotion = emotion,
                text = segmentText
            });
        }

        return segments;
    }

    IEnumerator ProcessEmotionSequence(string text)
    {
        var segments = ParseEmotionSegments(text);

        if (segments.Count == 0)
            yield break;

        float timeline = 0f;

        foreach (var seg in segments)
        {
            seg.duration = EstimateDuration(seg.text);
        }

        yield return new WaitForSeconds(0.05f);

        foreach (var seg in segments)
        {
            StartCoroutine(PlayEmotionEnvelope(seg.emotion, seg.duration));
            yield return new WaitForSeconds(seg.duration * emotionOverlap);
            // 🔥 chevauchement volontaire (clé du naturel)
        }
    }

    IEnumerator PlayEmotionEnvelope(string emotion, float duration)
    {
        var data = GetEmotionAUs(emotion);

        float attack = duration * attackRatio;
        float decay = duration - attack;

        float t = 0f;

        while (t < duration)
        {
            float intensityFactor;

            if (t < attack)
            {
                intensityFactor = Mathf.SmoothStep(0, 1, t / attack);
            }
            else
            {
                float d = (t - attack) / decay;
                intensityFactor = Mathf.Exp(-decayExponent * d);
            }

            ApplyEmotionDynamic(data, intensityFactor);

            t += Time.deltaTime;
            yield return null;
        }
    }

    void ApplyEmotionDynamic((int[], int[]) data, float factor)
    {
        int[] aus = data.Item1;
        int[] baseIntensities = data.Item2;

        int[] intensities = new int[baseIntensities.Length];

        for (int i = 0; i < baseIntensities.Length; i++)
        {
            float noise = UnityEngine.Random.Range(0.95f, 1.05f);

            intensities[i] = (int)(
                baseIntensities[i] *
                factor *
                noise *
                globalEmotionIntensity
            );
        }

        faceExpression.AccumulateExpression(aus, intensities);
    }

    float EstimateDuration(string text)
    {
        int words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return Mathf.Max(0.4f, words / speechSpeed);
    }

    (int[], int[]) GetEmotionAUs(string emotion)
    {
        switch (emotion)
        {
            case "JOY":
                return (new int[] { 6, 12, 25 }, new int[] { 80, 90, 30 });

            case "SAD":
                return (new int[] { 1, 4, 15, 17 }, new int[] { 60, 70, 70, 40 });

            case "ANGER":
                return (new int[] { 4, 7, 23, 24, 17 }, new int[] { 90, 70, 60, 80, 50 });

            case "SURPRISE":
                return (new int[] { 1, 2, 5, 26 }, new int[] { 80, 80, 90, 20 });

            case "FEAR":
                return (new int[] { 1, 2, 4, 5, 20, 26 }, new int[] { 70, 70, 60, 80, 50, 30 });

            case "DISGUST":
                return (new int[] { 9, 10, 17 }, new int[] { 80, 70, 50 });

            case "NEUTRAL":
            default:
                return (new int[] { }, new int[] { });
        }
    }


    void PlayEmotionSmooth(string emotion, float duration)
    {
        var data = GetEmotionAUs(emotion);

        // 🔥 petite variation naturelle
        int[] intensities = new int[data.Item2.Length];
        for (int i = 0; i < data.Item2.Length; i++)
        {
            int variation = UnityEngine.Random.Range(-10, 10);
            intensities[i] = Mathf.Clamp(data.Item2[i] + variation, 0, 100);
        }

        faceExpression.BlendToExpression(data.Item1, intensities, duration);
    }

    IEnumerator CrossFadeEmotion(string from, string to, float duration)
    {
        var fromData = GetEmotionAUs(from);
        var toData = GetEmotionAUs(to);

        float t = 0f;

        while (t < duration)
        {
            float lerp = t / duration;

            int length = Mathf.Max(fromData.Item2.Length, toData.Item2.Length);
            int[] blended = new int[length];

            for (int i = 0; i < length; i++)
            {
                int fromVal = i < fromData.Item2.Length ? fromData.Item2[i] : 0;
                int toVal = i < toData.Item2.Length ? toData.Item2[i] : 0;

                blended[i] = (int)Mathf.Lerp(fromVal, toVal, lerp);
            }

            faceExpression.BlendToExpression(toData.Item1, blended, 0.1f);

            t += Time.deltaTime;
            yield return null;
        }
    }
















    
    private void SendToChat(JsonValue conversationList)
    {
        if (conversationList.ArrayValues.Count == 0)
            return;
        JsonValue fullConv = new JsonValue(JsonType.Array);
        JsonValue systemTurn = new JsonValue(JsonType.Object);
        JsonValue systemRole = new JsonValue(JsonType.String);
        systemRole.StringValue = "system";
        JsonValue systemContent = new JsonValue(JsonType.String);
        systemContent.StringValue = Regex.Replace(Regex.Replace(preprompt, "[\"\']", ""), "\\s", " ");
        //systemContent.StringValue = "Tu t'appelles John et tu r�ponds avec un niveau de patience qui va de 1, tr�s patient, � 5, tr�s impatient. Le niveau de patience actuelle est �gale � :" +computationalModel.getEmotion();
        systemTurn.ObjectValues.Add("role", systemRole);
        systemTurn.ObjectValues.Add("content", systemContent);
        fullConv.ArrayValues.Add(systemTurn);
        fullConv.ArrayValues.AddRange(conversationList.ArrayValues);
        JsonValue data = new JsonValue(JsonType.Object);
        JsonValue modelNameValue = new JsonValue(JsonType.String);
        modelNameValue.StringValue = modelName;
        data.ObjectValues.Add("model", modelNameValue);
        data.ObjectValues.Add("messages", fullConv);
        JsonValue streamValue = new JsonValue(JsonType.Boolean);
        streamValue.BoolValue = false;
        data.ObjectValues.Add("stream", streamValue);
        string endPointS = "";
        if (endPoint == EndPoint.OpenWebUI)
        {
            endPointS = "api/chat/completions";
        }
        if (endPoint == EndPoint.Ollama)
        {
            endPointS = "api/chat";
        }
        StartCoroutine(ChatRequest(urlOllama + endPointS, data.ToJsonString()));
    }

    private void UserAnalysis(String content)
    {

        JsonValue fullConv = new JsonValue(JsonType.Array);
        JsonValue systemTurn = new JsonValue(JsonType.Object);
        JsonValue systemRole = new JsonValue(JsonType.String);
        systemRole.StringValue = "system";
        JsonValue systemContent = new JsonValue(JsonType.String);
        systemContent.StringValue = "Tu es un syst�me d'analyse des �motions. Quand je te parle tu r�ponds une valeur enti�re entre 0 et 100 d'intensit� �motionnelle que tu d�tectes dans ma phrase. Tu ne dis rien d'autre que la valeur. Tu ne dis pas un mot, juste la valeur num�rique, comme une machine.";
        systemTurn.ObjectValues.Add("role", systemRole);
        systemTurn.ObjectValues.Add("content", systemContent);
        fullConv.ArrayValues.Add(systemTurn);
        JsonValue userTurn = new JsonValue(JsonType.Object);
        JsonValue userRole = new JsonValue(JsonType.String);
        userRole.StringValue = "user";
        JsonValue userContent = new JsonValue(JsonType.String);
        userContent.StringValue = content;
        userTurn.ObjectValues.Add("role",userRole);
        userTurn.ObjectValues.Add("content",userContent);
        fullConv.ArrayValues.Add(userTurn);
        JsonValue data = new JsonValue(JsonType.Object);
        JsonValue modelNameValue = new JsonValue(JsonType.String);
        modelNameValue.StringValue = modelName;
        data.ObjectValues.Add("model", modelNameValue);
        data.ObjectValues.Add("messages", fullConv);
        JsonValue streamValue = new JsonValue(JsonType.Boolean);
        streamValue.BoolValue = false;
        data.ObjectValues.Add("stream", streamValue);
        string endPointS = "";
        if (endPoint == EndPoint.OpenWebUI)
        {
            endPointS = "api/chat/completions";
        }
        if (endPoint == EndPoint.Ollama)
        {
            endPointS = "api/chat";
        }
        StartCoroutine(UserRequest(urlOllama + endPointS, data.ToJsonString()));
    }

    private void LLMAnalysis(String content)
    {
        JsonValue fullConv = new JsonValue(JsonType.Array);
        JsonValue systemTurn = new JsonValue(JsonType.Object);
        JsonValue systemRole = new JsonValue(JsonType.String);
        systemRole.StringValue = "system";
        JsonValue systemContent = new JsonValue(JsonType.String);
        systemContent.StringValue = "Tu es un syst�me d'analyse des �motions. Quand je te parle tu r�ponds une valeur enti�re entre 0 et 100 d'intensit� �motionnelle que tu d�tectes dans ma phrase. Tu ne dis rien d'autre que la valeur. Tu ne dis pas un mot, juste la valeur num�rique, comme une machine.";
        systemTurn.ObjectValues.Add("role", systemRole);
        systemTurn.ObjectValues.Add("content", systemContent);
        fullConv.ArrayValues.Add(systemTurn);
        JsonValue userTurn = new JsonValue(JsonType.Object);
        JsonValue userRole = new JsonValue(JsonType.String);
        userRole.StringValue = "user";
        JsonValue userContent = new JsonValue(JsonType.String);
        userContent.StringValue = content;
        userTurn.ObjectValues.Add("role", userRole);
        userTurn.ObjectValues.Add("content", userContent);
        fullConv.ArrayValues.Add(userTurn);
        JsonValue data = new JsonValue(JsonType.Object);
        JsonValue modelNameValue = new JsonValue(JsonType.String);
        modelNameValue.StringValue = modelName;
        data.ObjectValues.Add("model", modelNameValue);
        data.ObjectValues.Add("messages", fullConv);
        JsonValue streamValue = new JsonValue(JsonType.Boolean);
        streamValue.BoolValue = false;
        data.ObjectValues.Add("stream", streamValue);
        string endPointS = "";
        if (endPoint == EndPoint.OpenWebUI)
        {
            endPointS = "api/chat/completions";
        }
        if (endPoint == EndPoint.Ollama)
        {
            endPointS = "api/chat";
        }
        StartCoroutine(LLMRequest(urlOllama + endPointS, data.ToJsonString()));
    }


    /*
     * Cette m�thode permet de jouer un fichier audio depuis le r�pertoire Resources/Sounds dont le nom est de la forme <entier>.mp3 
     */
    public void PlayAudio(int a)
    {
        try
        {
            //Charge un fichier audio depuis le r�pertoire Resources
            AudioClip music = (AudioClip)Resources.Load("Sounds/" + a);
            audioSource.PlayOneShot(music, volume);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
    }


    IEnumerator postTTSRequest(string text)
    {
        text = Regex.Replace(Regex.Replace(text, "[\"\']", ""), "\\s"," ");
        var uwr = new UnityWebRequest("http://localhost:"+ piperPort.ToString(), "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes("{ \"text\": \"" + text + "\" , \"speaker_id\": " + speakerID.ToString()+"}");
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();
        byte[] wavData = uwr.downloadHandler.data;
        if (usePhonemeGenerator)
        {
            string json = Wav2VecClient.SendWav(wavData);
            Debug.Log("Python returned: " + json);
        }
        
        AudioClip clip = WavUtility.ToAudioClip(wavData, "DownloadedClip");
        audioSource.clip = clip;
        audioSource.Play();
    }


    /*
     * Cette m�thode permet de demander � piperTTS de g�n�rer un audio, puis de le jouer, � partir du texte
     * piperTTS server doit donc �tre lanc� sur la machine.
     */
    public void PlayAudio(string text)
    {

        if (!usePiper)
        {
#if UNITY_STANDALONE_WIN
            Narrator.speak(text);
#else
            Debug.Log("Narrator not available");
#endif
        }
        else
        {
            StartCoroutine(postTTSRequest(text));
        }
    }



    /*
     * Cette m�thode affiche du texte dans le panneau d'affichage � gauche de l'UI
     */
    public void InformationDisplay(string s)
    {

        Text text = informationPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        text.text = s;

    }
    /*
     * Cette m�thode affiche le texte de la question dans la partie basse de l'UI
     */
    public void DisplayQuestion(string s)
    {
        Text text = textPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        text.text = s;
    }

    public void EndDialog()
    {

        anim.SetTrigger("Greet");
    }


    /*
     * Cette m�thode permet de faire jouer des AUs � l'agent
     */
    public void DisplayAUs(int[] aus, int[] intensities, float duration)
    {
        faceExpression.setFacialAUs(aus, intensities, duration);
    }

    /*
    * Exemple de fonction d�clenchant une expression �motionnelle
    * intensity_factor devrait �tre entre 0 et 1
    */
    public void Doubt(float intensity_factor, float duration)
    {
        DisplayAUs(new int[] { 6, 4, 14 }, new int[] { (int)(intensity_factor * 100), (int)(intensity_factor * 80), (int)(intensity_factor * 80) }, duration);
    }


}
