namespace SombraDaSanidade.Interaction
{
    public enum SkillCheckResult { Good, Great, Fail }

    // Opcional para IInteractable que tem skill check de multímetro durante a canalização (GDD 4.4,
    // "calibragem de circuitos"). Hoje só o DriveServer. IInteractable não muda: disjuntor e itens seguem iguais.
    public interface ISkillCheckable
    {
        // Config com a seção "Skill check".
        InteractionConfig SkillCheckConfig { get; }

        // false quando já teve o máximo de checks (3 por HD).
        bool CanSkillCheck { get; }

        // Chamado pelo SkillCheck quando um check começa e quando termina.
        void OnSkillCheckStarted();
        void OnSkillCheckResolved(SkillCheckResult result);
    }
}
