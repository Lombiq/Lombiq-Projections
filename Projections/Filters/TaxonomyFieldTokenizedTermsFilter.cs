using Lombiq.Projections.Constants;
using Lombiq.Projections.Models;
using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Core.Title.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using Orchard.Taxonomies.Fields;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using Orchard.Taxonomies.Settings;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    /// <summary>
    /// Allows filtering content with Terms selected for a specific <see cref="TaxonomyField"/>
    /// instead of a Taxonomy (which can be selected for multiple fields at the same time).
    /// </summary>
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TaxonomyFieldTokenizedTermsFilter : IFilterProvider
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentFieldDriver> _contentFieldDrivers;
        private readonly IContentManager _contentManager;
        private readonly ITaxonomyService _taxonomyService;

        private int _termsFilterId;


        public TaxonomyFieldTokenizedTermsFilter(
            IContentDefinitionManager contentDefinitionManager,
            IEnumerable<IContentFieldDriver> contentFieldDrivers,
            IContentManager contentManager,
            ITaxonomyService taxonomyService)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentFieldDrivers = contentFieldDrivers;
            _contentManager = contentManager;
            _taxonomyService = taxonomyService;

            T = NullLocalizer.Instance;
        }


        public Localizer T { get; set; }


        public void Describe(DescribeFilterContext describe)
        {
            foreach (var part in _contentDefinitionManager.ListPartDefinitions().Where(p => p.Fields.Any(f => f.FieldDefinition.Name == nameof(TaxonomyField))))
            {
                var descriptor = describe.For(
                    part.Name + nameof(TaxonomyFieldTokenizedTermsFilter),
                    T("{0} Taxonomy Fields", part.Name.CamelFriendly()),
                    T("Taxonomy Fields for {0}).", part.Name.CamelFriendly()));

                foreach (var field in part.Fields.Where(f => f.FieldDefinition.Name == nameof(TaxonomyField)))
                {
                    var membersContext = new DescribeMembersContext((storageName, storageType, displayName, description) =>
                        descriptor.Element(
                            type: part.Name + "." + field.Name + "." + storageName,
                            name: new LocalizedString(field.DisplayName + (displayName != null ? ": " + displayName.Text : "")),
                            description: description,
                            filter: context => ApplyFilter(context, storageName, storageType, part, field),
                            display: context => DisplayFilter(context, storageName, storageType, part, field),
                            form: TaxonomyFieldTokenizedTermsFilterForm.FormName));

                    membersContext
                        .Member(null, typeof(TitleSortableTermContentItem), T("Terms"), T("The Terms selected for this {0} defined by a static value or a Token.", nameof(TaxonomyField)))
                        .Enumerate<TaxonomyField>(() => contentField => contentField.Terms);
                }
            }
        }

        public LocalizedString DisplayFilter(FilterContext context, string storageName, Type storageType, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            if (field.FieldDefinition.Name != nameof(TaxonomyField)) return T("Inactive filter: This filter only works with {0}!", nameof(TaxonomyField));

            var taxonomyName = GetSelectedTaxonomyNameForField(field);

            if (string.IsNullOrEmpty(taxonomyName) || _taxonomyService.GetTaxonomyByName(taxonomyName) == null)
                return T("Inactive filter: This field doesn't have a Taxonomy selected!");

            var values = new TaxonomyFieldTokenizedTermsFilterFormElements(context.State);

            if (values.TermProperty == null) return T("Inactive filter: You need to define which Term property to match!");

            if (string.IsNullOrEmpty(values.Terms)) return T("Inactive filter: You need to define the Terms to match!");

            if (values.Operator > 1) return T("Inactive filter: You need to define the operator to match the Terms with!");

            return T("Content items where the value \"{0}\" {1} {2} of the \"{3}\" Taxonomy's Terms' \"{4}\" property selected for {5}.{6}.",
                values.Terms,
                values.Contains ? T("matches") : T("doesn't match"),
                values.Operator == 0 ? T("any") : T("all"),
                taxonomyName,
                values.TermProperty,
                part.Name,
                field.DisplayName);
        }

        public void ApplyFilter(FilterContext context, string storageName, Type storageType, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            if (field.FieldDefinition.Name != nameof(TaxonomyField)) return;

            var values = new TaxonomyFieldTokenizedTermsFilterFormElements(context.State);

            // "Terms" being empty should cause the Query not to filter anything. At this point it's not possible to determine whether
            // the user didn't provide a value or "Terms" was evaluated to empty string (e.g. by tokenization).
            if (string.IsNullOrEmpty(values.Terms) || values.TermProperty == null || values.Operator > 1) return;

            var taxonomyName = GetSelectedTaxonomyNameForField(field);

            if (string.IsNullOrEmpty(taxonomyName)) return;

            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);

            if (taxonomy == null) return;

            var terms = values.Terms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(term => term.Trim()).ToArray();

            if (terms.Length == 0) return;

            switch (values.TermProperty)
            {
                case nameof(TermPart.Id): break;
                case nameof(TermPart.Name): // We need to translate Term names into Ids.
                    var termsQuery = _contentManager
                        .Query<TermPart, TermPartRecord>(VersionOptions.Published)
                        .Where(term => term.TaxonomyId == taxonomy.Id);

                    var termIds = termsQuery
                        .Join<TitlePartRecord>()
                        .Where(title => terms.Contains(title.Title))
                        .List()
                        .Select(term => term.Id.ToString())
                        .Distinct()
                        .ToArray();

                    if (!termIds.Any())
                    {
                        // There are no matching terms, so the query shouldn't return any results.
                        context.Query.Where(
                            a => a.ContentPartRecord<TermPartRecord>(),
                            ex => ex.Eq("Id", 0));
                        return;
                    }
                    else terms = termIds;

                    break;
                default: return;
            }

            Action<IAliasFactory> alias = a => a
                .ContentPartRecord<TermsPartRecord>()
                .Property(nameof(TermsPartRecord.Terms), $"{part.Name}-{field.Name}-{taxonomy.Name}-terms".ToSafeName());
            Action<IHqlExpressionFactory> expression = e => { };

            context.Query.Where(alias, ex => ex.Eq(nameof(TermContentItem.Field), field.Name));

            switch (values.Operator)
            {
                case 0: // Any Term matches.
                    if (values.Contains) expression = ex => ex
                        .In($"{nameof(TermContentItem.TermRecord)}.{nameof(TermPartRecord.Id)}", terms);
                    else expression = ex => ex.Not(ex2 => ex2
                        .In($"{nameof(TermContentItem.TermRecord)}.{nameof(TermPartRecord.Id)}", terms));

                    break;
                case 1: // All Terms match.
                    foreach (var term in terms)
                    {
                        if (values.Contains) expression = ex => ex
                            .Eq($"{nameof(TermContentItem.TermRecord)}.{nameof(TermPartRecord.Id)}", term);
                        else expression = ex => ex.Not(ex2 => ex2
                            .Eq($"{nameof(TermContentItem.TermRecord)}.{nameof(TermPartRecord.Id)}", term));
                    }

                    break;
            }

            context.Query.Where(alias, expression);
        }


        private string GetSelectedTaxonomyNameForField(ContentPartFieldDefinition field) =>
            field.Settings[$"{nameof(TaxonomyFieldSettings)}.{nameof(TaxonomyFieldSettings.Taxonomy)}"];
    }
}