using System.Diagnostics.CodeAnalysis;

namespace TransAnyWhereApp.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class WsProtocolModel
    {
        public string type { get; set; } = string.Empty;
        public object? data { get; set; }
    }
}
