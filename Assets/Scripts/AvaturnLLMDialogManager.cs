using ACTA;
using Assets.Scripts;
using Assets.Scripts.Utils;
using Assets.Scripts.Emotions;
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
using Emotion = Assets.Scripts.Emotions.EmotionalState.Emotion;

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

/// <summary>
/// StoryPart: Structure pour gérer les parties narrative de l'histoire Lucas
/// </summary>
public class StoryPart
{
    public int id;
    public string title;
    public string narrative;
    public string userQuestion;

    public StoryPart(int id, string title, string narrative, string userQuestion)
    {
        this.id = id;
        this.title = title;
        this.narrative = narrative;
        this.userQuestion = userQuestion;
    }
}

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
    public int numberOfTurn = 50; // Augmenté pour inclure toute l'histoire Lucas (9 parties)
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

    // ===== HISTOIRE LUCAS =====
    private List<StoryPart> lucasStory = new List<StoryPart>();
    private int currentStoryPart = 0;
    private bool isStoryMode = false;
    private string lastUserResponse = "";
    private Emotion detectedUserEmotion = Emotion.Surprise;
    private ContextualEmotionMapper emotionMapper = new ContextualEmotionMapper();
    




















    /// <summary>
    /// Mode interactif de l'histoire (0 ou 1)
    /// 0: Parties 0-3 non-interactives (automatiques), Parties 4-7 interactives (questions)
    /// 1: Parties 0-3 interactives (questions), Parties 4-7 non-interactives (automatiques)
    /// </summary>
    public int storyInteractionMode = 0;

















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

        // Initialiser l'histoire Lucas
        CreateLucasStory();
        StartStory();

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

        // Si on est en story mode et que la partie actuelle est INTERACTIVE
        // Stocker la réponse et passer à la partie suivante
        // Sinon, envoyer normalement au chat (ou ignorer si partie non-interactive)
        if (isStoryMode)
        {
            if (IsCurrentPartInteractive())
            {
                lastUserResponse = text;
                NextStoryPart();
            }
            else
            {
                // Partie non-interactive: ignorer la réponse de l'utilisateur
                Debug.Log("ℹ️ Partie non-interactive: réponse utilisateur ignorée");
            }
        }
        else
        {
            SendToChat(conversationList);
        }
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
        // Ignorer complètement si l'histoire est terminée
        if (!isStoryMode)
        {
            Debug.Log("🔇 OnRecordStop() ignoré: histoire terminée ou pas en mode story");
            return;
        }

        _buffer = "";

        var res = await whisper.GetTextAsync(audioChunk.Data, audioChunk.Frequency, audioChunk.Channels);
        if (res == null)
            return;

        var text = res.Result;
        Debug.Log($"📝 RÉPONSE UTILISATEUR: '{text}'");
        
        // En mode histoire, ne pas faire d'analyse générique
        if (!isStoryMode)
        {
            UserAnalysis(text);
        }
        
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

        // Si on est en story mode et que la partie actuelle est INTERACTIVE
        // Analyser l'émotion CONTEXTUELLE et passer à la partie suivante
        // Sinon, envoyer normalement au chat (ou ignorer si partie non-interactive)
        if (isStoryMode)
        {
            if (IsCurrentPartInteractive())
            {
                lastUserResponse = text;
                NextStoryPart();  // ✅ Analyse contextuelle basée sur la question et la partie
            }
            else
            {
                // Partie non-interactive: ignorer la réponse de l'utilisateur
                Debug.Log("ℹ️ Partie non-interactive: réponse utilisateur ignorée");
            }
        }
        else
        {
            SendToChat(conversationList);
        }
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
            
            // Debug: Afficher l'état du mode et de l'interactivité
            bool shouldAutoAdvance = isStoryMode && !IsCurrentPartInteractive();
            Debug.Log($"🔍 Mode={storyInteractionMode}, isStoryMode={isStoryMode}, currentPart={currentStoryPart}, IsInteractive={IsCurrentPartInteractive()}, ShouldAutoAdvance={shouldAutoAdvance}");
            
            // Si c'est une partie non-interactive en story mode, avancer automatiquement après l'audio
            if (shouldAutoAdvance)
            {
                Debug.Log("⏱️ Partie non-interactive: avancement automatique programmé après l'audio");
                StartCoroutine(AutoAdvanceStoryPartAfterAudio());
            }
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

        // ✅ UTILISER L'ÉMOTION DÉTECTÉE DE LA RÉPONSE PRÉCÉDENTE
        // Au lieu d'attendre des tags {EMOTION} du LLM
        if (isStoryMode && detectedUserEmotion != Emotion.Surprise)
        {
            Debug.Log($"🎭 UTILISATION DE L'ÉMOTION DÉTECTÉE: {detectedUserEmotion}");
            StartCoroutine(PlayDetectedEmotion(detectedUserEmotion, response));
        }
        else
        {
            // Si pas d'émotion spécifique, essayer de parser les tags (fallback)
            var segments = ParseEmotionSegments(response);
            if (segments.Count > 0)
            {
                StartCoroutine(ProcessEmotionSequence(response));
            }
            else
            {
                Debug.Log("😐 Aucune émotion détectée via analyse ou tags");
            }
        }

        // Supprime les tags pour le TTS
        return Regex.Replace(response, "{.*?}", "").Trim();
    }

    /// <summary>
    /// Joue l'émotion détectée à travers toute la durée du texte
    /// </summary>
    IEnumerator PlayDetectedEmotion(Emotion emotion, string text)
    {
        Debug.Log($"🎭 JOUANT ÉMOTION DÉTECTÉE: {emotion}");
        
        // Estimer la durée du texte
        float totalDuration = EstimateDuration(text);
        Debug.Log($"⏱️ Durée estimée: {totalDuration:F2}s");
        
        // Jouer l'émotion pendant toda la durée
        StartCoroutine(PlayEmotionEnvelope(EmotionToTag(emotion), totalDuration));
        
        yield return new WaitForSeconds(totalDuration);
        Debug.Log($"✅ Émotion {emotion} jouée jusqu'à la fin");
    }

    /// <summary>
    /// Convertit une enum Emotion en tag texte pour PlayEmotionEnvelope
    /// </summary>
    /// <summary>
    /// Convertit une enum Emotion en tag texte pour PlayEmotionEnvelope
    /// UNIQUEMENT les 6 émotions primaires
    /// </summary>
    private string EmotionToTag(Emotion emotion)
    {
        return emotion switch
        {
            Emotion.Joy => "JOY",
            Emotion.Sadness => "SAD",
            Emotion.Anger => "ANGER",
            Emotion.Fear => "FEAR",
            Emotion.Surprise => "SURPRISE",
            Emotion.Disgust => "DISGUST",
            _ => "SURPRISE"  // Fallback
        };
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
        {
            Debug.Log("😐 Aucun tag émotionnel détecté dans la réponse de l'agent");
            yield break;
        }
        
        // Afficher les émotions détectées
        string emotionsList = "";
        foreach (var seg in segments)
        {
            emotionsList += seg.emotion + ", ";
        }
        Debug.Log($"😊 ÉMOTIONS DÉTECTÉES: {emotionsList.TrimEnd(',', ' ')}");

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
        Debug.Log($"🎭 JOUANT ÉMOTION: {emotion} (durée: {duration:F2}s)");
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

    /// <summary>
    /// Envoie un message pour une partie de l'histoire SANS historique de conversation
    /// Cela évite que l'historique ancien pollue la narration
    /// </summary>
    private void SendChatForStory(JsonValue storyMessage)
    {
        if (storyMessage.ArrayValues.Count == 0)
            return;

        JsonValue fullConv = new JsonValue(JsonType.Array);
        
        // Ajouter le systemPrompt
        JsonValue systemTurn = new JsonValue(JsonType.Object);
        JsonValue systemRole = new JsonValue(JsonType.String);
        systemRole.StringValue = "system";
        JsonValue systemContent = new JsonValue(JsonType.String);
        systemContent.StringValue = Regex.Replace(Regex.Replace(preprompt, "[\"\']", ""), "\\s", " ");
        systemTurn.ObjectValues.Add("role", systemRole);
        systemTurn.ObjectValues.Add("content", systemContent);
        fullConv.ArrayValues.Add(systemTurn);
        
        // Ajouter UNIQUEMENT le message de cette partie (PAS l'historique)
        fullConv.ArrayValues.AddRange(storyMessage.ArrayValues);
        
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
     * Cette méthode permet de faire jouer des AUs à l'agent
     * Avec vérification de sécurité pour éviter les AUs invalides
     */
    public void DisplayAUs(int[] aus, int[] intensities, float duration)
    {
        // Vérification de sécurité : filtrer les AUs valides sur l'avatar
        int[] validAUs = { 1, 2, 4, 5, 6, 7, 9, 10, 12, 14, 15, 16, 17, 18, 20, 22, 23, 24, 25, 26, 27 };
        
        List<int> safeAUs = new List<int>();
        List<int> safeIntensities = new List<int>();
        
        for (int i = 0; i < aus.Length; i++)
        {
            if (System.Array.Exists(validAUs, element => element == aus[i]))
            {
                safeAUs.Add(aus[i]);
                safeIntensities.Add(intensities[i]);
            }
            else
            {
                Debug.LogWarning($"⚠️ AU {aus[i]} n'existe pas sur l'avatar. AU ignoré.");
            }
        }
        
        if (safeAUs.Count > 0)
        {
            Debug.Log($"🎨 DisplayAUs sécurisé: AUs={string.Join(",", safeAUs)}, Duration={duration}");
            faceExpression.setFacialAUs(safeAUs.ToArray(), safeIntensities.ToArray(), duration);
        }
        else
        {
            Debug.LogWarning("⚠️ Aucun AU valide à afficher");
        }
    }

    /*
    * Exemple de fonction d�clenchant une expression �motionnelle
    * intensity_factor devrait �tre entre 0 et 1
    */
    public void Doubt(float intensity_factor, float duration)
    {
        DisplayAUs(new int[] { 6, 4, 14 }, new int[] { (int)(intensity_factor * 100), (int)(intensity_factor * 80), (int)(intensity_factor * 80) }, duration);
    }

    // ========== HISTOIRE LUCAS ==========

    /// <summary>
    /// Crée les 8 parties de l'histoire Thomas
    /// </summary>
    private void CreateLucasStory()
    {
        lucasStory.Clear();

        lucasStory.Add(new StoryPart(1, "Le Réveil à Oberkampf",
            "Le 8 avril, à 7h10, Thomas Rivière, 29 ans, journaliste indépendant, se réveille dans son appartement au 42 rue Oberkampf à Paris. Il enfile un pantalon et un pull gris clair. Sur son bureau, un enregistreur vocal clignote encore depuis la veille.",
            "Es-tu intrigué par cet enregistreur laissé allumé ?"
        ));

        lucasStory.Add(new StoryPart(2, "Le Trajet vers Bastille",
            "À 8h00, Thomas quitte son appartement. Il prend la ligne 5 du métro à République en direction de Bastille, où il arrive à 8h12. Il a rendez-vous avec une source anonyme dans un café nommé \"Le Central\".",
            "Ressens-tu une certaine tension avant cette rencontre ?"
        ));

        lucasStory.Add(new StoryPart(3, "La Rencontre au Café",
            "À 8h20, un homme d'une cinquantaine d'années, manteau beige et regard fuyant, s'assoit face à lui. Sans se présenter, il lui tend une enveloppe kraft épaisse. À l'intérieur, des photos et un plan d'immeuble situé au 17 rue des Érables.",
            "Cette rencontre te semble-t-elle inquiétante ?"
        ));

        lucasStory.Add(new StoryPart(4, "Première Visite au 17 rue des Érables",
            "À 9h15, Thomas se rend à l'adresse indiquée. L'immeuble est ancien, avec une façade fissurée et des volets fermés. Une affichette mentionne qu'il est inhabité depuis 10 ans.",
            "Penses-tu que cet endroit cache quelque chose de suspect ?"
        ));

        lucasStory.Add(new StoryPart(5, "L'Entrée par la Porte Entrouverte",
            "En contournant le bâtiment, Thomas découvre une porte entrouverte à l'arrière. Il hésite quelques secondes avant d'entrer. À l'intérieur, l'air est froid et une odeur de poussière flotte.",
            "Trouves-tu son choix d'entrer risqué ?"
        ));

        lucasStory.Add(new StoryPart(6, "Le Symbole Mystérieux au Rez-de-Chaussée",
            "À 10h05, il explore le rez-de-chaussée. Sur un mur, il remarque des inscriptions effacées et un symbole étrange dessiné à la craie. Il enregistre ses observations avec son dictaphone.",
            "Ce symbole te semble-t-il important pour la suite ?"
        ));

        lucasStory.Add(new StoryPart(7, "L'Ordinateur au Premier Étage",
            "À 10h40, Thomas monte au premier étage. Une pièce attire son attention : une table, une chaise, et un ordinateur portable encore branché. L'écran affiche un fichier nommé \"Dossier_21\".",
            "Es-tu curieux de savoir ce que contient ce fichier ?"
        ));

        lucasStory.Add(new StoryPart(8, "La Révélation et la Fin",
            "À 11h00, il ouvre le fichier. Des documents détaillent une série d'événements inexpliqués liés à l'immeuble, remontant à 2003. Parmi les noms mentionnés, Thomas reconnaît celui de son ancien rédacteur en chef.",
            "Merci de m'avoir écouté."
        ));
    }

    /// <summary>
    /// Détermine si la partie courante doit avoir une interaction utilisateur (question)
    /// </summary>
    private bool IsCurrentPartInteractive()
    {
        if (storyInteractionMode == 0)
        {
            // Mode 0: Parties 0-3 non-interactives, Parties 4-7 interactives
            return currentStoryPart >= 4;
        }
        else
        {
            // Mode 1: Parties 0-3 interactives, Parties 4-7 non-interactives
            return currentStoryPart < 4;
        }
    }

    /// <summary>
    /// Coroutine pour avancer automatiquement à la partie suivante après que l'audio finisse
    /// Utilisée pour les parties non-interactives
    /// </summary>
    private IEnumerator AutoAdvanceStoryPartAfterAudio()
    {
        Debug.Log($"📕 AUTO-ADVANCE COROUTINE STARTED for part {currentStoryPart}");
        // Attendre que l'audio commence à jouer
        yield return new WaitForSeconds(0.2f);
        
        Debug.Log($"⏳ Waiting for audio to finish (isPlaying={audioSource.isPlaying})...");
        
        // Attendre que l'audio finisse de jouer
        float timeout = 0f;
        while (audioSource.isPlaying && timeout < 300f) // Max 5 minutes de timeout
        {
            yield return new WaitForSeconds(0.1f);
            timeout += 0.1f;
        }
        
        Debug.Log($"🎵 Audio finished (or timeout) - Auto-advancing from part {currentStoryPart}");
        
        // Avancer à la partie suivante sans attendre d'interaction utilisateur
        // On crée une progression fictive
        lastUserResponse = "";  // Pas de réponse utilisateur
        NextStoryPart();
    }

    /// <summary>
    /// Démarre la narration de l'histoire
    /// </summary>
    private void StartStory()
    {
        if (lucasStory.Count == 0)
            return;

        Debug.Log("📖 DÉMARRAGE DE L'HISTOIRE: Histoire Lucas (Mode " + storyInteractionMode + ")");
        isStoryMode = true;
        currentStoryPart = 0;
        TellCurrentStoryPart();
    }

    /// <summary>
    /// Génère les directives émotionnelles détaillées selon l'émotion détectée
    /// </summary>
    private string GetEmotionalToneGuidance(Emotion emotion, int storyPartIndex)
    {
        if (storyPartIndex == 0 || emotion == Emotion.Surprise)
            return ""; // Pas de contexte émotionnel spécifique par défaut

        string toneGuidance = emotion switch
        {
            Emotion.Sadness => 
                "\nTON DE VOIX: Parle avec une voix douce, mélancolique, pensive. Ralentis le rythme. Laisse des pauses significatives.",

            Emotion.Joy =>
                "\nTON DE VOIX: Parle avec chaleur et optimisme. La voix est plus légère et porte de l'espoir.",

            Emotion.Fear =>
                "\nTON DE VOIX: Parle avec une légère appréhension. La voix est prudente et attentive.",

            Emotion.Anger =>
                "\nTON DE VOIX: Parle avec une intensité contenue et fermeté. La voix est directe.",

            Emotion.Surprise =>
                "\nTON DE VOIX: Parle avec étonnement. La voix monte légèrement sur les moments clés.",

            Emotion.Disgust =>
                "\nTON DE VOIX: Parle avec une légère répulsion discrète. La voix montre un dégoût subtil.",

            _ => ""
        };

        if (string.IsNullOrEmpty(toneGuidance))
            return "";

        return toneGuidance;
    }

    private string GetDetailedEmotionContext(Emotion emotion, int storyPartIndex)
    {
        if (storyPartIndex == 0 || emotion == Emotion.Surprise)
            return ""; // Pas de contexte émotionnel pour la première partie

        string emotionGuidance = emotion switch
        {
            Emotion.Sadness => 
                "⚠️ CONTEXTE: L'utilisateur a manifesté de la TRISTESSE/MÉLANCOLIE.\n" +
                "📝 DIRECTIVE: Renforce les éléments émotionnels profonds et mélancoliques. " +
                "Utilise une tonalité poétique et réfléchie. Laisse de l'espace pour la réflexion émotionnelle. " +
                "Intègre les tags {SAD} et {SURPRISE} naturellement.",

            Emotion.Joy =>
                "⚠️ CONTEXTE: L'utilisateur a manifesté de la JOIE/ENTHOUSIASME.\n" +
                "📝 DIRECTIVE: Maintiens une énergie positive et engageante. " +
                "Alterne entre {JOY} pour les moments d'espoir et {SURPRISE} pour les découvertes. " +
                "Montre comment l'histoire progresse positivement.",

            Emotion.Fear =>
                "⚠️ CONTEXTE: L'utilisateur a manifesté de la PEUR/APPRÉHENSION.\n" +
                "📝 DIRECTIVE: Construis progressivement la tension tout en assurant l'engagement. " +
                "Utilise {FEAR} pour montrer le suspense, puis {SURPRISE} quand les révélations arrivent. " +
                "Balance entre tension et progression de l'histoire.",

            Emotion.Anger =>
                "⚠️ CONTEXTE: L'utilisateur a manifesté de la COLÈRE/FRUSTRATION.\n" +
                "📝 DIRECTIVE: Reconnaissez le conflit émotionnel de l'utilisateur. " +
                "Utilise {ANGER} brièvement, puis {SURPRISE} pour transformer l'émotion en curiosité. " +
                "Chaque révélation doit au progressivement amener à la compréhension.",

            Emotion.Surprise =>
                "⚠️ CONTEXTE: L'utilisateur a manifesté de la SURPRISE/ÉTONNEMENT.\n" +
                "📝 DIRECTIVE: Maintiens le ton revelateur et suspenseful. " +
                "Utilise {SURPRISE} pour les moments clés et {FEAR} ou {SADNESS} pour ancrer les émotions. " +
                "Chaque détail doit inviter à plus de découvertes.",

            Emotion.Disgust =>
                "⚠️ CONTEXTE: L'utilisateur a manifesté du DÉGOÛT/RÉPULSION.\n" +
                "📝 DIRECTIVE: Respectez cette émotion sans la renforcer négativement. " +
                "Utilisez {DISGUST} brièvement, puis transformez via {SURPRISE} ou {SADNESS}. " +
                "Apportez de la nuance et de la compréhension pour progresser dans l'histoire.",

            _ => ""
        };

        if (string.IsNullOrEmpty(emotionGuidance))
            return "";

        return $"\n\n{emotionGuidance}";
    }

    /// <summary>
    /// Affiche l'émotion détectée sur le visage de l'agent avec les unités d'action faciales appropriées
    /// Utilise UNIQUEMENT les 6 émotions primaires: JOY, SAD, ANGER, SURPRISE, FEAR, DISGUST
    /// </summary>
    private void DisplayEmotionOnFace(Emotion emotion)
    {
        if (faceExpression == null)
        {
            Debug.LogWarning("⚠️ FaceExpression not assigned!");
            return;
        }

        Debug.Log($"🎭 EXPRESSION FACIALE: Affichage de l'émotion {emotion}");

        // Mapper chaque émotion à des unités d'action faciales (Action Units)
        // Les AU codes suivent le système Facial Action Coding System (FACS)
        switch (emotion)
        {
            case Emotion.Sadness:
                // AU: Baissement des sourcils (4), Baissement des coins de la bouche (15,17)
                DisplayAUs(new int[] { 4, 15, 17 }, new int[] { 60, 50, 50 }, 1.5f);
                Debug.Log("😢 Affichage: TRISTESSE/MÉLANCOLIE");
                break;

            case Emotion.Joy:
                // AU: Sourire de Duchenne (6, 12) = levée des pommettes + coin des lèvres
                DisplayAUs(new int[] { 6, 12 }, new int[] { 70, 80 }, 1.5f);
                Debug.Log("😊 Affichage: JOIE/ENTHOUSIASME");
                break;

            case Emotion.Fear:
                // AU: Levée des sourcils (1,2), Ouverture des yeux (5,26), Tension des lèvres (23)
                DisplayAUs(new int[] { 1, 2, 5, 26, 23 }, new int[] { 50, 50, 70, 70, 60 }, 1.5f);
                Debug.Log("😨 Affichage: PEUR/APPRÉHENSION");
                break;

            case Emotion.Anger:
                // AU: Sourcils abaissés et rapprochés (4), Fermeture des lèvres (23,24)
                DisplayAUs(new int[] { 4, 23, 24 }, new int[] { 70, 60, 50 }, 1.5f);
                Debug.Log("😠 Affichage: COLÈRE");
                break;

            case Emotion.Surprise:
                // AU: Levée des sourcils (1,2), Ouverture de la bouche (26)
                DisplayAUs(new int[] { 1, 2, 26, 5 }, new int[] { 80, 80, 70, 60 }, 1.2f);
                Debug.Log("😲 Affichage: SURPRISE/ÉTONNEMENT");
                break;

            case Emotion.Disgust:
                // AU: Levée de la lèvre supérieure (9,10), Plissement du nez (9)
                DisplayAUs(new int[] { 9, 10 }, new int[] { 50, 60 }, 1.2f);
                Debug.Log("🤢 Affichage: DÉGOÛT");
                break;

            default:
                // Expression par défaut pour les émotions non spécifiées
                Debug.Log("😐 Affichage: PAR DÉFAUT");
                break;
        }
    }

    /// <summary>
    /// Raconte la partie actuelle de l'histoire
    /// </summary>
    private void TellCurrentStoryPart()
    {
        // Sécurité absolue: ne rien faire si l'histoire n'est pas en cours
        if (!isStoryMode)
        {
            Debug.Log("🔇 TellCurrentStoryPart() appelée mais isStoryMode=false - ARRÊT COMPLET");
            return;
        }

        if (currentStoryPart >= lucasStory.Count)
        {
            Debug.Log("🏁 TellCurrentStoryPart(): HISTOIRE TERMINÉE! (currentStoryPart >= Count)");
            isStoryMode = false;
            return;
        }

        StoryPart part = lucasStory[currentStoryPart];
        bool isInteractive = IsCurrentPartInteractive();
        
        Debug.Log($"📕 RACONTE PARTIE {part.id}/8: {part.title} (Mode: {(isInteractive ? "INTERACTIF" : "AUTOMATIQUE")})");
        
        // Pour les parties après la première, inclure UNIQUEMENT des directives de TON/VOIX, pas de contenu
        string emotionContext = isInteractive ? GetEmotionalToneGuidance(detectedUserEmotion, currentStoryPart) : "";
        
        // Construire le message pour l'IA avec la partie et la question (ou pas)
        string storyMessage;
        
        if (isInteractive)
        {
            // Mode INTERACTIF: poser la question et attendre la réponse de l'utilisateur
            storyMessage = $"IMPORTANT: Tu dois faire EXACTEMENT ceci:\n\n" +
                $"1. Lis le texte suivant CARACTÈRE PAR CARACTÈRE (aucune modification, aucun ajout, aucune suppression):\n\n" +
                $"\"{part.narrative}\"\n\n" +
                $"2. Puis pose EXACTEMENT cette question (parole par parole):\n\n" +
                $"\"{part.userQuestion}\"\n\n" +
                $"RÈGLES ABSOLUES:\n" +
                $"- ZÉRO modification du texte\n" +
                $"- ZÉRO ajout de mots\n" +
                $"- ZÉRO suppression de mots\n" +
                $"- ZÉRO réinterprétation\n" +
                $"- ZÉRO paraphrase\n" +
                $"Copie le texte exactement, puis pose la question exactement.{emotionContext}";
        }
        else
        {
            // Mode AUTOMATIQUE: ne pas poser de question, continuer la narration
            storyMessage = $"IMPORTANT: Tu dois faire EXACTEMENT ceci:\n\n" +
                $"Lis le texte suivant CARACTÈRE PAR CARACTÈRE (aucune modification, aucun ajout, aucune suppression):\n\n" +
                $"\"{part.narrative}\"\n\n" +
                $"RÈGLES ABSOLUES:\n" +
                $"- ZÉRO modification du texte\n" +
                $"- ZÉRO ajout de mots\n" +
                $"- ZÉRO suppression de mots\n" +
                $"- ZÉRO réinterprétation\n" +
                $"- ZÉRO paraphrase\n" +
                $"- NE POSE PAS DE QUESTION APRÈS\n" +
                $"Lis simplement le texte exactement comme écrit.";
        }
        
        // Créer une conversation FRAÎCHE pour cette partie (pas d'historique qui interfère)
        JsonValue storyConversation = new JsonValue(JsonType.Array);
        
        // Ajouter le message de la partie
        JsonValue userTurn = new JsonValue(JsonType.Object);
        JsonValue userRole = new JsonValue(JsonType.String);
        userRole.StringValue = "user";
        JsonValue userContent = new JsonValue(JsonType.String);
        userContent.StringValue = storyMessage;
        userTurn.ObjectValues.Add("role", userRole);
        userTurn.ObjectValues.Add("content", userContent);
        storyConversation.ArrayValues.Add(userTurn);

        // Envoyer au chat avec conversation fraîche
        SendChatForStory(storyConversation);
    }

    /// <summary>
    /// Passe à la partie suivante de l'histoire après réponse utilisateur
    /// Analyse l'émotion avec contexte avant de continuer
    /// </summary>
    public void NextStoryPart()
    {
        if (!isStoryMode)
        {
            Debug.Log("⚠️ NextStoryPart() appelé mais isStoryMode=false");
            return;
        }

        // Vérifier si la partie actuelle était interactive
        bool wasInteractive = IsCurrentPartInteractive();
        
        Debug.Log($"📌 NextStoryPart() - Partie actuelle: {currentStoryPart}, Mode: {(wasInteractive ? "INTERACTIF" : "AUTOMATIQUE")}, Réponse: '{lastUserResponse}'");

        // N'analyser l'émotion que si la partie était INTERACTIVE
        if (wasInteractive && !string.IsNullOrWhiteSpace(lastUserResponse) && currentStoryPart < lucasStory.Count)
        {
            Debug.Log($"✅ Analysing emotion for response: '{lastUserResponse}'");
            
            var emotionData = emotionMapper.MapUserResponseToEmotion(
                lastUserResponse,
                currentStoryPart,
                out Emotion detectedEmotion
            );
            
            detectedUserEmotion = detectedEmotion;
            
            Debug.Log($"😊 ANALYSE ÉMOTIONNELLE PARTIE {currentStoryPart}: " +
                $"Émotion={emotionData.detectedEmotion}, " +
                $"Confiance={emotionData.confidence:F2}, " +
                $"Tag={emotionData.emotionTag}");
            Debug.Log($"   → Raison: {emotionData.reasoning}");
            
            // ✅ AFFICHER L'ÉMOTION SUR LE VISAGE DE L'AGENT
            Debug.Log($"🎭 Appel à DisplayEmotionOnFace avec émotion: {detectedUserEmotion}");
            DisplayEmotionOnFace(detectedUserEmotion);
        }
        else if (!wasInteractive)
        {
            Debug.Log($"ℹ️ Partie non-interactive - pas d'analyse émotionnelle");
        }
        else
        {
            Debug.Log($"⚠️ Pas d'analyse émotionnelle: lastUserResponse empty={string.IsNullOrWhiteSpace(lastUserResponse)}, currentStoryPart={currentStoryPart}, count={lucasStory.Count}");
        }

        // Avancer à la partie suivante
        currentStoryPart++;
        
        if (currentStoryPart >= lucasStory.Count)
        {
            Debug.Log($"🏁 HISTOIRE TERMINÉE! (Mode {storyInteractionMode}, dernière partie = {currentStoryPart - 1})");
            isStoryMode = false;
            
            // Désactiver le microphone/dictation pour éviter que l'agent continue à parler
            if (useWhisper)
            {
                if (microphoneRecord.IsRecording)
                {
                    microphoneRecord.StopRecord();
                }
            }
            else
            {
                if (dictationRecognizer.Status == SpeechSystemStatus.Running)
                {
                    dictationRecognizer.Stop();
                }
            }
            
            Debug.Log("🔇 Microphone/Dictation désactivé après fin de l'histoire");
            return;
        }

        TellCurrentStoryPart();
    }

}
