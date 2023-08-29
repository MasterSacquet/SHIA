using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
/*
* La classe DialogManager permet de centraliser les fonctionnalités liés à l'aspect conversationnel de l'agent. 
* C'est notamment ici qu'un Chatbot est chargé et est géré. Le DialogManager met à jour l'interface de conversation
* suivant l'état du dialogue du Chatbot.
*/
public class DialogManager : MonoBehaviour
{
    
    public AudioSource audioSource;
    
    public float volume = 0.5f;
    
    private Chatbot dialog;

    public Transform informationPanel;
    public Transform textPanel;
    public Transform buttonPanel;
    public GameObject ButtonPrefab;
    public FacialExpression faceExpression;
    private Animator anim;
    //OpenMary
    public string mary_voice = "upmc-pierre-hsmm";

    // Start is called before the first frame update
    void Start()
    {
        anim = this.gameObject.GetComponent<Animator>();
        //dialog = Chatbot.readDialogueFile<ExampleBot>("Assets/DialogElements/Dialogue/json/dialogue0.json", this);
        dialog = Chatbot.readDialogueFile<Swahili>("Assets/DialogElements/Dialogue/json/swahili.json", this);
        InformationDisplay("");
        dialog.nextDialogue();

    }

    // Update is called once per frame
    void Update()
    {

    }
    /*
     * Cette méthode permet de jouer un fichier audio depuis le répertoire Resources/Sounds dont le nom est de la forme <entier>.mp3 
     */
    public void PlayAudio(int a)
    {
        try
        {
            //Charge un fichier audio depuis le répertoire Resources
            AudioClip music = (AudioClip)Resources.Load("Sounds/"+a);
            audioSource.PlayOneShot(music, volume);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /*
     * Cette méthode permet de demander à MaryTTS de générer un audio, puis de le jouer, à partir du texte
     * MaryTTS server doit donc être lancé sur la machine.
     * Pour l'instant, il est attendu que le répertoire marytts-5.2 soit copié dans le répertoire StreamingAssets du projet 
     * et que MaryTTS-Server soit exécuté à partir de /marytts-5.2/bin/ 
     */
    public void PlayAudio(string text)
    {
        // need to change player setting to allow non-https connections
        string maryTTS_request = "http://localhost:59125/process?INPUT_TEXT=" + text.Replace(" ", "+") + "&INPUT_TYPE=TEXT&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=fr&VOICE=" + mary_voice;
        Debug.Log("request: " + maryTTS_request);

        StartCoroutine(SetAudioClipFromFile(maryTTS_request));
    }

    IEnumerator SetAudioClipFromFile(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
                Debug.Log("Unable to use MaryTTS voice synthesiser.");
                string MaryTTSLocation = Application.streamingAssetsPath + "/marytts-5.2/bin/marytts-server";
                if (File.Exists(MaryTTSLocation))
                {
                    Debug.Log("Trying to restart MaryTTS server.");
                    Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized, // cannot close it if set to hidden
                            CreateNoWindow = true,
                            FileName = MaryTTSLocation
                        }
                    };
                    try
                    {
                        proc.Start();
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Failed to start MaryTTS server: ");
                        Debug.LogException(e);
                    }

                    if (proc.StartTime <= DateTime.Now && !proc.HasExited)
                    {
                        Debug.Log("Restarted MaryTTS server.");
                    }
                    else
                    {
                        var errorMsg = string.Format("Failed to started MaryTTS (server not running). Disabling MaryTTS.");

                        if (proc.HasExited)
                        {
                            errorMsg = string.Format("Failed to started MaryTTS (server was closed). Disabling MaryTTS.");
                        }

                        Debug.Log(errorMsg);
                    }
                }
                else
                {
                    Debug.Log("Failed to restart MaryTTS server. Disabling MaryTTS.");
                }
            }
            else
            {
                AudioClip music = DownloadHandlerAudioClip.GetContent(www);
                audioSource.PlayOneShot(music, volume);
            }
        }
    }


    /*
     * Cette méthode affiche du texte dans le panneau d'affichage à gauche de l'UI
     */
    public void InformationDisplay(string s)
    {

        Text text = informationPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        text.text = s;

    }
    /*
     * Cette méthode affiche le texte de la question dans la partie basse de l'UI
     */
    public void DisplayQuestion(string s)
    {
        Text text = textPanel.transform.GetComponentInChildren<Text>().GetComponent<Text>();
        text.text = s;
    }

    /* 
     * Cette méthode affiche les réponses sous forme de boutons dans l'UI.
     */
    public void DisplayAnswers(List<string> proposals)
    {


        if (proposals.Count == 0)
        {
            Debug.Log("** Il y a une erreur dans votre code: la liste de proposition est vide. Ou alors c'est la fin du dialogue?");
        }


        int i = 0;
        //On retire tout d'abord tous les boutons de l'interface
        foreach (Button child in buttonPanel.transform.GetComponentsInChildren<Button>())
        {
            Destroy(child.gameObject);
        }
        //Pour chaque valeur, on rajoute un bouton, et on lui associe la fonction responseSelected pour quand le bouton est cliqué
        for (int j = 0; j < proposals.Count; j++)
        {
            GameObject button = (GameObject)Instantiate(ButtonPrefab);
            button.GetComponentInChildren<Text>().text = proposals[j];
            int temp = j;
            button.GetComponent<Button>().onClick.AddListener(delegate { responseSelected(temp); });
            button.GetComponent<RectTransform>().position = new Vector3(i * 170.0f + 90.0f, 39.0f, 0.0f);
            button.transform.SetParent(buttonPanel);

            Debug.Log(i + ". " + temp);
            i = i + 1;
        }


    }

    public void EndDialog()
    {
        foreach (Button child in buttonPanel.transform.GetComponentsInChildren<Button>())
        {
            Destroy(child.gameObject);
        }
        anim.SetTrigger("Greet");
    }

    //Quand une réponse est choisie, on appelle la méthode du Chatbot qui gère la réponse et ensuite, si le dialogue n'est pas 
    //en train de réaliser une action spéciale, on avance dans la question suivante.
    public void responseSelected(int response)
    {
        dialog.handleResponse(response);
        dialog.nextDialogue();
    }
    /*
     * Cette méthode permet de faire jouer des AUs à l'agent
     */
    public void DisplayAUs(int[] aus, int[] intensities, float duration)
    {
        faceExpression.setFacialAUs(aus, intensities, duration);
    }
}
