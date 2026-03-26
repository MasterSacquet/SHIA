# SHIA - Science de l'humain et IA
## Agent Dialog Conversationnel Multi-Modal avec Analyse Émotionnelle et Expression Faciale

---

## Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture générale](#architecture-générale)
3. [Composants principaux](#composants-principaux)
4. [Système d'histoire](#système-dhistoire)
5. [Système émotionnel](#système-émotionnel)
6. [Modes d'interaction](#modes-dinteraction)
7. [Flux d'exécution](#flux-dexécution)
8. [Configuration](#configuration)
9. [Dépendances externes](#dépendances-externes)
10. [Guide d'utilisation](#guide-dutilisation)

---

## Vue d'ensemble

**SHIA** est un agent conversationnel interactif basé sur Unity qui crée une expérience immersive en combinant:

- 🤖 **LLM** (Large Language Model) - Ollama/OpenWebUI pour la narration et la conversation
- 🎤 **Reconnaissance vocale** - Whisper pour la capture audio et compréhension utilisateur
- 🎵 **Synthèse vocale** - Piper TTS pour la narration de l'agent
- 😊 **Analyse émotionnelle** - Détection des 6 émotions primaires via analyse contextuelle
- 🎭 **Expressions faciales** - Avatar 3D avec blendshapes FACS (Facial Action Coding System)
- 📖 **Narration adaptive** - Deux modes d'interaction (automatique et interactif) pour les histoires

Le système crée une **expérience narrative branching** où l'agent s'adapte émotionnellement aux réponses de l'utilisateur.

---

## Architecture générale

```
┌─────────────────────────────────────────────────────────────────┐
│                    SHIA Agent Architecture                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐         ┌─────────────────┐                  │
│  │   Microphone │────────→│    Whisper      │                  │
│  │   Recording  │         │  STT (Speech    │                  │
│  └──────────────┘         │  to Text)       │                  │
│                           └────────┬────────┘                  │
│                                    │                            │
│  ┌────────────────────────────────▼──────────────────────────┐ │
│  │  AvaturnLLMDialogManager (Orchestrator Principal)         │ │
│  │                                                             │ │
│  │  • Gère le flux narratif (8 parties)                      │ │
│  │  • Route les interactions utilisateur                     │ │
│  │  • Contrôle les deux modes (0 et 1)                       │ │
│  └────────────────────────────────────────────────────────────┘ │
│         │                    │                    │              │
│         ▼                    ▼                    ▼              │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐ │
│  │   LLM Chat   │  │ Emotion Analyzer │  │   Facial Expr.   │ │
│  │ (Ollama API) │  │  (UserResponse   │  │  (Avatar FACS)   │ │
│  │              │  │   + Contextual)  │  │                  │ │
│  └──────┬───────┘  └────────┬─────────┘  └────────┬─────────┘ │
│         │                   │                     │             │
│         ▼                   ▼                     ▼             │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐ │
│  │  Piper TTS   │  │  Emotion Mapper  │  │  Display Action  │ │
│  │ (Text-to-   │  │  (6 emotions)    │  │  Units (AUs)     │ │
│  │  Speech)     │  │                  │  │                  │ │
│  └──────┬───────┘  └──────────────────┘  └────────┬─────────┘ │
│         │                                         │             │
│         └─────────────────────┬───────────────────┘             │
│                               │                                 │
│                               ▼                                 │
│                      ┌──────────────────┐                       │
│                      │   Audio Output   │                       │
│                      │  + Avatar Update │                       │
│                      └──────────────────┘                       │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Composants principaux

### 1. **AvaturnLLMDialogManager.cs** (Orchestrator)

**Rôle**: Gestionnaire principal orchestrant l'ensemble du flux.

**Responsabilités principales**:
- Gestion du cycle d'histoire (8 parties)
- Routage des interactions utilisateur (microphone, dictation)
- Contrôle des deux modes d'interaction (`storyInteractionMode`)
- Communication avec le LLM via API
- Gestion de la synthèse vocale (Piper TTS)

**Propriétés clés**:
```csharp
public int storyInteractionMode = 0;  // 0 = Auto puis Interactif, 1 = Interactif puis Auto
public string urlOllama;               // URL du serveur (http://localhost:11434/)
public string modelName;               // Modèle LLM (ex: "mistral")
public bool usePiper = true;           // Activer TTS
public bool useWhisper = true;         // Activer STT
public float speakerID = 1;            // Voix Piper (1-7)
```

**Méthodes critiques**:
- `CreateLucasStory()` - Crée les 8 parties de l'histoire Thomas
- `StartStory()` - Démarre la narration
- `TellCurrentStoryPart()` - Raconte la partie courante
- `NextStoryPart()` - Avance à la partie suivante avec analyse émotionnelle
- `IsCurrentPartInteractive()` - Détermine le type de partie

---

### 2. **ContextualEmotionMapper.cs** (Analyseur contextuel)

**Rôle**: Analyse les réponses utilisateur dans le contexte de chaque partie.

**Caractéristiques**:
- Analyse contextuelle spécifique à chaque partie de l'histoire
- Mappe les réponses aux 6 émotions primaires avec confiance
- Génère des explications sur les choix émotionnels

**Flux**:
```
Réponse utilisateur
        ↓
MapUserResponseToEmotion()
  • UserResponseAnalyzer (analyse basique)
  • EnrichissementContextuel (spécifique à la partie)
  • MapPartX() (8 méthodes)
        ↓
ContextualEmotionData {émotion, confiance, raison, tag}
```

**6 émotions primaires mappées**:
- **JOY** - Joie, enthousiasme, optimisme
- **SAD** - Tristesse, mélancolie, résignation
- **ANGER** - Colère, frustration, indignation
- **FEAR** - Peur, inquiétude, appréhension
- **SURPRISE** - Surprise, étonnement, curiosité
- **DISGUST** - Dégoût, aversion, répulsion

---

### 3. **UserResponseAnalyzer.cs** (Analyseur basique)

**Rôle**: Premier niveau d'analyse des réponses utilisateur (fallback).

**Approche**:
- Extraction de mots-clés émotionnels français (150+ mots)
- Calcul de dimensions émotionnelles:
  - **Valence**: -1 (négatif) à +1 (positif)
  - **Arousal**: -1 (calme) à +1 (activé)
  - **Intensité**: 0 (faible) à 1 (très intense)
- Modèle circumplex pour prédiction d'émotion

**Clé de prédiction** (domaines circumplex):
```
        Positif
           ↑
    Q1     │     Q2
   Joy  Surprise
           │
Calme  ----+---- Activé
           │
  Sadness │ Anger
    Q3     │     Q4
           ↓
        Négatif
```

---

### 4. **EmotionalState.cs** (Gestionnaire d'état)

**Rôle**: Gère l'état émotionnel actuel du système.

**Propriétés**:
- État émotionnel courant
- Dimensions émotionnelles (valence, arousal, intensité)
- Historique des émotions
- Événements de changement émotionnel

**Utilité**:
- Transitions douces entre émotions
- Décroissance naturelle de l'intensité
- Enregistrement historique pour adaptation future

---

### 5. **FacialExpressionAvaturn.cs** (Expressions faciales)

**Rôle**: Applique les expressions faciales à l'avatar 3D.

**Système d'unités d'action (Action Units - AU)**:
- Basé sur le Facial Action Coding System (FACS)
- Combine multiple AUs pour créer expressions complexes

**Mappages émotions → AUs**:
```
SADNESS:    AU 4 (Baissement sourcils) + AU 15, 17 (Baissement bouche)
JOY:        AU 6 (Sourcil levé) + AU 12 (Sourire)
ANGER:      AU 4 + AU 7 (Resserrement yeux) + AU 23 (Serrement lèvres)
FEAR:       AU 1 (Levé sourcil interne) + AU 5 (Ouverture yeux)
SURPRISE:   AU 1 + AU 2 (Levé sourcil) + AU 26 (Ouverture bouche)
DISGUST:    AU 9 (Levé nez) + AU 16, 17 (Baissement commande)
```

**Paramètres**:
```csharp
public float speechSpeed = 2.5f;           // Vitesse narration
public float emotionOverlap = 0.85f;       // Chevauchement émotions
public float globalEmotionIntensity = 1.0f; // Scaling intensité
```

---

### 6. **EmotionalKeywords.cs** (Dictionnaire)

**Contient**: 150+ mots-clés français par émotion.

**Structure**:
```csharp
public static List<string> positiveKeywords = new() { "heureux", "joyeux", ... };
public static List<string> negativeKeywords = new() { "triste", "déprimé", ... };
public static List<string> angerKeywords = new() { "colère", "furieux", ... };
// ... Fear, Surprise, Disgust
```

---

## Système d'histoire

### Structure de l'histoire Thomas

**L'histoire en 8 parties** - Thomas Rivière, journaliste, à Paris (8 avril):

| Partie | Titre | Contexte | Question |
|--------|-------|----------|----------|
| 0 | Le Réveil à Oberkampf | 7h10 - Réveil, préparation | Intrigué par l'enregistreur? |
| 1 | Trajet vers Bastille | Métro, collègue mystérieux | Que ressens-tu? |
| 2 | Découverte du bâtiment | 242 rue du Faubourg | Peurs ou curiosité? |
| 3 | Exploration intérieure | Escaliers, documents | Confiance ou doute? |
| 4 | La cavité souterraine | Descente, archives | Que décides-tu? |
| 5 | Le symbole récurrent | Symbole ancien découvert | Signification personnelle? |
| 6 | Les documents secrets | 1950-2003, noms reconnaissables | Révélations? |
| 7 | Conclusion et importance | 11h00, ancien rédacteur en chef | Réflexions finales? |

### Mode d'interaction (storyInteractionMode)

#### Mode 0 (par défaut)
```
Parties 0-3 (Exposition)  →  AUTOMATIQUES, sans questions
↓
Parties 4-7 (Climax)      →  INTERACTIVES, avec questions et analyse émotionnelle
↓
Résultat: Narration d'introduction libre, puis engagement progressif
```

#### Mode 1 (alternatif)
```
Parties 0-3 (Exposition)  →  INTERACTIVES, avec questions et analyse émotionnelle
↓
Parties 4-7 (Climax)      →  AUTOMATIQUES, sans questions
↓
Résultat: Engagement immédiat, puis déploiement narratif continu
```

### Logique d'avancement

**Parties interactives**:
1. Agent pose la question
2. Utilisateur répond (microphone/clavier)
3. Analyse émotionnelle contextuelle
4. Expression faciale correspondante
5. Avancement à la partie suivante

**Parties automatiques**:
1. Agent raconte sans question
2. Après fin de la narration (audio)
3. Avancement automatique à la partie suivante
4. Pas d'interaction utilisateur acceptée

---

## Système émotionnel

### 6 Émotions Primaires

Le système utilise **UNIQUEMENT** ces 6 émotions (pas de secondaires):

| Émotion | Tag | Contexte | Facette |
|---------|-----|----------|---------|
| **JOY** | {JOY} | Bonheur, plaisir, satisfaction | Positive + Activée |
| **SAD** | {SAD} | Tristesse, mélancolie, perte | Négative + Calme |
| **ANGER** | {ANGER} | Colère, frustration, irritation | Négative + Activée |
| **FEAR** | {FEAR} | Peur, anxiété, inquiétude | Négative + Activée |
| **SURPRISE** | {SURPRISE} | Étonnement, découverte, curiosité | Neutre + Activée |
| **DISGUST** | {DISGUST} | Dégoût, aversion, répulsion | Négative + Calme |

### Cycle d'analyse émotionnelle

```
Utilisateur répond: "Je me sens un peu perdu"
        ↓
UserResponseAnalyzer:
  • Détecte mots-clés: "perdu" → Arousal basse, Valence négative
  • Prédit: Fear/Surprise/Sadness possible
        ↓
ContextualEmotionMapper:
  • Contextualise par rapport à la partie 2 (Trajet)
  • Enrichit avec règles métier
  • Choisit: FEAR (plus adapté au contexte de trajet mystérieux)
  • Confiance: 0.78
        ↓
Émotion finale: FEAR
        ↓
DisplayEmotionOnFace(Emotion.Fear):
  • AU 1, 5: Levé sourcil, ouverture yeux
  • Duration: 1.5s
        ↓
FacialExpressionAvaturn:
  • Applique les blendshapes
  • Expression faciale de peur sur avatar
```

### Fallback émotionnel

Si aucune émotion détectée: **SURPRISE** (emotion neutre/curiosité).

---

## Modes d'interaction

### Mode Whisper (Reconnaissance vocale)

**Flux**:
```
1. Utilisateur appuie sur bouton "Record"
2. MicrophoneRecord capture audio
3. Whisper envoie à API locale/distante
4. Retour transcription texte
5. OnRecordStop() traite la réponse
6. NextStoryPart() si interactive
```

**Configuration**:
- `useWhisper = true` - Activer cette méthode
- Se branche automatiquement si dictation non disponible

### Mode Dictation (Windows)

**Flux**:
```
1. DictationRecognizer gère la reconnaissance
2. DictationRecognizer_DictationResult() intercepte
3. DictationRecognizer_DictationComplete() finalise
4. NextStoryPart() si interactive
```

**Configuration**:
- `useWhisper = false` - Utiliser la dictation Windows
- Disponible sur Windows uniquement

### Mode Chat (Conversation libre)

Quand `isStoryMode = false`:
- Utilisateur peut converser librement
- `SendToChat()` route vers conversation normale
- LLM répond en mode assistant
- Pas d'analyse émotionnelle contextuelle

---

## Flux d'exécution

### 1. Démarrage (Start())

```csharp
void Start()
{
    // Initialisation UI
    anim = GetComponent<Animator>();
    InformationDisplay("");
    
    // Bouton de dictation
    button = Instantiate(ButtonPrefab);
    button.GetComponent<Button>().onClick.AddListener(OnButtonPressed);
    
    // Création et démarrage histoire
    CreateLucasStory();   // 8 parties
    StartStory();         // isStoryMode = true
                          // currentStoryPart = 0
                          // TellCurrentStoryPart() appelé
}
```

### 2. Narration d'une partie (TellCurrentStoryPart())

```csharp
private void TellCurrentStoryPart()
{
    // Sécurité: histoire terminée?
    if (!isStoryMode || currentStoryPart >= lucasStory.Count)
        return;
    
    // Déterminer si interactive
    bool isInteractive = IsCurrentPartInteractive();
    
    // Construire message pour LLM
    string storyMessage = isInteractive 
        ? "Lis [TEXTE]... Puis pose [QUESTION]..."
        : "Lis [TEXTE]... NE pose PAS de question...";
    
    // Envoyer au LLM
    SendChatForStory(storyConversation);
}
```

### 3. Réception réponse utilisateur (OnRecordStop/DictationResult)

```csharp
private async void OnRecordStop(AudioChunk audioChunk)
{
    // Par défaut ne rien faire si histoire terminée
    if (!isStoryMode) return;
    
    // Transcrire audio
    string text = await whisper.GetTextAsync(...);
    
    // Si partie interactive: traiter réponse
    if (isStoryMode && IsCurrentPartInteractive())
    {
        lastUserResponse = text;
        NextStoryPart();  // Analyse émotionnelle + avance
    }
    // Si partie auto: ignorer
}
```

### 4. Transitions (NextStoryPart())

```csharp
public void NextStoryPart()
{
    // Pour parties interactives: analyser émotion
    if (wasInteractive && lastUserResponse != "")
    {
        emotionData = emotionMapper.MapUserResponseToEmotion(
            lastUserResponse,
            currentStoryPart,
            out emotionDetected
        );
        DisplayEmotionOnFace(emotionDetected);
    }
    
    // Avancer
    currentStoryPart++;
    
    // Fin?
    if (currentStoryPart >= lucasStory.Count)
    {
        isStoryMode = false;
        // Désactiver microphone
        if (useWhisper)
            microphoneRecord.StopRecord();
        return;
    }
    
    // Partie suivante
    TellCurrentStoryPart();
}
```

### 5. Parties automatiques (AutoAdvanceStoryPartAfterAudio)

```csharp
private IEnumerator AutoAdvanceStoryPartAfterAudio()
{
    // Attendre démarrage audio
    yield return new WaitForSeconds(0.2f);
    
    // Attendre fin audio
    while (audioSource.isPlaying)
        yield return new WaitForSeconds(0.1f);
    
    // Avancer sans interaction
    NextStoryPart();
}
```

---

## Configuration

### Dans l'inspecteur Unity

#### Dialog Settings
```
URL Ollama:              http://localhost:11434/
Endpoint:                Ollama (ou OpenWebUI)
Model Name:              mistral (ou autre)
API Key:                 [si OpenWebUI]
Number of Turns:         50
```

#### Preprompt (System instruction)
```
Tu es Thomas, un journaliste menant une enquête mystérieuse.
Tu dois raconter l'histoire de manière immersive et captivante.
Adapte ton ton à l'émotion détectée de l'utilisateur.
Lis EXACTEMENT le texte fourni sans modification.
```

#### Voice Settings
```
Use Piper:               true
Piper Port:              5000
Speaker ID:              1-7 (voix différentes)
Speech Speed:            2.5
```

#### Emotion Settings
```
Emotion Overlap:         0.85
Global Emotion Intensity: 1.0
Attack Ratio:            0.25
Decay Exponent:          3.0
```

#### Story Settings
```
Story Interaction Mode:  0 ou 1 (voir Modes ci-dessus)
Use Whisper:             true
Use Piper:               true
```

---

## Dépendances externes

### Services requis

#### 1. **Ollama (LLM)**
```bash
# Installation
https://ollama.ai/download

# Lancer
ollama serve

# Modèle utilisé
ollama pull mistral  # (ou altro as desired)
```

**Endpoint**: `http://localhost:11434/api/chat`

#### 2. **Piper TTS (Synthèse vocale)**
```bash
# Installation
pip install piper-tts

# Lancer server
piper-server --cuda_device 0
```

**Port**: 5000 (configurable)

#### 3. **Whisper (Reconnaissance vocale)**

Intégré via plugin Unity ou API OpenAI:
```
OpenAI Whisper API: https://api.openai.com/v1/audio/transcriptions
Ou: Locale (whisper CLI)
```

### Pipelines

**Preprocessing**:
```
Audio Input → Whisper → Text (UTF-8)
```

**Processing**:
```
User Text → Analyzer → Emotion → LLM → Response Text
```

**Postprocessing**:
```
LLM Response Text → Piper → Audio File → Play
Audio + Emotion → FacialExpressionAvaturn → AUs → Avatar
```

---

## Guide d'utilisation

### Démarrage rapide

1. **Préparer les services**:
   ```bash
   # Terminal 1: Ollama
   ollama serve
   
   # Terminal 2: Piper
   piper-server
   ```

2. **Configurer dans Unity**:
   - URL Ollama: `http://localhost:11434/`
   - Model: `mistral`
   - Speaker: `1`

3. **Lancer la scène**:
   - Play dans éditeur Unity
   - Agent démarre histoire automatiquement

4. **Interagir**:
   - Cliquer bouton "Record" pour répondre
   - Parler dans le microphone
   - Résultats en console (logs)

### Paramètres clés à ajuster

| Paramètre | Valeurs | Effet |
|-----------|---------|-------|
| `storyInteractionMode` | 0 ou 1 | Organisation parties interactives |
| `speakerID` | 1-7 | Voix différentes (Piper) |
| `speechSpeed` | 1.0-3.0 | Vitesse du récit |
| `globalEmotionIntensity` | 0.5-2.0 | Force expressions faciales |
| `emotionOverlap` | 0.0-1.0 | Chevauchement émotions |

### Débogage

**Logs importants**:
```
📖 DÉMARRAGE DE L'HISTOIRE: Histoire Lucas (Mode X)
📕 RACONTE PARTIE X/8: [Titre]
📝 RÉPONSE UTILISATEUR: '[Texte]'
😊 ANALYSE ÉMOTIONNELLE PARTIE X: Émotion=Y, Confiance=Z
🎭 EXPRESSION FACIALE: Affichage de l'émotion X
⏱️ Partie non-interactive: avancement automatique
🏁 HISTOIRE TERMINÉE!
```

**Console Unity**:
- Activer "Collapse" pour grouper logs
- Filtrer par "[Story]" ou "[Emotion]" tags
- Monitoring fps et mémoire

---

## Cas d'usage

### Scénario Mode 0 (Auto + Interactif)
**Public**: Utilisateurs moins expérimentés
- Parties 0-3: Exposition narrative sans demander
- Parties 4-7: Engagement progressif

**Flux**:
1. Agent raconte parties 0-3 d'une traite
2. Puis pose questions et attend réponses (parties 4-7)
3. Adaptation émotionnelle basée sur réponses

### Scénario Mode 1 (Interactif + Auto)
**Public**: Utilisateurs engagés dès le départ
- Parties 0-3: Questions et dialogue
- Parties 4-7: Déploiement narratif continu

**Flux**:
1. Agent pose questions (parties 0-3)
2. Analyse réponses et s'adapte
3. Puis raconte climax (parties 4-7) dynamiquement

---

## Limitations et considérations

### Actuelles
- LLM doit respecter directives (risque de dérive)
- Whisper nécessite audio clair
- Avatar FACS limité à ~20 AUs disponibles
- Latence réseau Ollama (peut être 2-5sec)

### Futures améliorations possibles
- Fine-tuning LLM spécifique à narration
- Support multi-langue
- Intégration dialogue non-verbal (gestes)
- Persistance historique utilisateur
- Branching narratif personnalisé

---

## Troubleshooting

### Le LLM ne répond pas
```
✓ Vérifier Ollama tourne: http://localhost:11434/
✓ Vérifier modèle installé: ollama list
✓ Vérifier firewall/réseau
✓ Logger réponse API: voir ChatRequest() logs
```

### Whisper n'entend pas
```
✓ Vérifier microphone en input
✓ Vérifier volume
✓ Test dictation Windows si Whisper off
✓ Vérifier logs: "📝 RÉPONSE UTILISATEUR"
```

### Avatar ne fait pas d'expression
```
✓ Vérifier FacialExpressionAvaturn assigné en Inspector
✓ Vérifier AUs válidas (AU 1, 2, 4, 5, 6, etc.)
✓ Vérifier intensité > 0
✓ Logs: "🎭 DisplayEmotionOnFace"
```

### Histoire ne s'arrête pas en Mode 1
```
✓ Vérifier isStoryMode = false en fin
✓ Vérifier currentStoryPart >= 8
✓ Vérifier microphone désactivé
✓ Logs: "🏁 HISTOIRE TERMINÉE!"
```

---

## Ressources

- [Ollama Documentation](https://ollama.ai/)
- [Piper TTS GitHub](https://github.com/rhasspy/piper)
- [OpenAI Whisper](https://github.com/openai/whisper)
- [Facial Action Coding System (FACS)](https://en.wikipedia.org/wiki/Facial_Action_Coding_System)
- [Circumplex Model of Emotion](https://en.wikipedia.org/wiki/Circumplex_model_of_emotion)

---

## Auteur et Crédits

Développé comme solution **multi-modale de narration adaptive** utilisant:
- Unity Engine
- Ollama/OpenWebUI (LLM)
- Piper TTS (Synthèse vocale)
- OpenAI Whisper (Reconnaissance vocale)
- System FACS (Expressions faciales)

---

## Licence

[À définir selon votre politique]

---

**Version**: 1.0  
**Dernière mise à jour**: Mars 2026  
**Statut**: Stable avec Mode 0 et Mode 1 fonctionnels
