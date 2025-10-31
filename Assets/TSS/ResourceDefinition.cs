using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    public interface IGrantsProfessionExperience
    {
        Professions.ProfessionIndex Profession { get; }
        int ExperienceAmount { get; }
    }

    public static class ProfessionExperienceExtensions
    {
        public static void GrantProfessionExperience(this Agent agent, IGrantsProfessionExperience experienceSource, int quantity = 1)
        {
            if (agent == null || experienceSource == null)
                return;

            if (quantity <= 0)
                return;

            int experience = experienceSource.ExperienceAmount * quantity;
            if (experience <= 0)
                return;

            Professions professions = agent.GetComponent<Professions>();
            professions?.AddExperience(experienceSource.Profession, experience);
        }
    }

    public abstract class ResourceDefinition : ItemDefinition, IGrantsProfessionExperience
    {
        [SerializeField]
        private Professions.ProfessionIndex _profession = Professions.ProfessionIndex.Mining;

        [SerializeField, Min(0)]
        private int _experienceAmount = 100;

        public virtual Professions.ProfessionIndex Profession => _profession;
        public virtual int ExperienceAmount => _experienceAmount;
    }
}
