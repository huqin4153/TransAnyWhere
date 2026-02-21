using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransAnyWhereApp.Services.Network
{
    public interface IHtmlProvider
    {
        string GetMobilePage(string ip, string port);

        string BuildHttpResponse(string html);
    }
}
