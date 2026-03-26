using System.Collections.Generic;
using UnityEngine;
using Emotion = Assets.Scripts.Emotions.EmotionalState.Emotion;

namespace Assets.Scripts.Emotions
{
    /// <summary>
    /// UserResponseAnalyzer: Analyse les réponses textuelles de l'utilisateur 
    /// pour extraire des dimensions émotionnelles (valence, arousal, intensité).
    /// 
    /// Utilise EmotionalKeywords pour reconnaissance de mots-clés français.
    /// 
    /// Exemple:
    /// ```csharp
    /// var analyzer = new UserResponseAnalyzer();
    /// var result = analyzer.AnalyzeUserResponse("Je suis très heureux et enthousiaste!");
    /// Debug.Log($"Valence: {result.dominantValence}, Arousal: {result.dominantArousal}");
    /// ```
    /// </summary>
    public class UserResponseAnalyzer
    {
        /// <summary>
        /// Résultat d'analyse avec toutes les dimensions émotionnelles extraites
        /// </summary>
        public struct AnalysisResult
        {
            public float dominantValence;      // -1 (très négatif) à +1 (très positif)
            public float dominantArousal;      // -1 (calme) à +1 (activé/énergique)
            public float estimatedIntensity;   // 0 (faible) à 1 (très intense)
            public List<string> matchedKeywords;
            public Emotion predictedEmotion;   // Émotion prédominante (prévu pour EmotionMapper)
            public int wordCount;
            public bool containsIntensifiers;
            public string rawText;

            public override string ToString()
            {
                return $"[Analysis] Valence: {dominantValence:F2} | Arousal: {dominantArousal:F2} | " +
                       $"Intensity: {estimatedIntensity:F2} | Emotion: {predictedEmotion} | " +
                       $"Keywords: {matchedKeywords.Count}";
            }
        }

        private List<string> allKeywordLists = new List<string>();

        public UserResponseAnalyzer()
        {
            // Initialiser la liste combinée de tous les mots-clés
            BuildKeywordIndex();
        }

        /// <summary>
        /// Construit un index des mots-clés pour recherche rapide
        /// </summary>
        private void BuildKeywordIndex()
        {
            allKeywordLists.Clear();
            allKeywordLists.AddRange(EmotionalKeywords.positiveKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.negativeKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.angerKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.fearKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.surpriseKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.disgustKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.interestKeywords);
            allKeywordLists.AddRange(EmotionalKeywords.boredomKeywords);
        }

        /// <summary>
        /// Analyse le texte d'une réponse utilisateur et retourne les dimensions émotionnelles
        /// </summary>
        public AnalysisResult AnalyzeUserResponse(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
                return CreateEmptyResult();

            var result = new AnalysisResult
            {
                rawText = userText,
                matchedKeywords = new List<string>()
            };

            // Tokeniser et nettoyer le texte
            string[] words = TokenizeAndClean(userText);
            result.wordCount = words.Length;

            // Extraire les mots-clés émotionnels
            ExtractKeywords(words, result.matchedKeywords);

            // Si pas de mots-clés trouvés, utiliser analyse par contenu
            if (result.matchedKeywords.Count == 0)
            {
                AnalyzeByContent(userText, result);
                return result;
            }

            // Calculer les dimensions émotionnelles
            result.dominantValence = EmotionalKeywords.GetValenceFromKeywords(result.matchedKeywords);
            result.dominantArousal = EmotionalKeywords.GetArousalFromKeywords(result.matchedKeywords);

            // Détecter les intensificateurs (très, extrêmement, etc.)
            result.containsIntensifiers = ContainsIntensifier(words);

            // Calculer l'intensité basée sur: nombre de mots-clés, intensificateurs, ponctuation
            result.estimatedIntensity = CalculateIntensity(
                result.matchedKeywords.Count,
                result.wordCount,
                result.containsIntensifiers,
                userText
            );

            // Prédire l'émotion primaire basée sur les dimensions
            result.predictedEmotion = PredictPrimaryEmotion(result.dominantValence, result.dominantArousal);

            return result;
        }

        /// <summary>
        /// Extrait les mots-clés émotionnels du texte
        /// </summary>
        private void ExtractKeywords(string[] words, List<string> matchedKeywords)
        {
            foreach (var word in words)
            {
                string cleaned = word.ToLower();
                cleaned = cleaned.TrimPunctuation();

                if (EmotionalKeywords.positiveKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.negativeKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.angerKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.fearKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.surpriseKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.disgustKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.interestKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
                else if (EmotionalKeywords.boredomKeywords.Contains(cleaned))
                    matchedKeywords.Add(cleaned);
            }
        }

        /// <summary>
        /// Analyse le contenu quand peu/pas de mots-clés sont trouvés
        /// (utilise présence de questions, ponctuation, etc.)
        /// </summary>
        private void AnalyzeByContent(string text, AnalysisResult result)
        {
            // Si la réponse contient beaucoup de points d'interrogation → surpris
            int questionMarks = text.Split('?').Length - 1;
            if (questionMarks > 1)
            {
                result.dominantValence = 0f;
                result.dominantArousal = 0.3f;
                result.estimatedIntensity = 0.4f;
                result.predictedEmotion = Emotion.Surprise;
                return;
            }

            // Si beaucoup de points d'exclamation → excité ou énervé
            int exclamations = text.Split('!').Length - 1;
            if (exclamations > 1)
            {
                result.dominantArousal = 0.6f;
                result.estimatedIntensity = 0.6f;
                // Besoin de contexte pour valence
                result.dominantValence = 0.5f;
                result.predictedEmotion = Emotion.Joy;
                return;
            }

            // Si points de suspension → dégoûté ou mystérieux
            if (text.Contains("..."))
            {
                result.dominantArousal = -0.3f;
                result.estimatedIntensity = 0.3f;
                result.dominantValence = 0f;
                result.predictedEmotion = Emotion.Disgust;
                return;
            }

            // Par défaut: surprise
            result.dominantValence = 0f;
            result.dominantArousal = 0f;
            result.estimatedIntensity = 0.2f;
            result.predictedEmotion = Emotion.Surprise;
        }

        /// <summary>
        /// Détecte la présence de mots intensificateurs (très, extrêmement, etc.)
        /// </summary>
        private bool ContainsIntensifier(string[] words)
        {
            foreach (var word in words)
            {
                string cleaned = word.ToLower().TrimPunctuation();
                if (EmotionalKeywords.intensityModifiers.Contains(cleaned))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Calcule un score d'intensité global (0 à 1)
        /// Basé sur: nombre de mots-clés, longueur du texte, intensificateurs, ponctuation
        /// </summary>
        private float CalculateIntensity(int keywordCount, int wordCount, bool hasIntensifiers, string text)
        {
            float intensity = 0f;

            // Facteur 1: densité de mots-clés (0-0.4)
            float keywordDensity = (float)keywordCount / Mathf.Max(wordCount, 1);
            intensity += Mathf.Clamp01(keywordDensity) * 0.4f;

            // Facteur 2: intensificateurs (0-0.2)
            if (hasIntensifiers)
                intensity += 0.2f;

            // Facteur 3: ponctuation expressive (0-0.3)
            int exclamations = text.Split('!').Length - 1;
            int questions = text.Split('?').Length - 1;
            float punctuationScore = (exclamations * 0.05f + questions * 0.03f);
            intensity += Mathf.Clamp01(punctuationScore) * 0.3f;

            // Facteur 4: longueur du texte (bonus pour texte long = plus émotionnel) (0-0.1)
            if (wordCount > 20)
                intensity += 0.1f;

            return Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Prédit l'émotion primaire basée sur valence et arousal
        /// Utilise approche simple de quadrants circulaires
        /// </summary>
        private Emotion PredictPrimaryEmotion(float valence, float arousal)
        {
            // Quadrants du modèle circumplex:
            // Q1: Valence+, Arousal+ → Joy, Surprise
            // Q2: Valence-, Arousal+ → Anger, Fear
            // Q3: Valence-, Arousal- → Sadness, Disgust
            // Q4: Valence+, Arousal- → Boredom (ou contentement calme)

            bool positiveValence = valence > 0.1f;
            bool highArousal = arousal > 0.1f;

            // Centre du graphique → surprise (fallback)
            if (Mathf.Abs(valence) < 0.2f && Mathf.Abs(arousal) < 0.2f)
                return Emotion.Surprise;

            // Quadrant 1: Positif + Très activé
            if (positiveValence && highArousal && arousal > 0.4f)
                return Emotion.Joy;

            // Quadrant 1: Positif + Modérément activé
            if (positiveValence && highArousal && arousal <= 0.4f)
                return Emotion.Surprise;

            // Quadrant 2: Négatif + Très activé + High arousal
            if (!positiveValence && highArousal && arousal > 0.5f)
            {
                // Discrimination: Colère (valence plus négative) vs Peur (arousal très haut)
                return arousal > valence + 0.5f ? Emotion.Fear : Emotion.Anger;
            }

            // Quadrant 2: Négatif + Modérément activé
            if (!positiveValence && highArousal)
                return arousal > 0.3f ? Emotion.Anger : Emotion.Anger;

            // Quadrant 3: Négatif + Peu activé
            if (!positiveValence && !highArousal)
            {
                // Sadness si clairement négatif
                if (valence < -0.3f)
                    return Emotion.Sadness;
                // Disgust si légèrement négatif + très calme
                if (arousal < -0.4f)
                    return Emotion.Disgust;
                // Disgust si très calme
                return Emotion.Disgust;
            }

            // Quadrant 4: Positif + Peu activé
            if (positiveValence && !highArousal)
                return Emotion.Joy; // Contentement tranquille

            // Par défaut
            return Emotion.Surprise;
        }

        /// <summary>
        /// Tokenise et nettoie un texte en mots
        /// </summary>
        private string[] TokenizeAndClean(string text)
        {
            // Remplacer la ponctuation par des espaces
            char[] separators = new[] { ' ', ',', '.', '!', '?', '\'', '"', ':', ';', '(', ')' };
            return text.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Résultat vide (pour texte vide ou null)
        /// </summary>
        private AnalysisResult CreateEmptyResult()
        {
            return new AnalysisResult
            {
                dominantValence = 0f,
                dominantArousal = 0f,
                estimatedIntensity = 0f,
                matchedKeywords = new List<string>(),
                predictedEmotion = Emotion.Surprise,
                wordCount = 0,
                containsIntensifiers = false,
                rawText = ""
            };
        }

        /// <summary>
        /// Interprète les résultats en langage naturel (pour debug)
        /// </summary>
        public string InterpretResults(AnalysisResult result)
        {
            string interpretation = "";

            if (result.dominantValence > 0.6f)
                interpretation += "Très positif ";
            else if (result.dominantValence > 0.3f)
                interpretation += "Positif ";
            else if (result.dominantValence < -0.6f)
                interpretation += "Très négatif ";
            else if (result.dominantValence < -0.3f)
                interpretation += "Négatif ";
            else
                interpretation += "Neutre ";

            if (result.dominantArousal > 0.5f)
                interpretation += "et très énergique";
            else if (result.dominantArousal > 0.2f)
                interpretation += "et un peu énergique";
            else if (result.dominantArousal < -0.5f)
                interpretation += "et très calme";
            else if (result.dominantArousal < -0.2f)
                interpretation += "et un peu calme";
            else
                interpretation += "et neutre";

            return interpretation;
        }
    }

    /// <summary>
    /// Méthodes d'extension pour string
    /// </summary>
    public static class StringExtensions
    {
        public static string TrimPunctuation(this string str)
        {
            return str.Trim(new[] { '.', ',', '!', '?', '\'', '"', ':', ';', '(', ')', '-' });
        }
    }
}
