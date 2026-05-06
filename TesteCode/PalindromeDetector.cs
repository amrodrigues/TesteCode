using System;

namespace TesteCode
{
    public static class PalindromeDetector
    {
        public static bool EhPalindromo(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return false;

            // Remove espaços e coloca em minúsculo
            var chars = new System.Text.StringBuilder();
            foreach (char c in texto)
            {
                if (c != ' ')
                {
                    chars.Append(char.ToLower(c));
                }
            }

            string textoLimpo = chars.ToString();

            int esquerda = 0;
            int direita = textoLimpo.Length - 1;

            while (esquerda < direita)
            {
                if (textoLimpo[esquerda] != textoLimpo[direita])
                    return false;

                esquerda++;
                direita--;
            }

            return true;
        }
    }
}