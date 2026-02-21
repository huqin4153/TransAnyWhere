using System.Diagnostics.CodeAnalysis;

namespace TransAnyWhereApp.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class FileLinkModel
    {
        public string name { get; set; } = string.Empty;
        public long size { get; set; }
        public string sizeText { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
    }
}
