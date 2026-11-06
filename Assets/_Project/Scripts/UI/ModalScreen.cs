namespace SombraDaSanidade.UI
{
    // Telas que tomam o mouse e o teclado do jogo (painel de teste da Beta, por exemplo).
    // A lanterna ignora clique e F enquanto alguma está aberta, igual ao UpgradeTerminal.
    public static class ModalScreen
    {
        private static int open;

        public static bool IsOpen => open > 0;

        public static void Push() => open++;

        public static void Pop()
        {
            if (open > 0) open--;
        }
    }
}
