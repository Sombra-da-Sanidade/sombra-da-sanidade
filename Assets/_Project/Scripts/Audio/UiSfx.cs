using System;

namespace SombraDaSanidade.Audio
{
    // Sons de interface (#106): navegar, confirmar e voltar nos menus. Os menus chamam direto porque também
    // existem fora das cenas de jogo (o Menu não tem GameManager nem GameSfxHooks). Sem o GameSfx, fica mudo.
    public static class UiSfx
    {
        public static void Move() => Play(s => s.uiMove);
        public static void Confirm() => Play(s => s.uiConfirm);
        public static void Back() => Play(s => s.uiBack);

        private static void Play(Func<GameSfx, SoundEvent> pick)
        {
            var sfx = GameSfx.Load();
            if (sfx != null) AudioService.Play(pick(sfx));
        }
    }
}
