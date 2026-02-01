using UnityEngine;

namespace Magikill.Combat
{
    /// <summary>
    /// ScriptableObject that defines a skill's properties and behavior.
    /// Create instances via Assets > Create > Magikill > Skills > Skill Definition
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "Magikill/Skills/Skill Definition", order = 1)]
    public class SkillDefinition : ScriptableObject
    {
        #region Basic Info

        [Header("Basic Information")]
        [SerializeField]
        [Tooltip("Display name of the skill")]
        private string skillName = "New Skill";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Description shown in UI tooltips")]
        private string description = "Skill description here.";

        [SerializeField]
        [Tooltip("Icon displayed on skill buttons")]
        private Sprite icon;

        #endregion

        #region Combat Properties

        [Header("Combat Properties")]
        [SerializeField]
        [Tooltip("Base damage dealt by this skill")]
        private float damage = 10f;

        [SerializeField]
        [Tooltip("Type of damage (Physical or Magical)")]
        private DamageType damageType = DamageType.Physical;

        [SerializeField]
        [Tooltip("Mana cost to cast this skill")]
        private float manaCost = 10f;

        [SerializeField]
        [Tooltip("Cooldown duration in seconds")]
        private float cooldown = 1f;

        [SerializeField]
        [Tooltip("Maximum range at which this skill can be used")]
        private float range = 5f;

        #endregion

        #region Targeting & Effects

        [Header("Targeting & Effects")]
        [SerializeField]
        [Tooltip("How this skill targets enemies")]
        private SkillTargetType targetType = SkillTargetType.AutoTarget;

        [SerializeField]
        [Tooltip("How the skill effect is delivered")]
        private SkillEffectType effectType = SkillEffectType.Instant;

        #endregion

        #region Visual Feedback

        [Header("Visual Feedback")]
        [SerializeField]
        [Tooltip("Particle effect spawned when skill is cast")]
        private GameObject particleEffectPrefab;

        [SerializeField]
        [Tooltip("Animation trigger name for the character animator")]
        private string animationTrigger = "Attack";

        #endregion

        #region Public Accessors

        public string SkillName => skillName;
        public string Description => description;
        public Sprite Icon => icon;
        public float Damage => damage;
        public DamageType DamageType => damageType;
        public float ManaCost => manaCost;
        public float Cooldown => cooldown;
        public float Range => range;
        public SkillTargetType TargetType => targetType;
        public SkillEffectType EffectType => effectType;
        public GameObject ParticleEffectPrefab => particleEffectPrefab;
        public string AnimationTrigger => animationTrigger;

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure values are not negative
            damage = Mathf.Max(0f, damage);
            manaCost = Mathf.Max(0f, manaCost);
            cooldown = Mathf.Max(0f, cooldown);
            range = Mathf.Max(0f, range);
        }

        #endregion

        #region Debug Utilities

        /// <summary>
        /// Returns a formatted string with all skill properties for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"[Skill: {skillName}] Damage: {damage} ({damageType}), Mana: {manaCost}, " +
                   $"Cooldown: {cooldown}s, Range: {range}, Target: {targetType}, Effect: {effectType}";
        }

        #endregion
    }
}
