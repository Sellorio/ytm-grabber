using System.Globalization;
using System.Linq;
using System.Text;

namespace Sellorio.YouTubeMusicGrabber.Helpers;

internal static class CompareHelper
{
    public static string ToSearchNormalisedTitle(string text)
    {
        return text.Replace(" ", "");
    }

    public static string ToSearchNormalisedName(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var filtered = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        var textWithoutAccents = new string(filtered.ToArray()).Normalize(NormalizationForm.FormC);

        // remove japanese elongated vowel representation to avoid inconsistency
        return textWithoutAccents.Replace("ou", "o").Replace("Ou", "O");
    }
}
