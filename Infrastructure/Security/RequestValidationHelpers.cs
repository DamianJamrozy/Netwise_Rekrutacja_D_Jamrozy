using System.Text.RegularExpressions;

namespace Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Security;

public static partial class RequestValidationHelpers
{
    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeFileNameRegex();

    public static bool IsSafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            return false;
        }

        if (fileName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return SafeFileNameRegex().IsMatch(fileName);
    }
}