using System.Collections.Generic;

namespace TesteCode
{
    public static class FibonacciGenerator
    {
        public static List<int> GerarFibonacci(int x)
        {
            var sequencia = new List<int>();

            if (x <= 0) return sequencia;

            sequencia.Add(0);
            if (x == 1) return sequencia;

            sequencia.Add(1);
            if (x == 2) return sequencia;

            for (int i = 2; i < x; i++)
            {
                int proximo = sequencia[i - 1] + sequencia[i - 2];
                sequencia.Add(proximo);
            }

            return sequencia;
        }
    }
}