using Lombiq.Projections.Constants;
using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Title.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using Orchard.Taxonomies.Fields;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    /// <summary>
    /// Allows filtering content with Terms selected for any <see cref="TaxonomyField"/>(s)
    /// that belongs to the selected (one, more or any) Taxonomies.
    /// </summary>
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TokenizedTaxonomyTermsFilter : IFilterProvider
    {
        private readonly ITaxonomyService _taxonomyService;
        private readonly IContentManager _contentManager;
        private int _termsFilterId;

        public Localizer T { get; set; }


        public TokenizedTaxonomyTermsFilter(ITaxonomyService taxonomyService, IContentManager contentManager)
        {
            _taxonomyService = taxonomyService;
            _contentManager = contentManager;
            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            describe.For("Taxonomy", T("Taxonomy"), T("Taxonomy"))
                .Element(typeof(TokenizedTaxonomyTermsFilter).Name, T("Tokenized Taxonomy Terms"), T("Content items with matching tokenized Taxonomy Terms definition."),
                    ApplyFilter, DisplayFilter, TokenizedTaxonomyTermsFilterForm.FormName);
        }

        public void ApplyFilter(FilterContext context)
        {
            var values = new TokenizedTaxonomyTermsFilterFormElements(context.State);

            // "Terms" being empty should cause the Query not to filter anything. At this point it's not possible to determine whether
            // the user didn't provide a value or "Terms" was evaluated to empty string (e.g. by tokenization).
            if (string.IsNullOrEmpty(values.Terms)) return;

            void zeroResultAlias(IAliasFactory a) => a.ContentPartRecord<TermPartRecord>();
            void zeroResultExpression(IHqlExpressionFactory ex) => ex.Eq("Id", 0);

            // The user is warned in the Query editor that either of these values being "null" will cause the Query to yield no results.
            if (values.TermProperty == null || values.Operator > 1)
            {
                context.Query.Where(zeroResultAlias, zeroResultExpression);
                return;
            }

            var terms = values.Terms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(term => term.Trim()).ToArray();

            if (terms.Length == 0) return;

            var taxonomyIds = values.TaxonomyAliases.Any() ?
                _contentManager
                    .Query(VersionOptions.Published, "Taxonomy")
                    .List()
                    .Where(taxonomy => values.TaxonomyAliases.Contains(taxonomy.As<IAliasAspect>().Path))
                    .Select(taxonomy => taxonomy.Id)
                    .ToArray() :
                new int[] { };

            switch (values.TermProperty)
            {
                case nameof(TermPart.Id): break;
                case nameof(TermPart.Name): // We need to translate Term names into Ids.
                    var termsQuery = _contentManager.Query<TermPart, TermPartRecord>(VersionOptions.Published);
                    if (taxonomyIds.Any())
                        termsQuery = termsQuery.Where<TermPartRecord>(term => taxonomyIds.Contains(term.TaxonomyId));

                    var termIds = termsQuery
                        .Join<TitlePartRecord>()
                        .Where(title => terms.Contains(title.Title))
                        .List()
                        .Select(term => term.Id.ToString())
                        .Distinct()
                        .ToArray();

                    if (!termIds.Any())
                    {
                        // There are not matching terms, so the query shouldn't return any results.
                        context.Query.Where(zeroResultAlias, zeroResultExpression);
                        return;
                    }
                    else terms = termIds;

                    break;
                default: return;
            }

            Action<IAliasFactory> alias;
            Action<IHqlExpressionFactory> expression;

            switch (values.Operator)
            {
                case 0: // Any Term matches.
                    alias = a => a
                        .ContentPartRecord<TermsPartRecord>()
                            .Property("Terms", string.Format("terms-" + _termsFilterId++));

                    if (taxonomyIds.Any())
                        if (values.Contains) expression = ex => ex.And(
                            ex2 => ex2.In("TermRecord.Id", terms),
                            ex2 => ex2.In("TermRecord.TaxonomyId", taxonomyIds));
                        else expression = ex => ex.Not(ex2 => ex2.And(
                            ex3 => ex3.In("TermRecord.Id", terms),
                            ex3 => ex3.In("TermRecord.TaxonomyId", taxonomyIds)));
                    else
                        if (values.Contains) expression = ex => ex.In("TermRecord.Id", terms);
                    else expression = ex => ex.Not(ex2 => ex2.In("TermRecord.Id", terms));


                    context.Query.Where(alias, expression);

                    break;
                case 1: // All Terms match.
                    foreach (var term in terms)
                    {
                        alias = a => a
                            .ContentPartRecord<TermsPartRecord>()
                                .Property("Terms", string.Format("terms-" + term));

                        if (values.Contains) expression = ex => ex.Eq("TermRecord.Id", term);
                        else expression = ex => ex.Not(ex2 => ex2.Eq("TermRecord.Id", term));

                        context.Query.Where(alias, expression);
                    }

                    break;
            }
        }

        public LocalizedString DisplayFilter(FilterContext context)
        {
            var values = new TokenizedTaxonomyTermsFilterFormElements(context.State);

            if (values.TermProperty == null) return T("Inactive filter: You need to define which Term property to match.");

            if (string.IsNullOrEmpty(values.Terms)) return T("Inactive filter: You need to define the Terms to match.");

            if (values.Operator > 1) return T("Inactive filter: You need to define the operator to match the Terms with.");

            var notNameMatching = T("Content items where the value \"{0}\" {1} {2} of the Terms' \"{3}\" property.",
                values.Terms,
                values.Contains ? T("matches") : T("doesn't match"),
                values.Operator == 0 ? T("any") : T("all"),
                values.TermProperty);

            if (values.TermProperty == nameof(TermPart.Id)) return notNameMatching;
            if (values.TermProperty == nameof(TermPart.Name) && !values.TaxonomyAliases.Any())
                return T("{0} {1}",
                    notNameMatching,
                    T("NOTE: The matching property for Terms is their Name, but there is no Taxonomy selected. This will cause the Query to return zero results if the same Term name occurs across multiple Taxonomies and at least two of those are appearing on the given content."));

            var taxonomyNames = _contentManager
                .Query(VersionOptions.Published, "Taxonomy")
                .List()
                .Where(taxonomy => values.TaxonomyAliases.Contains(taxonomy.As<IAliasAspect>().Path))
                .Select(taxonomy => taxonomy.As<ITitleAspect>().Title);

            return T("Content items where the value \"{0}\" {1} {2} of the {3} {4} Terms' \"{5}\" property.",
                values.Terms,
                values.Contains ? T("matches") : T("doesn't match"),
                values.Operator == 0 ? T("any") : T("all"),
                string.Join(", ", taxonomyNames),
                taxonomyNames.Count() > 1 ? T("Taxonomies'") : T("Taxonomy's"),
                values.TermProperty);
        }
    }
}