using Orchard.ContentManagement.MetaData;
using Orchard.Data.Migration;
using Orchard.Fields.Settings;
using Orchard.Projections.Models;
using Piedone.HelpfulExtensions;
using static Lombiq.Projections.Constants.FieldNames.ProjectionPart;

namespace Lombiq.Projections.Migrations
{
    public class ProjectionPartMigrations : DataMigrationImpl
    {
        public int Create()
        {
            ContentDefinitionManager.AlterPartDefinition(nameof(ProjectionPart), part => part
                .WithBooleanField(ShowResultCount, field => field
                    .WithBooleanFieldSettings(new BooleanFieldSettings { Hint = "Determines whether the number of results should be displayed or not." })));

            return 1;
        }
    }
}