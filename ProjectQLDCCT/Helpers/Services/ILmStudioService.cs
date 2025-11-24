using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ProjectQLDCCT.Helpers.Services
{
    public interface ILmStudioService
    {
        Task<string> SuggestAsync(string sectionTitle, string courseName);
    }
}
