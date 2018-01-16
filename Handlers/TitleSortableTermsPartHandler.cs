using Lombiq.Projections.Constants;
using Lombiq.Projections.Models;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Taxonomies.Fields;
using System.Linq;

namespace Lombiq.Projections.Handlers
{
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TitleSortableTitleSortableTermsPartHandler : ContentHandler
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;


        public TitleSortableTitleSortableTermsPartHandler(
            IContentDefinitionManager contentDefinitionManager,
            IRepository<TitleSortableTermsPartRecord> repository)
        {
            _contentDefinitionManager = contentDefinitionManager;

            Filters.Add(StorageFilter.For(repository));
        }


        protected override void Activating(ActivatingContentContext context)
        {
            base.Activating(context);

            // Weld the TitleSortableTermsPart dynamically, if any Content Part has a Taxonomy Field.
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(context.ContentType);
            if (contentTypeDefinition == null) return;

            if (contentTypeDefinition.Parts.Any(
                part => part.PartDefinition.Fields.Any(
                    field => field.FieldDefinition.Name == nameof(TaxonomyField))))
            {
                context.Builder.Weld<TitleSortableTermsPart>();
            }
        }
    }
}
