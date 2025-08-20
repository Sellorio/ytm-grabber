using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sellorio.YouTubeMusicGrabber.Helpers;

internal static partial class CompareHelper
{
    public static string ToSearchNormalisedTitle(string text)
    {
        if (text == null)
        {
            return null;
        }

        if (ExtractFeaturingCredit(text, out var featuringCreditIndex) != null)
        {
            var textWithoutFeaturingCredit = text.Substring(0, featuringCreditIndex);
            return ToSearchNormalisedTitle(textWithoutFeaturingCredit);
        }

        var sb = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            switch (c)
            {
                case '’': sb.Append('\''); continue;
                case '“': sb.Append('\"'); continue;
                case '”': sb.Append('\"'); continue;
                case '？': sb.Append('?'); continue;
                case '⁰' or '₀': sb.Append('0'); continue;
                case '¹' or '₁': sb.Append('1'); continue;
                case '²' or '₂': sb.Append('2'); continue;
                case '³' or '₃': sb.Append('3'); continue;
                case '⁴' or '₄': sb.Append('4'); continue;
                case '⁵' or '₅': sb.Append('5'); continue;
                case '⁶' or '₆': sb.Append('6'); continue;
                case '⁷' or '₇': sb.Append('7'); continue;
                case '⁸' or '₈': sb.Append('8'); continue;
                case '⁹' or '₉': sb.Append('9'); continue;
                case '⁺' or '₊': sb.Append('+'); continue;
                case '⁻' or '₋': sb.Append('-'); continue;
                case '⁼' or '₌': sb.Append('='); continue;
                case '⁽' or '₍' or '（' or '(' or '「': continue;
                case '⁾' or '₎' or '）' or ')' or '」': continue;
                case 'ⁿ' or 'ₙ': sb.Append('n'); continue;
                case 'ₐ': sb.Append('a'); continue;
                case 'ₑ': sb.Append('e'); continue;
                case 'ₒ': sb.Append('o'); continue;
                case 'ₓ': sb.Append('x'); continue;
                case 'ₔ': sb.Append('e'); continue;
                case 'ₕ': sb.Append('h'); continue;
                case 'ₖ': sb.Append('k'); continue;
                case 'ₗ': sb.Append('l'); continue;
                case 'ₘ': sb.Append('m'); continue;
                case 'ₚ': sb.Append('p'); continue;
                case 'ₛ': sb.Append('s'); continue;
                case 'ₜ': sb.Append('t'); continue;
                case '–' or '-': continue;
                case '〜' or '~': continue;
                case '・': continue;
                case '/' or '\\': continue;
                case '…': sb.Append("..."); continue;
                case '&': sb.Append("and"); continue;
            }

            if (char.IsPunctuation(c) && c is not ':' and not ',' and not '.')
            {
                sb.Append(c);
            }
            else if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }

    public static string ToSearchNormalisedName(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var filtered = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        var textWithoutAccents = new string(filtered.ToArray()).Normalize(NormalizationForm.FormC);

        return
            textWithoutAccents
                // remove japanese elongated vowel representation to avoid inconsistency
                .Replace("ou", "o")
                .Replace("Ou", "O")
                // YouTube: ULTRATOWER, MusicBrainz: ULTRA TOWER, Me: :(
                .Replace(" ", "")
                .ToLowerInvariant();
    }

    public static string ExtractFeaturingCredit(string text, out int startIndex)
    {
        var matchForFeaturingCredit = FeaturingCreditRegex().Match(text);
        startIndex = matchForFeaturingCredit.Success ? matchForFeaturingCredit.Index : default;
        return matchForFeaturingCredit.Success ? matchForFeaturingCredit.Groups[1].Value : null;
    }

    [GeneratedRegex(@" (?:\(|)f(?:ea|)t\.(?: |)(.+?)\)")]
    private static partial Regex FeaturingCreditRegex();
}
