using System.Linq;

namespace FalcaoWebServer.Utils
{
    public static class CpfCnpjValidator
    {
        public static bool IsValidCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return false;
            var numbers = new string(cpf.Where(char.IsDigit).ToArray());
            if (numbers.Length != 11) return false;
            if (numbers.Distinct().Count() == 1) return false;

            int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            var temp = numbers.Substring(0, 9).Select(c => int.Parse(c.ToString())).ToArray();
            int sum = 0;
            for (int i = 0; i < 9; i++) sum += temp[i] * mult1[i];
            int rest = sum % 11;
            int dig1 = rest < 2 ? 0 : 11 - rest;

            var temp2 = temp.Concat(new[] { dig1 }).ToArray();
            sum = 0;
            for (int i = 0; i < 10; i++) sum += temp2[i] * mult2[i];
            rest = sum % 11;
            int dig2 = rest < 2 ? 0 : 11 - rest;

            var last2 = numbers.Substring(9, 2);
            return last2 == $"{dig1}{dig2}";
        }

        public static bool IsValidCnpj(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj)) return false;
            var numbers = new string(cnpj.Where(char.IsDigit).ToArray());
            if (numbers.Length != 14) return false;
            if (numbers.Distinct().Count() == 1) return false;

            int[] mult1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] mult2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            var temp = numbers.Substring(0, 12).Select(c => int.Parse(c.ToString())).ToArray();
            int sum = 0;
            for (int i = 0; i < 12; i++) sum += temp[i] * mult1[i];
            int rest = sum % 11;
            int dig1 = rest < 2 ? 0 : 11 - rest;

            var temp2 = temp.Concat(new[] { dig1 }).ToArray();
            sum = 0;
            for (int i = 0; i < 13; i++) sum += temp2[i] * mult2[i];
            rest = sum % 11;
            int dig2 = rest < 2 ? 0 : 11 - rest;

            var last2 = numbers.Substring(12, 2);
            return last2 == $"{dig1}{dig2}";
        }
    }
}
