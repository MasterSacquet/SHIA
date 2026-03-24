# Système de Narration Adaptive avec Analyse Émotionnelle

## Vue d'ensemble

Le système fonctionne selon ce cycle:

```
1. Démarrage Unity
   ↓
2. L'agent raconte Partie 1 (neutre)
   ↓
3. Agent pose la question à la fin
   ↓
4. Utilisateur répond (par voix ou texte)
   ↓
5. UserResponseAnalyzer calcule le score émotionnel
   ↓
6. Score converti en Émotion (Joy, Sadness, Anger, etc.)
   ↓
7. Agent continue Partie 2 avec tag émotionnel {JOY}
   ↓
8. FacialExpressionAvaturn applique l'expression faciale
   ↓
9. Retour à l'étape 3 pour la partie suivante
```

## Fichiers concernés

### 1. **AvaturnLLMDialogManager.cs**
- **Méthode `CreateLucasStory()`**: Définit les 9 parties de l'histoire
- **Méthode `TellCurrentStoryPart()`**: Raconte la partie actuelle
- **Méthode `NextStoryPart()`**: Passe à la partie suivante après analyse émotionnelle
- **Intégration**: Reçoit la réponse utilisateur → appelle `UserResponseAnalyzer`

### 2. **UserResponseAnalyzer.cs**
- **Méthode `AnalyzeUserResponse(string userResponse)`**:
  - Extrait les mots-clés émotionnels français
  - Calcule une valence (-1 à +1)
  - Calcule une arousal (-1 à +1)
  - Prédit l'émotion primaire via le modèle circumplex
  - Retourne un `AnalysisResult` avec l'émotion détectée

### 3. **EmotionalState.cs**
- Gère les 10 émotions: Joy, Sadness, Anger, Fear, Surprise, Disgust, Interest, Boredom, Frustration, Neutral
- Associe chaque émotion à un tag: `{JOY}`, `{SAD}`, `{ANGER}`, etc.

### 4. **EmotionalKeywords.cs**
- Dictionnaire de 150+ mots-clés français par émotion
- Utilisé par `UserResponseAnalyzer` pour identifier les sentiments

### 5. **FacialExpressionAvaturn.cs**
- Reçoit le tag émotionnel (ex: `{JOY}`)
- Applique les blendshapes correspondants
- Produit l'expression faciale du personnage

## Flux complet

### Initialisation
```csharp
// Au démarrage de la scène (AvaturnLLMDialogManager.Start())
CreateLucasStory();        // Charge les 9 parties
StartStory();              // Lance la Partie 1
TellCurrentStoryPart();    // Envoie au LLM
```

### Après la réponse de l'utilisateur
```
1. L'utilisateur répond à la question (ex: "je m'ennuie")
2. UserResponseAnalyzer.AnalyzeUserResponse(userInput)
   - Détecte: "ennui" → Emotion.Boredom
   - Retourne: AnalysisResult { predictedEmotion = Boredom, ... }
3. AvaturnLLMDialogManager reçoit le résultat émotionnel
4. Appelle ProcessAffectiveContent() avec le tag {BOREDOM}
5. FacialExpressionAvaturn applique l'expression triste/ennuyée
6. Appelle NextStoryPart() pour passer à la partie suivante
```

## Configuration requise

### Avant de lancer Unity:

1. **Serveur Ollama** doit tourner
   ```
   ollama serve
   ```
   (Le modèle doit être disponible sur `http://localhost:11434/`)

2. **Piper TTS** doit être actif pour la synthèse vocale

3. **Whisper** (optionnel) pour la reconnaissance vocale

### Dans l'Inspector d'AvaturnLLMDialogManager:

- **Preprompt**: Instruction système pour le LLM (voir exemple ci-dessous)
- **EndPoint**: URL du serveur Ollama (ex: `http://localhost:11434/`)
- **usePiper**: `true` pour activer la synthèse vocale
- **useWhisper**: `true` pour la reconnaissance vocale

## Exemple de Preprompt

```
Tu es un narrateur empathique qui raconte l'histoire de Lucas Garnier avec émotion et humain. 

INSTRUCTIONS CRITIQUES:
1. Quand tu reçois une partie de l'histoire (marquée [Partie X/9: ...]), tu DOIS:
   - Raconter EXACTEMENT le texte fourni
   - OBLIGATOIREMENT terminer par la question fournie
   - Ne pas ajouter de questions supplémentaires
   - Poser la question EXACTEMENT comme elle est écrite

2. Si la section "APRÈS AVOIR RACONTÉ..." arrive, tu DOIS poser la question mentionnée, sans exceptions

3. À partir de la Partie 2, ajoute un tag émotionnel au début basé sur la réponse précédente:
   - {JOY} pour la joie, contentement
   - {SAD} pour la tristesse, mélancolie
   - {ANGER} pour la colère, frustration
   - {FEAR} pour la peur, anxiété
   - {SURPRISE} pour la surprise, étonnement
   - {DISGUST} pour le dégoût, répugnance

4. Adapte ton ton et ton style selon l'émotion du tag

5. CHAQUE RÉPONSE DOIT SE TERMINER PAR LA QUESTION POSÉE. C'EST NON-NÉGOCIABLE.
```

## Les 9 parties de Lucas Garnier

### Partie 1 - Le Réveil
**Narration**: Le 5 novembre 2019, à 7h28, Lucas Garnier, étudiant de 24 ans, se réveille dans son appartement situé au 3 rue des Lilas à Lille. Il porte un pull vert foncé et une écharpe rouge. Sa sœur Camille Garnier, 19 ans, est hospitalisée depuis le 28 octobre à l'hôpital Saint-Vincent-de-Paul, situé à 2,5 km de chez lui.

**Question**: Que penses-tu de cette histoire?

### Partie 2 - Le Trajet
**Narration**: Lucas quitte son appartement à 8h05. Il prend le métro ligne 1 à la station Wazemmes à 8h12, direction CHU Eurasanté, et arrive à 8h26. À l'hôpital, il se rend au bâtiment B, chambre 214.

**Question**: Que penses-tu de ma manière de parler?

### Partie 3 - À l'Hôpital
**Narration**: Une infirmière nommée Nadia Lefèvre, âgée de 38 ans, lui indique que l'état de Camille est stable. Lucas entre dans la chambre à 8h40. Ils discutent et évoquent un séjour en 2008 à Arcachon, dans une maison blanche aux volets bleus, avec leur chien Milo.

**Question**: Qu'en penses-tu?

### Partie 4 - Résultats Médicaux
**Narration**: À 11h20, le médecin Antoine Girard, 50 ans, leur présente des résultats médicaux indiquant une évolution lente. À 11h45, Lucas quitte l'hôpital et marche jusqu'au café "Le Passage", situé à 600 mètres. Il commande un chocolat chaud à 3,20 euros.

**Question**: Comment te sens-tu?

### Partie 5 - Les Messages
**Narration**: À 11h58, Lucas reçoit un message indiquant qu'il est accepté pour un stage à Zurich, du 1er février au 31 juillet 2020, dans une entreprise appelée NeuroTech Labs. À 11h59, il reçoit un second message indiquant que sa mère Isabelle Garnier, 54 ans, arrivera avec un retard de 25 minutes sur le TGV 5210, soit à 15h43 au lieu de 15h18.

**Question**: Quel est ton ressenti?

### Partie 6 - Retrouvaille
**Narration**: Lucas retourne à l'hôpital à 12h30. À 15h10, il retrouve sa mère à son arrivée. Ils lisent ensemble un passage du livre "Le Petit Prince", à la page 47.

**Question**: Trouve-tu ce moment réconfortant?

### Partie 7 - L'Attente
**Narration**: À 16h10, Camille reçoit un plateau contenant une compote, un yaourt et un verre d'eau. À 16h25, Lucas observe une horloge murale indiquant 16h27. À 17h05, il quitte la chambre pendant quelques minutes.

**Question**: Qu'en ressens-tu?

### Partie 8 - Retour à la Maison
**Narration**: À 17h30, Lucas retrouve sa mère dans le hall. Ils prennent le bus ligne L5 à 17h52. Lucas rentre chez lui à 18h20.

**Question**: Te sens-tu fatigué?

### Partie 9 - La Soirée
**Narration**: Chez lui, Lucas trouve un cahier bleu clair contenant une liste écrite par Camille le 20 octobre, mentionnant un voyage à Barcelone. À 19h10, il envoie un message à 6 amis. À 21h45, il regarde 12 photos de famille. Il se couche à 23h30.

**Question**: Crois-tu à l'avenir?

## Comment ça marche techniquement

1. **UserResponseAnalyzer** détecte les mots émotionnels dans la réponse
2. Le modèle circumplex (valence + arousal) prédit l'émotion
3. Example:
   - "je m'ennuie" → Valence négatif + Arousal bas → **Boredom**
   - "je suis heureux" → Valence positif + Arousal moyen → **Joy**
   - "j'ai peur" → Valence négatif + Arousal élevé → **Fear**
4. L'émotion prédite est convertie en tag (`{JOY}`, `{SAD}`, etc.)
5. Le LLM reçoit ce tag et l'insère dans sa réponse (Partie suivante)
6. FacialExpressionAvaturn parse le tag et applique les blendshapes
7. L'utilisateur voit l'expression faciale correspondante

## Prochaines étapes

1. Lancer Unity
2. Vérifier que Ollama et Piper tournent
3. Remplir le champ **Preprompt** dans l'Inspector
4. Cliquer Play
5. L'agent commence la Partie 1
6. Répondre aux questions
7. L'agent adapte son émotion en fonction de vos réponses
