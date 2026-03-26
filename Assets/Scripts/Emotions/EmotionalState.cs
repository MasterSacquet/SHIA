using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Emotions
{
    /// <summary>
    /// Encapsule l'état émotionnel actuel de l'agent.
    /// - Émotion discrète (une de 10 émotions)
    /// - Dimensions continues (valence, arousal, intensité)
    /// - Historique des transitions émotionnelles
    /// </summary>
    public class EmotionalState
    {
        public enum Emotion
        {
            Joy,           // Joyeux, heureux
            Sadness,       // Triste, déprimé
            Anger,         // Furieux, en colère
            Fear,          // Apeuré, effrayé
            Surprise,      // Surpris, étonné
            Disgust,       // Dégoûté, répugné
            Interest,      // Intéressé, fasciné
            Boredom,       // Ennuyé, ennuyeux
            Frustration,   // Frustré, agacé
            Neutral        // Neutre, calme
        }

        /// <summary>
        /// Dimensions émotionnelles continues (Circumplex model)
        /// </summary>
        [System.Serializable]
        public struct EmotionalDimension
        {
            public float valence;   // -1 (négatif) à +1 (positif)
            public float arousal;   // -1 (calme) à +1 (activé)
            public float intensity; // 0 à 1, force de l'émotion

            public EmotionalDimension(float val, float arous, float intens)
            {
                valence = Mathf.Clamp(val, -1f, 1f);
                arousal = Mathf.Clamp(arous, -1f, 1f);
                intensity = Mathf.Clamp(intens, 0f, 1f);
            }

            public override string ToString() => $"(V:{valence:F2}, A:{arousal:F2}, I:{intensity:F2})";
        }

        /// <summary>
        /// Snapshot pour l'historique
        /// </summary>
        [System.Serializable]
        public struct EmotionSnapshot
        {
            public Emotion emotion;
            public EmotionalDimension dimension;
            public float timestamp;
            public string context;

            public EmotionSnapshot(Emotion e, EmotionalDimension d, float t, string ctx)
            {
                emotion = e;
                dimension = d;
                timestamp = t;
                context = ctx;
            }
        }

        // ===== État interne =====
        private Emotion currentEmotion = Emotion.Surprise;
        private EmotionalDimension currentDimension = new EmotionalDimension(0, 0, 0);
        private List<EmotionSnapshot> history = new List<EmotionSnapshot>();

        // ===== Événements =====
        public event Action<Emotion, Emotion> OnEmotionChanged; // from, to

        public EmotionalState(Emotion initialEmotion = Emotion.Surprise)
        {
            currentEmotion = initialEmotion;
            currentDimension = new EmotionalDimension(0, 0, 0);
            RecordSnapshot($"Initialized: {initialEmotion}");
        }

        // ===== Accesseurs =====
        public Emotion CurrentEmotion => currentEmotion;
        public EmotionalDimension CurrentDimension => currentDimension;
        public List<EmotionSnapshot> HistorySnapshot => new List<EmotionSnapshot>(history);

        /// <summary>
        /// Met à jour l'état émotionnel complet
        /// </summary>
        public void SetEmotionalState(Emotion newEmotion, EmotionalDimension dimension, string context = "")
        {
            Emotion previousEmotion = currentEmotion;
            currentEmotion = newEmotion;
            currentDimension = dimension;
            
            RecordSnapshot(context);
            OnEmotionChanged?.Invoke(previousEmotion, newEmotion);
        }

        /// <summary>
        /// Diminue l'intensité émotionnelle (décroissance naturelle)
        /// </summary>
        public void DecayIntensity(float factor)
        {
            currentDimension.intensity *= Mathf.Clamp(factor, 0f, 1f);
            if (currentDimension.intensity < 0.05f)
            {
                currentDimension.intensity = 0f;
                // Optionnel: revenir à Surprise si intensité trop faible
                if (currentEmotion != Emotion.Surprise)
                {
                    BlendTowardEmotion(Emotion.Surprise, 0.2f);
                }
            }
        }

        /// <summary>
        /// Blend progressivement vers une nouvelle émotion
        /// </summary>
        public void BlendTowardEmotion(Emotion targetEmotion, float blendFactor = 0.15f)
        {
            if (currentEmotion == targetEmotion)
                return;

            float lerpFactor = Mathf.Clamp(blendFactor, 0f, 1f);
            currentDimension.intensity = Mathf.Lerp(currentDimension.intensity, 0.7f, lerpFactor);
            
            // Transition douce après quelques frames
            if (currentDimension.intensity > 0.5f)
            {
                currentEmotion = targetEmotion;
                RecordSnapshot($"Blended to {targetEmotion}");
            }
        }

        /// <summary>
        /// Obtient le tag prompt pour injection dans le LLM
        /// </summary>
        public string GetPromptTag()
        {
            return $"{{{currentEmotion.ToString().ToUpper()}}}";
        }

        /// <summary>
        /// Enregistre un snapshot dans l'historique (max 20 entries)
        /// </summary>
        private void RecordSnapshot(string context = "")
        {
            var snapshot = new EmotionSnapshot(currentEmotion, currentDimension, Time.time, context);
            history.Add(snapshot);
            
            if (history.Count > 20)
                history.RemoveAt(0);
        }

        /// <summary>
        /// Affiche l'historique (debug)
        /// </summary>
        public void PrintHistory()
        {
            Debug.Log("=== Emotional History ===");
            foreach (var snap in history)
            {
                Debug.Log($"[{snap.timestamp:F2}] {snap.emotion} {snap.dimension} - {snap.context}");
            }
        }
    }
}
