using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Helpers
{
    public static class ModelBuilderExtensions
    {
        public static void MarkAllEntitiesAsHavingTriggers(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetAnnotation("Relational:Triggers", new[] { "" });
            }
        }
    }
}
