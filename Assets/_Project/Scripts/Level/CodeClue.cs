using SombraDaSanidade.Interaction;
using UnityEngine;

namespace SombraDaSanidade.Level
{
    // Pista em fósforo com um dígito da senha do HD-03 (Ala Executiva, "Senhas com sanidade baixa").
    // Fica no mesmo objeto do SanityClue, que cuida de só mostrar com a sanidade baixa. O texto mostra a posição
    // do dígito ("SENHA _ 7 _ _"), então a ordem em que o jogador acha as pistas não importa.
    public class CodeClue : MonoBehaviour
    {
        [SerializeField] private KeypadLock keypad;
        [SerializeField, Min(0)] private int index;

        public int Index => index;
        public string Text { get; private set; } = "";

        // Start: a senha é sorteada no Awake do KeypadLock.
        private void Start()
        {
            if (keypad == null) keypad = FindAnyObjectByType<KeypadLock>();
            if (keypad == null)
            {
                Debug.LogError($"{name}: CodeClue sem KeypadLock na cena.", this);
                return;
            }

            var sb = new System.Text.StringBuilder("SENHA ");
            for (int i = 0; i < keypad.Code.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(i == index ? keypad.Digit(i) : '_');
            }
            Text = sb.ToString();
            var mesh = GetComponent<TextMesh>();
            if (mesh != null) mesh.text = Text;
        }
    }
}
