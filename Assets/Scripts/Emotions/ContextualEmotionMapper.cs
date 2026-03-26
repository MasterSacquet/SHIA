using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Emotion = Assets.Scripts.Emotions.EmotionalState.Emotion;

namespace Assets.Scripts.Emotions
{
    /// <summary>
    /// ContextualEmotionMapper: Détecte les émotions de l'utilisateur EN FONCTION DU CONTEXTE
    /// de l'histoire et de la question spécifique posée.
    /// 
    /// Améliore UserResponseAnalyzer en ajoutant:
    /// - Analyse basée sur la question spécifique de chaque partie
    /// - Patterns de réponses courants
    /// - Cohérence émotionnelle avec le contexte narratif
    /// - Génération intelligente de tags émotionnels pour la réponse IA suivante
    /// 
    /// Exemple:
    /// ```csharp
    /// var mapper = new ContextualEmotionMapper();
    /// var emotionData = mapper.MapUserResponseToEmotion(
    ///     userResponse: "Je m'ennuie avec cette histoire",
    ///     questionAsked: "Que penses-tu de cette histoire?",
    ///     storyPartIndex: 0
    /// );
    /// Debug.Log($"Émotion: {emotionData.detectedEmotion}");
    /// ```
    /// </summary>
    public class ContextualEmotionMapper
    {
        /// <summary>
        /// Résultat du mappage contextuel
        /// </summary>
        public struct ContextualEmotionData
        {
            public Emotion detectedEmotion;        // Émotion primaire détectée
            public float confidence;               // Confiance (0-1)
            public string reasoning;               // Explication
            public Emotion[] suggestedNextEmotions; // Émotions pour la réponse IA
            public string emotionTag;              // Tag à utiliser ex: {JOY}, {FEAR}
        }

        // ===== QUESTIONS PAR PARTIE =====
        private static readonly string[] partQuestions = new string[]
        {
            "Es-tu intrigué par cet enregistreur laissé allumé ?",                  // Partie 0
            "Ressens-tu une certaine tension avant cette rencontre ?",              // Partie 1
            "Cette rencontre te semble-t-elle inquiétante ?",                       // Partie 2
            "Penses-tu que cet endroit cache quelque chose de suspect ?",           // Partie 3
            "Trouves-tu son choix d'entrer risqué ?",                              // Partie 4
            "Ce symbole te semble-t-il important pour la suite ?",                 // Partie 5
            "Es-tu curieux de savoir ce que contient ce fichier ?",                // Partie 6
            "Merci de m'avoir écouté."                                             // Partie 7
        };

        private UserResponseAnalyzer baseAnalyzer = new UserResponseAnalyzer();

        /// <summary>
        /// Mappe la réponse utilisateur à une émotion appropriée basée sur le contexte
        /// </summary>
        public ContextualEmotionData MapUserResponseToEmotion(
            string userResponse,
            int storyPartIndex,
            out Emotion detectedEmotion)
        {
            detectedEmotion = Emotion.Surprise;

            if (string.IsNullOrWhiteSpace(userResponse))
                return CreateNeutralResult(); // Retour par défaut: Surprise

            // Analyse de base
            var baseAnalysis = baseAnalyzer.AnalyzeUserResponse(userResponse);

            // Mapping contextuel basé sur la partie
            var contextualResult = MapByStoryContext(
                userResponse,
                baseAnalysis,
                storyPartIndex
            );

            detectedEmotion = contextualResult.detectedEmotion;
            return contextualResult;
        }

        /// <summary>
        /// Mappe l'émotion en fonction du contexte spécifique de chaque partie
        /// </summary>
        private ContextualEmotionData MapByStoryContext(
            string userResponse,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            int partIndex)
        {
            // Normaliser la réponse
            string normalized = userResponse.ToLower().Trim();
            
            // Détecter les réponses courtes/simples
            bool isShortResponse = normalized.Split(' ').Length <= 3;
            bool isNegative = normalized.Contains("non") || normalized.Contains("non") || 
                            normalized.Contains("pas") || normalized.Contains("rien");
            bool isAffirmative = normalized.Contains("oui") || normalized.Contains("oui") ||
                               normalized.Contains("bien sûr");

            // Mapper chaque partie avec sa logique specific
            return partIndex switch
            {
                0 => MapPart0_Intrigued(normalized, baseAnalysis, isAffirmative, isNegative),
                1 => MapPart1_Tension(normalized, baseAnalysis, isAffirmative, isNegative),
                2 => MapPart2_ThreatPerception(normalized, baseAnalysis, isAffirmative, isNegative),
                3 => MapPart3_Suspicion(normalized, baseAnalysis, isAffirmative, isNegative),
                4 => MapPart4_RiskAssessment(normalized, baseAnalysis, isAffirmative, isNegative),
                5 => MapPart5_ImportanceSymbol(normalized, baseAnalysis, isAffirmative, isNegative),
                6 => MapPart6_CuriosityFile(normalized, baseAnalysis, isAffirmative, isNegative),
                7 => MapPart7_Conclusion(normalized, baseAnalysis),
                _ => CreateResultFromBaseAnalysis(baseAnalysis)
            };
        }

        // ========== MAPPERS PAR PARTIE ==========

        /// <summary>
        /// PARTIE 0: "Es-tu intrigué par cet enregistreur..."
        /// Question sur curiosité face au mystère → détecte Surprise ou Fear
        /// </summary>
        private ContextualEmotionData MapPart0_Intrigued(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            // Réponse affirmative → Surprise (l'utilisateur est intriqué/surpris)
            if (isAffirmative || normalized.Contains("intrigu") || normalized.Contains("curieux") ||
                normalized.Contains("oui"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.85f,
                    reasoning = "L'utilisateur est surpris/intrigué par l'enregistreur mystérieux",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            if (isNegative || normalized.Contains("pas intéress"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Disgust,
                    confidence = 0.70f,
                    reasoning = "L'utilisateur rejette/n'est pas intrigué",
                    suggestedNextEmotions = new[] { Emotion.Disgust, Emotion.Surprise },
                    emotionTag = "{DISGUST}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 1: "Ressens-tu une certaine tension..."
        /// Question sur la tension émotionnelle → détecte Fear ou Surprise
        /// </summary>
        private ContextualEmotionData MapPart1_Tension(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            // Réponse affirmative → Fear (tension avant la rencontre)
            if (isAffirmative || normalized.Contains("tension") || normalized.Contains("nerveux") ||
                normalized.Contains("appréhens") || baseAnalysis.dominantArousal > 0.4f)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Fear,
                    confidence = 0.85f,
                    reasoning = "L'utilisateur ressent la tension avant la rencontre",
                    suggestedNextEmotions = new[] { Emotion.Fear, Emotion.Surprise },
                    emotionTag = "{FEAR}"
                };
            }

            // Réponse négative → Surprise (pas de tension, curiosité)
            if (isNegative)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.75f,
                    reasoning = "L'utilisateur reste surpris/curieux malgré l'absence de tension",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 2: "Cette rencontre te semble-t-elle inquiétante?"
        /// Question sur la menace → détecte Fear ou Surprise
        /// </summary>
        private ContextualEmotionData MapPart2_ThreatPerception(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            // Réponse affirmative → Fear (rencontre inquiétante)
            if (isAffirmative || normalized.Contains("inquiét") || normalized.Contains("suspect") ||
                normalized.Contains("danger") || baseAnalysis.dominantValence < -0.3f)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Fear,
                    confidence = 0.88f,
                    reasoning = "L'utilisateur trouve la rencontre inquiétante",
                    suggestedNextEmotions = new[] { Emotion.Fear, Emotion.Surprise },
                    emotionTag = "{FEAR}"
                };
            }

            // Réponse négative → Surprise (curiosité malgré le doute)
            if (isNegative)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.80f,
                    reasoning = "L'utilisateur n'est pas inquiet, mais reste surpris/curieux",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 3: "Penses-tu que cet endroit cache quelque chose de suspect?"
        /// Question sur suspicion → détecte Fear ou Surprise
        /// </summary>
        private ContextualEmotionData MapPart3_Suspicion(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            // Réponse affirmative → Fear (suspicion)
            if (isAffirmative || normalized.Contains("suspect") || normalized.Contains("caché") ||
                normalized.Contains("danger") || normalized == "oui" || normalized == "yes")
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Fear,
                    confidence = 0.85f,
                    reasoning = "L'utilisateur suspecte un danger ou un secret caché",
                    suggestedNextEmotions = new[] { Emotion.Fear, Emotion.Surprise },
                    emotionTag = "{FEAR}"
                };
            }

            // Réponse négative → Surprise (curiosité)
            if (isNegative)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.80f,
                    reasoning = "L'utilisateur est surpris/curieux mais sans appréhension",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 4: "Trouves-tu son choix d'entrer risqué?"
        /// Question sur évaluation du risque → détecte Fear ou Surprise
        /// </summary>
        private ContextualEmotionData MapPart4_RiskAssessment(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            // Réponse affirmative → Fear (le choix est risqué)
            if (isAffirmative || normalized == "oui" || normalized == "yes" || 
                normalized.Contains("risqué") || normalized.Contains("danger") ||
                normalized.Contains("dangereux"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Fear,
                    confidence = 0.88f,
                    reasoning = "L'utilisateur juge le choix d'entrer comme risqué",
                    suggestedNextEmotions = new[] { Emotion.Fear, Emotion.Surprise },
                    emotionTag = "{FEAR}"
                };
            }

            // Réponse négative → Surprise (pas de risque, juste de la curiosité)
            if (isNegative)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.80f,
                    reasoning = "L'utilisateur ne juge pas le choix risqué mais reste surpris/curieux",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 5: "Ce symbole te semble-t-il important pour la suite?"
        /// Question sur l'importance d'un indice → détecte Surprise ou Fear
        /// </summary>
        private ContextualEmotionData MapPart5_ImportanceSymbol(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            // Réponse affirmative → Surprise (le symbole est important/intrigant)
            if (isAffirmative || normalized == "oui" || normalized == "yes" || 
                normalized.Contains("important") || normalized.Contains("clé") ||
                normalized.Contains("significat"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.88f,
                    reasoning = "L'utilisateur est surpris/intrigué par l'importance du symbole",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            // Réponse négative ou doute → Fear
            if (isNegative || baseAnalysis.dominantValence < -0.3f)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Fear,
                    confidence = 0.75f,
                    reasoning = "L'utilisateur doute de l'importance mais reste appréhensif",
                    suggestedNextEmotions = new[] { Emotion.Fear, Emotion.Surprise },
                    emotionTag = "{FEAR}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 6: "Es-tu curieux de savoir ce que contient ce fichier?"
        /// Question sur curiosité face aux informations → détecte Surprise ou Disgust
        /// </summary>
        private ContextualEmotionData MapPart6_CuriosityFile(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis,
            bool isAffirmative,
            bool isNegative)
        {
            if (isAffirmative || normalized.Contains("curieux") || normalized.Contains("impatient") ||
                normalized.Contains("intéressé") || baseAnalysis.dominantArousal > 0.4f)
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Surprise,
                    confidence = 0.90f,
                    reasoning = "L'utilisateur est surpris/curieux de découvrir le contenu du fichier",
                    suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                    emotionTag = "{SURPRISE}"
                };
            }

            if (isNegative || normalized.Contains("pas intéressé"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Disgust,
                    confidence = 0.70f,
                    reasoning = "L'utilisateur rejette/n'est pas intéressé",
                    suggestedNextEmotions = new[] { Emotion.Disgust, Emotion.Surprise },
                    emotionTag = "{DISGUST}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// PARTIE 7: "Merci de m'avoir écouté"
        /// Conclusion de l'histoire → accepte toutes les émotions naturellement
        /// </summary>
        private ContextualEmotionData MapPart7_Conclusion(
            string normalized,
            UserResponseAnalyzer.AnalysisResult baseAnalysis)
        {
            if (baseAnalysis.dominantValence > 0.4f || normalized.Contains("merci") ||
                normalized.Contains("bonne") || normalized.Contains("intéressant"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Joy,
                    confidence = 0.85f,
                    reasoning = "L'utilisateur a apprécié l'histoire",
                    suggestedNextEmotions = new[] { Emotion.Joy, Emotion.Surprise },
                    emotionTag = "{JOY}"
                };
            }

            if (baseAnalysis.dominantValence < -0.3f || normalized.Contains("triste") ||
                normalized.Contains("effrayant"))
            {
                return new ContextualEmotionData
                {
                    detectedEmotion = Emotion.Sadness,
                    confidence = 0.80f,
                    reasoning = "L'histoire a laissé une impression sombre",
                    suggestedNextEmotions = new[] { Emotion.Sadness, Emotion.Fear },
                    emotionTag = "{SAD}"
                };
            }

            return CreateResultFromBaseAnalysis(baseAnalysis);
        }

        /// <summary>
        /// Convertit le résultat de l'analyse de base en ContextualEmotionData
        /// </summary>
        private ContextualEmotionData CreateResultFromBaseAnalysis(
            UserResponseAnalyzer.AnalysisResult baseAnalysis)
        {
            return new ContextualEmotionData
            {
                detectedEmotion = baseAnalysis.predictedEmotion,
                confidence = 0.65f + (baseAnalysis.estimatedIntensity * 0.25f),
                reasoning = $"Analyse basée sur: valence={baseAnalysis.dominantValence:F2}, arousal={baseAnalysis.dominantArousal:F2}",
                suggestedNextEmotions = new[] { baseAnalysis.predictedEmotion, Emotion.Surprise },
                emotionTag = EmotionToTag(baseAnalysis.predictedEmotion)
            };
        }

        /// <summary>
        /// Crée un résultat neutre par défaut
        /// </summary>
        private ContextualEmotionData CreateNeutralResult()
        {
            return new ContextualEmotionData
            {
                detectedEmotion = Emotion.Surprise,
                confidence = 0.5f,
                reasoning = "Impossible d'analyser la réponse - émotion par défaut",
                suggestedNextEmotions = new[] { Emotion.Surprise, Emotion.Fear },
                emotionTag = "{SURPRISE}"
            };
        }

        /// <summary>
        /// Convertit une émotion enum en tag texte pour le LLM
        /// Utilise UNIQUEMENT les 6 émotions primaires
        /// </summary>
        private string EmotionToTag(Emotion emotion)
        {
            return emotion switch
            {
                Emotion.Joy => "{JOY}",
                Emotion.Sadness => "{SAD}",
                Emotion.Anger => "{ANGER}",
                Emotion.Fear => "{FEAR}",
                Emotion.Surprise => "{SURPRISE}",
                Emotion.Disgust => "{DISGUST}",
                _ => "{SURPRISE}"  // Fallback: Surprise pour toute autre émotion
            };
        }
    }
}
