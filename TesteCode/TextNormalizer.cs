using System.Text.RegularExpressions;

namespace TesteCode
{
    public static class TextNormalizer
    {
        public static string NormalizarTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;

            string resultado = Regex.Replace(texto, @"[!?]*\?[!?]*![!?]*|[!?]*![!?]*\?[!?]*", "?!");
            resultado = Regex.Replace(resultado, @"(\?!)+", "?!");
            resultado = Regex.Replace(resultado, @"!{2,}", "!");
            resultado = Regex.Replace(resultado, @"\?{2,}", "?");

            return resultado;
        }
    }
}