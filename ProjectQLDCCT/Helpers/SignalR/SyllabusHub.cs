using Google;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Helpers.SignalR
{
    public class SyllabusHub : Hub
    {
        private readonly QLDCContext db;

        public SyllabusHub(QLDCContext _db)
        {
            db = _db;
        }
        private async Task<string> GetDisplayNameSafe()
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                var token = httpContext?.Request?.Cookies["jwt"];

                if (string.IsNullOrWhiteSpace(token))
                    return FallbackName();

                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken;

                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                }
                catch
                {
                    return FallbackName();
                }

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id_users")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return FallbackName();

                var email = await db.Users
                    .Where(x => x.id_users == userId)
                    .Select(x => x.email)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(email))
                    return FallbackName();

                var loadPermission = await db.CivilServants
                    .Where(g => g.email == email)
                    .Select(g => g.code_civilSer + " - " + g.fullname_civilSer)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(loadPermission))
                    return loadPermission;

                return FallbackName();
            }
            catch
            {
                return FallbackName();
            }
        }

        private string FallbackName()
        {
            var basic = Context.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(basic))
                return basic!;
            return $"User-{Context.ConnectionId.Substring(0, 6)}";
        }

        public async Task UpdateSectionDraft(int syllabusId, string sectionCode, string content)
        {
            await Clients.Group($"syllabus_{syllabusId}")
                .SendAsync("SectionDraftUpdated", syllabusId, sectionCode, content);
        }
        public async Task JoinSyllabusGroup(int syllabusId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"syllabus_{syllabusId}");
        }

        public async Task LeaveSyllabusGroup(int syllabusId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"syllabus_{syllabusId}");
        }

        private static readonly Dictionary<int, Dictionary<string, HashSet<string>>> typingMap
            = new();

        public async Task StartTyping(int syllabusId, string sectionCode)
        {
            var user = await GetDisplayNameSafe();

            if (!typingMap.ContainsKey(syllabusId))
                typingMap[syllabusId] = new();

            if (!typingMap[syllabusId].ContainsKey(sectionCode))
                typingMap[syllabusId][sectionCode] = new();

            typingMap[syllabusId][sectionCode].Add(user);

            await Clients.Group($"syllabus_{syllabusId}")
                .SendAsync(
                    "UserTypingStatusChanged",
                    syllabusId,
                    user,
                    sectionCode,
                    true
                );
        }

        public async Task StopTyping(int syllabusId, string sectionCode)
        {
            var user = await GetDisplayNameSafe();

            if (typingMap.ContainsKey(syllabusId) &&
                typingMap[syllabusId].ContainsKey(sectionCode))
            {
                typingMap[syllabusId][sectionCode].Remove(user);
            }

            await Clients.Group($"syllabus_{syllabusId}")
                .SendAsync(
                    "UserTypingStatusChanged",
                    syllabusId,
                    user,
                    sectionCode,
                    false
                );
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
