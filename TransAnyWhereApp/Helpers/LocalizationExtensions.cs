using Avalonia;
using Avalonia.Controls;
using System.Text.RegularExpressions;

namespace TransAnyWhereApp.Helpers
{
    public static class LocalizationExtensions
    {
        private static readonly Regex _cultureRegex = new Regex(@"\{\{(.+?)\}\}", RegexOptions.Compiled);

        public static string Culture(this string template)
        {
            if (string.IsNullOrEmpty(template)) return template;

            return _cultureRegex.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (Application.Current != null && Application.Current.TryFindResource(key, out var res))
                {
                    return res?.ToString() ?? match.Value;
                }
                return match.Value;
            });
        }
    }
}