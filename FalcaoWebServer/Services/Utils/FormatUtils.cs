using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FalcaoWebServer.Utils
{
    public static class FormatUtils
    {
        // Remove qualquer caractere não numérico
        public static string OnlyDigits(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return new string(input.Where(char.IsDigit).ToArray());
        }

        // Aplica máscara ao CPF
        public static string MaskCpf(string? cpf)
        {
            var digits = OnlyDigits(cpf);
            if (digits.Length != 11)
                return digits;
            return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
        }

        // Aplica máscara ao CNPJ
        public static string MaskCnpj(string? cnpj)
        {
            var digits = OnlyDigits(cnpj);
            if (digits.Length != 14)
                return digits;
            return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
        }

        // Aplica máscara ao CEP (com hífen obrigatório)
        public static string MaskCep(string? cep)
        {
            var digits = OnlyDigits(cep);
            if (digits.Length != 8)
                return digits;
            return $"{digits[..5]}-{digits[5..]}";
        }

        // Formata telefone fixo (ex: (11) 2345-6789)
        public static string MaskPhone(string? ddd, string? number)
        {
            var d = OnlyDigits(ddd);
            var n = OnlyDigits(number);
            if (string.IsNullOrEmpty(d) || string.IsNullOrEmpty(n))
                return string.Empty;

            return n.Length == 8
                ? $"({d}) {n[..4]}-{n[4..]}"
                : $"({d}) {n[..5]}-{n[5..]}";
        }

        // Remove todas as máscaras
        public static string Unmask(string? input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? string.Empty
                : Regex.Replace(input, @"\D", "");
        }
    }
}
