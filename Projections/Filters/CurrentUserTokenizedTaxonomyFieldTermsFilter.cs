using Lombiq.Projections.Constants;
using Lombiq.Projections.Models;
using Lombiq.Projections.Projections.Forms;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Taxonomies.Fields;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using Orchard.Utility.Extensions;
using Piedone.HelpfulLibraries.Contents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    /// <summary>
    /// Allows or prevents the query from returning any results based on this filter matching the current User.
    /// </summary>
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class CurrentUserTokenizedTaxonomyFieldTermsFilter : TokenizedTaxonomyFieldTermsFilter
    {
        private readonly WorkContext _workContext;

        public CurrentUserTokenizedTaxonomyFieldTermsFilter(
            IContentDefinitionManager contentDefinitionManager,
            IContentManager contentManager,
            ITaxonomyService taxonomyService,
            WorkContext workContext)
            : base(contentDefinitionManager, contentManager, taxonomyService)
        {
            _workContext = workContext;
        }

        public override void Describe(DescribeFilterContext describe)
        {
            foreach (var part in _contentDefinitionManager.GetTypeDefinition("User").Parts
                .Select(part => part.PartDefinition)
                .Where(p => p.Fields.Any(f => f.FieldDefinition.Name == nameof(TaxonomyField))))
            {
                var descriptor = describe.For(
                    nameof(CurrentUserTokenizedTaxonomyFieldTermsFilter),
                    T("Current User Taxonomy Fields"),
                    T("Current User Taxonomy Fields"));

                foreach (var field in part.Fields.Where(f => f.FieldDefinition.Name == nameof(TaxonomyField)))
                {
                    var membersContext = new DescribeMembersContext((storageName, storageType, displayName, description) =>
                        descriptor.Element(
                            type: part.Name + "." + field.Name + "." + storageName,
                            name: new LocalizedString($"{part.Name} - {field.DisplayName}"),
                            description: description,
                            filter: context => ApplyFilter(context, storageName, storageType, part, field),
                            display: context => DisplayFilter(context, storageName, storageType, part, field),
                            form: nameof(TokenizedTaxonomyFieldTermsFilterForm)));

                    membersContext.Member(
                        null,
                        typeof(TitleSortableTermContentItem),
                        null,
                        T("The Terms selected for this Taxonomy Field of the current User defined by a static value or a Token."));
                }
            }
        }

        public override LocalizedString DisplayFilter(
            FilterContext context,
            string storageName,
            Type storageType,
            ContentPartDefinition part,
            ContentPartFieldDefinition field)
        {
            var values = new TokenizedTaxonomyFieldTermsFilterFormElements(context.State);
            var taxonomyName = GetSelectedTaxonomyNameForField(field);

            return GetFormValidationError(field, taxonomyName, values)
                ?? T("The query will return results if the value \"{0}\" {1} {2} of the \"{3}\" Taxonomy's Terms' \"{4}\" property selected for {5}.{6} for the current User.",
                    values.Terms,
                    values.Contains ? T("matches") : T("doesn't match"),
                    values.Operator == 0 ? T("any") : T("all"),
                    taxonomyName,
                    values.TermProperty,
                    part.Name,
                    field.DisplayName);
        }

        public override void ApplyFilter(
            FilterContext context,
            string storageName,
            Type storageType,
            ContentPartDefinition part,
            ContentPartFieldDefinition field)
        {
            if (field.FieldDefinition.Name != nameof(TaxonomyField)) return;

            var formValues = new TokenizedTaxonomyFieldTermsFilterFormElements(context.State);

            // "Terms" being empty should cause the Query not to filter anything. At this point it's not possible to determine whether
            // the user didn't provide a value or "Terms" was evaluated to empty string (e.g. by tokenization).
            if (string.IsNullOrEmpty(formValues.Terms)
                || string.IsNullOrEmpty(formValues.TermProperty)
                || formValues.Operator > 1)
            {
                context.Query.NullQuery();

                return;
            }

            var taxonomyName = GetSelectedTaxonomyNameForField(field);

            if (string.IsNullOrEmpty(taxonomyName))
            {
                context.Query.NullQuery();

                return;
            }

            var taxonomy = _taxonomyService.GetTaxonomyByName(taxonomyName);

            if (taxonomy == null)
            {
                context.Query.NullQuery();

                return;
            }

            var terms = formValues.Terms
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(term => term.Trim())
                .Distinct()
                .ToArray();

            if (!terms.Any())
            {
                context.Query.NullQuery();

                return;
            }

            var taxonomyField = _workContext.CurrentUser.AsField<TaxonomyField>(part.Name, field.Name);

            if (taxonomyField == null || !taxonomyField.Terms.Any())
            {
                context.Query.NullQuery();

                return;
            }

            IEnumerable<string> userTermValues;
            switch (formValues.TermProperty)
            {
                case nameof(TermPart.Id):
                    userTermValues = taxonomyField.Terms.Select(term => term.Id.ToString());

                    break;
                case nameof(TermPart.Name):
                    userTermValues = taxonomyField.Terms.Select(term => term.Name);

                    break;
                default:
                    context.Query.NullQuery();

                    return;
            }

            if (formValues.Contains)
            {
                if ((formValues.Operator == 0 && !terms.Any(value => userTermValues.Contains(value)))
                    || (formValues.Operator == 1 && terms.Any(value => !userTermValues.Contains(value))))
                {
                    context.Query.NullQuery();
                }
            }
            else
            {
                if ((formValues.Operator == 0 && !terms.Any(value => !userTermValues.Contains(value)))
                    || (formValues.Operator == 1 && terms.Any(value => userTermValues.Contains(value))))
                {
                    context.Query.NullQuery();
                }
            }
        }
    }
}
