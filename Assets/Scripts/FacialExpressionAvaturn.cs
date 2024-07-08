
using ACTA;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * Ce script gère les expressions faciales et l'animation des lèvres de l'agent.
 * Il est un peu basique, mais couvre les besoins minimums pour le cours IA & SHS
 */
public class FacialExpressionAvaturn : MonoBehaviour
{

    public AudioSource audioSource;
    public List<SkinnedMeshRenderer> skinnedMeshRenderers;


    private float referenceLipTime;
    private float referenceFaceTime;
    private int choice;
    private float timeBetweenViseme = 0.175f;
    private float facialExpressionDuration = 2.0f;
    private int[] aus = { 0 };

    /*Pour chaque paramètre du visage, on va conserver la valeur cible, vers laquelle on souhaite que le muscle du visage aille,
    * et la valeur précédente (dans le paramètre Back) afin de pouvoir interpoler ensuite entre ces deux valeurs pour animer en douceur le visage
    */

    //VISEME AVATURN
    private int viseme_sil = 0;
    private int viseme_PP = 0;
    private int viseme_FF = 0;
    private int viseme_TH = 0;
    private int viseme_DD = 0;

    private int viseme_kk = 0;
    private int viseme_CH = 0;
    private int viseme_SS = 0;
    private int viseme_nn = 0;
    private int viseme_RR = 0;
    private int viseme_aa = 0;
    private int viseme_E = 0;
    private int viseme_I = 0;
    private int viseme_O = 0;
    private int viseme_U = 0;
    //VISEME AVATURN BACK
    private int viseme_sil_back = 0;
    private int viseme_PP_back = 0;
    private int viseme_FF_back = 0;
    private int viseme_TH_back = 0;
    private int viseme_DD_back = 0;

    private int viseme_kk_back = 0;
    private int viseme_CH_back = 0;
    private int viseme_SS_back = 0;
    private int viseme_nn_back = 0;
    private int viseme_RR_back = 0;
    private int viseme_aa_back = 0;
    private int viseme_E_back = 0;
    private int viseme_I_back = 0;
    private int viseme_O_back = 0;
    private int viseme_U_back = 0;
    //FACE (expression faciale des émotions par AUs)

    //AU2
    private int browInnerUp = 0;
    //AU1//AU4
    private int browDownLeft = 0;
    private int browDownRight = 0;
    //AU5
    private int eyeWideLeft = 0;
    private int eyeWideRight = 0;
    //AU6
    private int eyeSquintLeft = 0;
    private int eyeSquintRight = 0;
    //AU12
    private int mouthSmileLeft = 0;
    private int mouthSmileRight = 0;
    //AU15
    private int mouthFrownRight = 0;
    private int mouthFrownLeft = 0;
    //AU20
    private int mouthOpen = 0;
    //AU23
    private int mouthPucker = 0;
    //AU26
    private int jawOpen = 0;

    //FACE_BACK
    //AU2
    private int browInnerUp_back = 0;
    //AU1//AU4
    private int browDownLeft_back = 0;
    private int browDownRight_back = 0;
    //AU5
    private int eyeWideLeft_back = 0;
    private int eyeWideRight_back = 0;
    //AU6
    private int eyeSquintLeft_back = 0;
    private int eyeSquintRight_back = 0;
    //AU12
    private int mouthSmileLeft_back = 0;
    private int mouthSmileRight_back = 0;
    //AU15
    private int mouthFrownRight_back = 0;
    private int mouthFrownLeft_back = 0;
    //AU20
    private int mouthOpen_back = 0;
    //AU23
    private int mouthPucker_back = 0;
    //AU26
    private int jawOpen_back = 0;


    void Start()
    {
        referenceLipTime = Time.time;
        referenceFaceTime = Time.time;
        audioSource = GetComponent<AudioSource>();
        /*if (SkinnedMeshRendererTarget == null)
            SkinnedMeshRendererTarget = gameObject.GetComponent<SkinnedMeshRenderer>();
        */

    }

    void Update()
    {
        float now = Time.time;


        //FACE LERP
        float faceLerp = (now - referenceFaceTime) / facialExpressionDuration;
        //LIP LERP (CONSTANT)
        float lipLerp = (now - referenceLipTime) / timeBetweenViseme;
        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Count > 0)
        {
            if (audioSource.isPlaying || Narrator.isSpeaking())
            {
                if (now - referenceLipTime > timeBetweenViseme)
                {
                    UpdateLipBackWeight();
                    choice = Random.Range(0, 11);
                    referenceLipTime = Time.time;
                    setRandomViseme(choice);
                }

            }
            else
            {
                setVisemeNeutral();
                if (now - referenceFaceTime > facialExpressionDuration)
                {
                    UpdateFaceBackWeight();

                    setFaceNeutral();
                    referenceFaceTime = Time.time;

                }
            }
            //Interpolation des animations
            lerpViseme(lipLerp);
            lerpFace(faceLerp);

        }
    }

    public void setFacialAUs(int[] aus, int[] intensities, float duration)
    {
        this.aus = aus;
        facialExpressionDuration = duration;
        referenceFaceTime = Time.time;
        for (int i = 0; i < aus.Length; i++)
        {
            //-joie : AUs 6 et 12
            //-tristesse : AUs 1, 4 et 15
            //-peur : AUs 1, 2, 4, 5, 7, 20 et 26
            //-colère : AUs 4, 5, 7 et 23
            //Les AUs n'étant pas directement disponible dans le modèle 3D de Unity, nous les convertissons vers les BlendShapes équivalentes
            switch (aus[i])
            {

                case 1: browDownLeft = intensities[i]; browDownRight = intensities[i]; break;
                case 2: browInnerUp = intensities[i]; break;
                case 4:
                    browDownLeft = intensities[i]; browDownLeft = intensities[i]; break;
                case 5: eyeWideLeft = intensities[i]; eyeWideRight = intensities[i]; break;
                case 6: eyeSquintLeft = intensities[i]; eyeSquintRight = intensities[i]; break;
                case 7: eyeSquintLeft = intensities[i]; eyeSquintRight = intensities[i]; break;
                case 12: mouthSmileLeft = intensities[i]; mouthSmileRight = intensities[i]; break;
                case 15: mouthFrownLeft = intensities[i]; mouthFrownRight = intensities[i]; break;
                case 20: mouthOpen = intensities[i]; break;
                case 23: mouthPucker = intensities[i]; break;
                case 26: jawOpen = intensities[i]; break;
                default: break;
            }
        }

    }

    public void UpdateLipBackWeight()
    {
        viseme_sil_back = viseme_sil;
        viseme_PP_back = viseme_PP;
        viseme_FF_back = viseme_FF;
        viseme_TH_back = viseme_TH;
        viseme_DD_back = viseme_DD;

        viseme_kk_back = viseme_kk;
        viseme_CH_back = viseme_CH;
        viseme_SS_back = viseme_SS;
        viseme_nn_back = viseme_nn;
        viseme_RR_back = viseme_RR;
        viseme_aa_back = viseme_aa;
        viseme_E_back = viseme_E;
        viseme_I_back = viseme_I;
        viseme_O_back = viseme_O;
        viseme_U_back = viseme_U;

    }

    public void UpdateFaceBackWeight()
    {
        browInnerUp_back = browInnerUp;
        browDownLeft_back = browDownLeft;
        browDownRight_back = browDownRight;
        eyeWideLeft_back = eyeWideLeft;
        eyeWideRight_back = eyeWideRight;
        eyeSquintLeft_back = eyeSquintLeft;
        eyeSquintRight_back = eyeSquintRight;
        mouthSmileLeft_back = mouthSmileLeft;
        mouthSmileRight_back = mouthSmileRight;
        mouthFrownRight_back = mouthFrownLeft;
        mouthFrownLeft_back = mouthFrownRight;
        mouthOpen_back = mouthOpen;
        mouthPucker_back = mouthPucker;
        jawOpen_back = jawOpen;
    }

    /*!
       * @brief A function for getting blendshape index by name.
       * @return int
       */
    public int getBlendShapeIndex(SkinnedMeshRenderer smr, string bsName)
    {
        Mesh m = smr.sharedMesh;

        for (int i = 0; i < m.blendShapeCount; i++)
        {
            string name = m.GetBlendShapeName(i);
            if (bsName.Equals(m.GetBlendShapeName(i)) == true)
                return i;
        }

        return 0;
    }

    public void setRandomViseme(int choice)
    {

        switch (choice)
        {
            case 0: setViseme_PP(); break;
            case 1: setViseme_FF(); break;
            case 2: setViseme_TH(); break;
            case 3: setViseme_DD(); break;
            case 4: setViseme_kk(); break;
            case 5: setViseme_CH(); break;
            case 6: setViseme_SS(); break;
            case 7: setViseme_nn(); break;
            case 8: setViseme_RR(); break;
            case 9: setViseme_aa(); break;
            case 10: setViseme_E(); break;
            case 11: setViseme_I(); break;
            case 12: setViseme_O(); break;
            case 13: setViseme_U(); break;
            default: setViseme_sil(); break;

        }
    }



    public void setVisemeNeutral()
    {
        viseme_sil = 0;
        viseme_PP = 0;
        viseme_FF = 0;
        viseme_TH = 0;
        viseme_DD = 0;

        viseme_kk = 0;
        viseme_CH = 0;
        viseme_SS = 0;
        viseme_nn = 0;
        viseme_RR = 0;
        viseme_aa = 0;
        viseme_E = 0;
        viseme_I = 0;
        viseme_O = 0;
        viseme_U = 0;
    }

    public void setFaceNeutral()
    {
        browInnerUp = 0;
        browDownLeft = 0;
        browDownRight = 0;
        eyeWideLeft = 0;
        eyeWideRight = 0;
        eyeSquintLeft = 0;
        eyeSquintRight = 0;
        mouthSmileLeft = 0;
        mouthSmileRight = 0;
        mouthFrownRight = 0;
        mouthFrownLeft = 0;
        mouthOpen = 0;
        mouthPucker = 0;
        jawOpen = 0;
    }
    /*
     * On vient animer les Blendshapes des lèvres à l'aide de l'interpolation entre nos deux valeurs
     */
    public void lerpViseme(float lerp)
    {
        foreach (SkinnedMeshRenderer SkinnedMeshRendererTarget in skinnedMeshRenderers)
        {
            Mesh m = SkinnedMeshRendererTarget.sharedMesh;
            int i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_sil");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_sil_back, viseme_sil, lerp));

            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_PP");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_PP_back, viseme_PP, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_FF");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_FF_back, viseme_FF, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_TH");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_TH_back, viseme_TH, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_DD");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_DD_back, viseme_DD, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_kk");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_kk_back, viseme_kk, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_CH");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_CH_back, viseme_CH, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_SS");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_SS_back, viseme_SS, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_nn");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_nn_back, viseme_nn, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_RR");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_RR_back, viseme_RR, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_aa");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_aa_back, viseme_aa, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_O");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_O_back, viseme_O, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_E");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_E_back, viseme_E, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_I");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_I_back, viseme_I, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "viseme_U");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(viseme_U_back, viseme_U, lerp));
        }

    }

    /*
     * On vient animer les BlendShapes du visage (correspondant plus ou moins aux AUs) à l'aide de l'interpolation
     */
    public void lerpFace(float lerp)
    {
        foreach (SkinnedMeshRenderer SkinnedMeshRendererTarget in skinnedMeshRenderers)
        {
            Mesh m = SkinnedMeshRendererTarget.sharedMesh;
            int i = getBlendShapeIndex(SkinnedMeshRendererTarget, "browInnerUp");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(browInnerUp_back, browInnerUp, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "browDownLeft");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(browDownLeft_back, browDownLeft, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "browDownRight");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(browDownRight_back, browDownRight, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "eyeWideLeft");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(eyeWideLeft_back, eyeWideLeft, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "eyeWideRight");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(eyeWideRight_back, eyeWideRight, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "eyeSquintLeft");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(eyeSquintLeft_back, eyeSquintLeft, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "eyeSquintRight");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(eyeSquintRight_back, eyeSquintRight, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "mouthSmileLeft");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(mouthSmileLeft_back, mouthSmileLeft, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "mouthSmileRight");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(mouthSmileRight_back, mouthSmileRight, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "mouthFrownRight");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(mouthFrownRight_back, mouthFrownRight, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "mouthFrownLeft");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(mouthFrownLeft_back, mouthFrownLeft, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "mouthOpen");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(mouthOpen_back, mouthOpen, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "mouthPucker");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(mouthPucker_back, mouthPucker, lerp));
            i = getBlendShapeIndex(SkinnedMeshRendererTarget, "jawOpen");
            SkinnedMeshRendererTarget.SetBlendShapeWeight(i, (int)Mathf.Lerp(jawOpen_back, jawOpen, lerp));
        }
    }


    public void setViseme_PP()
    {
        setVisemeNeutral();
        viseme_PP = 100;
    }

    public void setViseme_FF()
    {
        setVisemeNeutral();
        viseme_FF = 100;
    }

    public void setViseme_TH()
    {
        setVisemeNeutral();
        viseme_TH = 100;
    }

    public void setViseme_DD()
    {
        setVisemeNeutral();
        viseme_DD = 100;
    }

    public void setViseme_kk()
    {
        setVisemeNeutral();
        viseme_kk = 100;
    }

    public void setViseme_CH()
    {
        setVisemeNeutral();
        viseme_CH = 100;
    }

    public void setViseme_SS()
    {
        setVisemeNeutral();
        viseme_SS = 100;
    }

    public void setViseme_nn()
    {
        setVisemeNeutral();
        viseme_nn = 100;
    }

    public void setViseme_RR()
    {
        setVisemeNeutral();
        viseme_RR = 100;
    }

    public void setViseme_aa()
    {
        setVisemeNeutral();
        viseme_aa = 100;
    }
    public void setViseme_E()
    {
        setVisemeNeutral();
        viseme_E = 100;
    }
    public void setViseme_I()
    {
        setVisemeNeutral();
        viseme_I = 100;
    }
    public void setViseme_O()
    {
        setVisemeNeutral();
        viseme_O = 100;
    }
    public void setViseme_U()
    {
        setVisemeNeutral();
        viseme_U = 100;
    }
    public void setViseme_sil()
    {
        setVisemeNeutral();
        viseme_sil = 100;
    }




}