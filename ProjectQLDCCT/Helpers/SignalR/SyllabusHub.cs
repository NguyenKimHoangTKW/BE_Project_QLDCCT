using Microsoft.AspNetCore.SignalR;

namespace ProjectQLDCCT.Helpers.SignalR
{
    public class SyllabusHub : Hub
    {
        public async Task JoinSyllabusGroup(int id_syllabus)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"syllabus_{id_syllabus}");
        }

        public async Task LeaveSyllabusGroup(int id_syllabus)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"syllabus_{id_syllabus}");
        }

        public async Task SendDraftSection(int id_syllabus, string section_code, string content)
        {
            await Clients
                .Group($"syllabus_{id_syllabus}")
                .SendAsync("SectionDraftUpdated",
                    id_syllabus,
                    section_code,
                    content
                );
        }
    }
}
