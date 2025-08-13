using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sellorio.YouTubeMusicGrabber.Helpers;

internal static partial class CompareHelper
{
    public static string ToSearchNormalisedTitle(string text)
    {
        if (ExtractFeaturingCredit(text, out var featuringCreditIndex) != null)
        {
            var textWithoutFeaturingCredit = text.Substring(0, featuringCreditIndex);
            return ToSearchNormalisedTitle(textWithoutFeaturingCredit);
        }

        var sb = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            if (c == '’')
            {
                sb.Append('\'');
            }
            else if (char.IsPunctuation(c) && c is not '(' and not ')')
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

        // remove japanese elongated vowel representation to avoid inconsistency
        return textWithoutAccents.Replace("ou", "o").Replace("Ou", "O").ToLowerInvariant();
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
