using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Emotions
{
    /// <summary>
    /// Dictionnaire statique de 150+ mots français classés par émotion.
    /// Utilisé par UserResponseAnalyzer pour détecter les émotions dans les textes.
    /// 
    /// Liste française avec catégories:
    /// - positiveKeywords: Joie, allégresse
    /// - negativeKeywords: Tristesse, dépression
    /// - angerKeywords: Colère, frustration
    /// - fearKeywords: Peur, anxiété
    /// - surpriseKeywords: Surprise, étonnement
    /// - disgustKeywords: Dégoût, répulsion
    /// </summary>
    public static class EmotionalKeywords
    {
        // ===== JOY / POSITIVE =====
        public static readonly List<string> positiveKeywords = new List<string>
        {
            // Joie/Bonheur intense
            "heureux", "heureuse", "heureux", "bonheur", "joyeux", "joie",
            "enthousiasme", "enthousiaste", "content", "contentement", "satisfaction", "satisfait",
            "ravi", "ravie", "délice", "délicieux", "magnifique", "superbe",
            "merveilleux", "formidable", "fantastique", "incroyable", "extraordinaire",
            "excellent", "sublime", "admirable", "adorable", "charmant",
            "beau", "belle", "beaux", "belles", "splendide",
            
            // Aimer/Amour
            "aimer", "amour", "adorable", "adorer", "chéri", "chérie",
            "affection", "tendre", "tendresse", "calin", "caresse",
            
            // Réussite/Accomplissement
            "succès", "réussi", "réussite", "triumph", "victoire", "gagner",
            "accomplissement", "réalisation", "fierté", "fier", "fière",
            
            // Relief/Alègement
            "relief", "soulagé", "soulagée", "soulagement", "ouff",
            
            // Autres positifs
            "bien", "super", "chouette", "parfait", "bonne", "bon"
        };

        // ===== SADNESS / NEGATIVE =====
        public static readonly List<string> negativeKeywords = new List<string>
        {
            // Tristesse
            "triste", "tristesse", "déprimé", "déprime", "dépression", "malheur",
            "malheureux", "malheureuse", "chagrin", "chagriner", "affliction", "affligé",
            "abattu", "abattue", "découragé", "découragée", "démoralisé", "démoralisée",
            "morose", "sombre", "noir", "étouffement", "étouffé",
            
            // Larmes/Pleurer
            "pleur", "pleurer", "larmes", "larme", "sanglot", "sangloter",
            "larmoyant", "larmoyante",
            
            // Perte/Absence
            "perte", "perdu", "perdue", "absence", "absent", "manque",
            "manquer", "vide", "vieillesse", "mort", "mortel", "décès",
            "deuil", "endeuillé", "endeuillée",
            
            // Mécontentement
            "mal", "mauvais", "mauvaise", "terrible", "horrible", "affreux",
            "atroce", "abominable", "déplorable", "détestable",
            
            // Autres négatifs
            "non", "non", "pas", "rien", "jamais"
        };

        // ===== ANGER / FRUSTRATION =====
        public static readonly List<string> angerKeywords = new List<string>
        {
            // Colère
            "colère", "coléreux", "colérease", "furibond", "furibonde", "fureur",
            "furieux", "furieuse", "furieusement", "accès de colère", "sortir de ses gonds",
            "irrité", "irritée", "irritation", "irritant", "irriter",
            "énervé", "énervée", "énervement", "énerver", "exaspéré", "exaspérée",
            "exaspération", "exaspérant", "exaspérante",
            
            // Rage
            "rage", "enragé", "enragée", "déchaîné", "déchaînée",
            "déchaînement", "feu", "tempête", "orage",
            
            // Frustration
            "frustration", "frustré", "frustrée", "frustrant", "frustrante",
            "déception", "déçu", "déçue", "décevant", "décevante", "décevoir",
            
            // Impatience
            "impatience", "impatient", "impatiente", "impatiemment",
            "urgence", "urgent", "urgente",
            
            // Agacement
            "agacement", "agacé", "agacée", "agaçant", "agaçante", "agacer",
            "embêtement", "embêté", "embêtée", "embêtant", "embêtante", "embêter",
            "ennui", "ennuyé", "ennuyée", "ennuyant", "ennuyante", "ennuyer",
            
            // Détester/Haïr
            "déteste", "détester", "détesté", "détestable",
            "haine", "haïr", "haïssable", "odious", "odieuse",
            
            // Conflits
            "conflit", "querelle", "dispute", "disputer", "se quereller",
            "querelleur", "querelleuse", "batailles", "batalla", "combattre"
        };

        // ===== FEAR / ANXIETY =====
        public static readonly List<string> fearKeywords = new List<string>
        {
            // Peur
            "peur", "peureux", "peureuse", "effrayé", "effrayée", "effrayer",
            "frayeur", "frayas", "terrorisé", "terrorisée", "terreur", "terrifié",
            "térrifiée", "terrifiant", "terrifiante", "terriblement", "terrêt",
            
            // Anxiété/Inquiétude
            "anxiété", "anxieux", "anxieuse", "angoisse", "angoissé", "angoissée",
            "angoissant", "angoissante", "inquiétude", "inquiet", "inquiète", "inquiets",
            "inquiètes", "inquiétant", "inquiétante", "s'inquiéter", "préoccupé", "préoccupée",
            "préoccupation", "préoccupant", "préoccupante", "préoccuper",
            
            // Alarme/Alerte
            "alerte", "alerté", "alertée", "alarme", "alarmé", "alarmée",
            "alarmant", "alarmante", "alarmer", "cri d'alerte",
            
            // Panique
            "panique", "paniqué", "paniquée", "paniqué", "paniquée", "paniquer",
            
            // Appréhension
            "appréhension", "appréhender", "appréhendé", "appréhendée",
            "crainte", "craintes", "craintif", "craintive", "craindre", "redout",
            "redoutable", "redoutable", "redoutance", "redoutée",
            
            // Menace/Danger
            "menace", "menacé", "menacée", "menaçant", "menaçante", "menacer",
            "danger", "dangéreux", "dangéreuse", "dangereux", "péril", "périlleux",
            "périodique", "péril", "menace", "menaçer"
        };

        // ===== SURPRISE =====
        public static readonly List<string> surpriseKeywords = new List<string>
        {
            // Surprise positive/neutre
            "surprise", "surpris", "surprise", "surprendre", "surprenant",
            "surprenante", "étonnement", "étonné", "étonnée", "étonnant",
            "étonnante", "étonner", "stupéfaction", "stupéfait", "stupéfaite",
            "décontenancé", "décontenancée", "déconcertation", "déconcertant",
            "déconcertante", "déconcerter", "inattendre", "inattendue",
            
            // Émerveillement
            "émerveillement", "émerveillé", "émerveillée", "émerveiller",
            "éblouissement", "ébloui", "éblouie", "éblouissant", "éblouissante",
            "éblouir", "spectaculaire", "spectaculairement",
            
            // Wow/Incroyable
            "wow", "waouh", "waoh", "incroyable", "incroyablement", "pas croyable",
            "je n'y crois pas", "impossible", "impensable", "improbable",
            
            // Extraordinaire
            "extraordinaire", "extraordinairement", "inusité", "inusitée",
            "singulier", "singulière", "singulièrement", "bizarre", "bizarrement"
        };

        // ===== DISGUST =====
        public static readonly List<string> disgustKeywords = new List<string>
        {
            // Dégoût
            "dégoût", "dégoutant", "dégoutante", "dégoûtant", "dégoûtante",
            "dégouté", "dégoutée", "dégoûté", "dégoûtée", "dégoutance", "dégoutas",
            "révolte", "révoltant", "révoltante", "révolté", "révoltée", "révolter",
            
            // Répugnance/Répul
            "répugnance", "répugnant", "répugnante", "répugner", "répugnez",
            "répulsif", "répulsive", "répulsion",
            
            // Haut le coeur/Nausée
            "nausée", "nauséabond", "nauséabonde", "nauséeux", "nauséeuse",
            "haut le coeur", "régurgitation", "régurgiter", "vomir", "vomissement",
            "rejet", "rejetable", "rejeter",
            
            // Sale/Infâme
            "sale", "salété", "saleté", "infâme", "infâmie", "abomination",
            "abominable", "détestable",
            
            // Honte/Ignominie
            "honte", "honteux", "honteuse", "ignominie", "ignominieux",
            "ignominieuse", "shame", "shameful", "vilenie", "vile",
            "vilain", "vilaine", "vilanement"
        };

        // ===== INTEREST =====
        public static readonly List<string> interestKeywords = new List<string>
        {
            // Curiosité/Intérêt
            "curiosité", "curieux", "curieuse", "curieusement", "intérêt",
            "intéressant", "intéressante", "intéressé", "intéressée", "intéresser",
            "attrait", "attrayant", "attrayante", "attirant", "attirante",
            
            // Fascination
            "fascination", "fasciné", "fascinée", "fascinant", "fascinante",
            "fasciner", "captivation", "captivant", "captivante", "captiver",
            "enchantement", "enchanté", "enchantée", "envoûtement", "envoûtant",
            "envoûtante", "envoûter", "charme", "charmant", "charmante", "charmer",
            
            // Engagement/Absorption
            "engagement", "engagé", "engagée", "absorption", "absorbé", "absorbée",
            "préoccupation", "préoccupé", "préoccupée", "préoccuper", "absorption",
            
            // Désir de connaissance
            "curiosité", "question", "questionner", "interrogation",
            "interroger", "enquête", "enquêter", "investigation",
            
            // Enthousiasme (aussi dans positif)
            "enthousiasme", "enthousiaste", "enthousiasmé", "enthousiasmée",
            "enthousiasmer", "zèle", "zélé", "zélée", "passion", "passionné", "passionnée"
        };

        // ===== BOREDOM =====
        public static readonly List<string> boredomKeywords = new List<string>
        {
            // Ennui
            "ennui", "ennuyé", "ennuyée", "ennuyant", "ennuyante", "ennuyer",
            "barbant", "barbante", "barbe", "rasoir", "rasante",
            
            // Apathie
            "apathie", "apathique", "apathiquement", "indifférence",
            "indifférent", "indifférente", "indifféremment",
            
            // Désintérêt
            "désintérêt", "désintéressé", "désintéressée", "désintéresser",
            "inattention", "inattentif", "inattentive",
            
            // Monotonie
            "monotonie", "monotone", "monotonement", "répétitif", "répétitive",
            "répétition", "répéter", "banal", "banale", "banalement",
            "commun", "commune", "couramment", "ordinaire", "ordinairement",
            "généralement", "usuel", "usuelle", "habituellement",
            
            // Somme
            "somme", "somnolent", "somnolente", "somnolence", "drowsy",
            "sommeiller", "accablé", "accablée", "lourd", "lourde", "lourdement",
            "fatigue", "fatigué", "fatiguée", "fatigant", "fatigante", "fatiguer"
        };

        // ===== INTENSITY MODIFIERS =====
        public static readonly List<string> intensityModifiers = new List<string>
        {
            "très", "extrêmement", "trop", "beaucoup", "énormément", "terriblement",
            "franchement", "carrément", "à peine", "légèrement", "un peu", "légèr",
            "légèrement", "plutôt", "assez", "pas mal", "vraiment", "réellement",
            "sincèrement", "pour de bon", "certainement", "absolument"
        };

        /// <summary>
        /// Compte le score émotionnel d'une liste de mots
        /// Retourne: valence de -1 à +1
        /// </summary>
        public static float GetValenceFromKeywords(List<string> matchedKeywords)
        {
            if (matchedKeywords.Count == 0)
                return 0f;

            float score = 0f;
            int positive = 0, negative = 0;

            foreach (var keyword in matchedKeywords)
            {
                string lower = keyword.ToLower();
                if (positiveKeywords.Contains(lower) || interestKeywords.Contains(lower))
                    positive++;
                else if (negativeKeywords.Contains(lower) || angerKeywords.Contains(lower) || 
                         fearKeywords.Contains(lower) || disgustKeywords.Contains(lower))
                    negative++;
            }

            score = (positive - negative) / (float)(positive + negative + 1);
            return Mathf.Clamp(score, -1f, 1f);
        }

        /// <summary>
        /// Détermine l'arousal basé sur les mots clés
        /// Retourne: arousal de -1 (calme) à +1 (activé)
        /// </summary>
        public static float GetArousalFromKeywords(List<string> matchedKeywords)
        {
            if (matchedKeywords.Count == 0)
                return 0f;

            int activating = 0;  // Colère, peur, surprise, joie intense
            int calming = 0;     // Tristesse, ennui, contentement - calme

            foreach (var keyword in matchedKeywords)
            {
                string lower = keyword.ToLower();
                if (angerKeywords.Contains(lower) || fearKeywords.Contains(lower) || 
                    surpriseKeywords.Contains(lower) || (positiveKeywords.Contains(lower) && matchedKeywords.Count > 2))
                    activating++;
                else if (negativeKeywords.Contains(lower) || boredomKeywords.Contains(lower))
                    calming++;
            }

            float score = (activating - calming) / (float)(activating + calming + 1);
            return Mathf.Clamp(score, -1f, 1f);
        }
    }
}
