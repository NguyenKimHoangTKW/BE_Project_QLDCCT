using Microsoft.AspNetCore.SignalR;

namespace ProjectQLDCCT.Hubs
{
    public class ImportHub : Hub
    {
        public async Task SendProgress(int processed, int total)
        {
            await Clients.All.SendAsync("updateProgressValue", processed, total);
        }

        public async Task SendCompleted(object result)
        {
            await Clients.All.SendAsync("importCompleted", result);
        }

        public async Task SendFailed(string message)
        {
            await Clients.All.SendAsync("importFailed", message);
        }

        public async Task SendCanceled(int processed)
        {
            await Clients.All.SendAsync("importCanceled", new { processed });
        }
    }
}
