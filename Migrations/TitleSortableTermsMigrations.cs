using Lombiq.Projections.Constants;
using Lombiq.Projections.Models;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Models;

namespace Lombiq.Projections.Migrations
{
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TitleSortableTermsMigrations : DataMigrationImpl
    {
        public int Create()
        {
            var termPartRecordColumnName = $"{nameof(TermPartRecord)}_id";
            var titleSortableTermsPartRecordColumnName = $"{nameof(TitleSortableTermsPartRecord)}_id";

            SchemaBuilder
                .CreateTable(nameof(TitleSortableTermContentItem), table => table
                    .Column<int>(nameof(TitleSortableTermContentItem.Id), column => column.PrimaryKey().Identity())
                    .Column<string>(nameof(TitleSortableTermContentItem.Field), column => column.WithLength(50))
                    .Column<bool>(nameof(TitleSortableTermContentItem.IsFirst))
                    .Column<string>(nameof(TitleSortableTermContentItem.Title))
                    .Column<int>(termPartRecordColumnName)
                    .Column<int>(titleSortableTermsPartRecordColumnName))
                .AlterTable(nameof(TitleSortableTermContentItem), table =>
                    {
                        table.CreateIndex($"IDX_{titleSortableTermsPartRecordColumnName}", titleSortableTermsPartRecordColumnName);
                        table.CreateIndex(
                            $"IDX_{titleSortableTermsPartRecordColumnName}_{nameof(TitleSortableTermContentItem.Field)}_{nameof(TitleSortableTermContentItem.IsFirst)}_{nameof(TitleSortableTermContentItem.Title)}",
                            titleSortableTermsPartRecordColumnName,
                            nameof(TitleSortableTermContentItem.Field),
                            nameof(TitleSortableTermContentItem.IsFirst));
                    });

            SchemaBuilder.CreateTable(nameof(TitleSortableTermsPartRecord), table => table.ContentPartRecord());

            return 1;
        }
    }
}