namespace SombraDaSanidade.Audio
{
    // Grupos do SombraMixer (docs/audio/sound-design.md, seção 3). Cada um vira um grupo do mixer com o volume
    // exposto como "<Nome>Volume". O nome do enum é o nome do grupo: não renomear sem recriar o mixer.
    public enum AudioBus
    {
        Music,
        Ambience,
        Sfx,
        Monster,
        Voice,
        Ui
    }
}
