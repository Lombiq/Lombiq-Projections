using Lombiq.Projections.Extensions;
using Lombiq.Projections.Helpers;
using Lombiq.Projections.Projections.Forms;
using Lombiq.Projections.Providers;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using Orchard.Services;
using Orchard.Utility.Extensions;
using Piedone.HelpfulLibraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class ChainableMemberBindingFilter : IFilterProvider
    {
        private readonly IEnumerable<IChainableMemberBindingProvider> _chainableMemberBindingProviders;
        private readonly IJsonConverter _jsonConverter;

        public Localizer T { get; set; }


        public ChainableMemberBindingFilter(
            IEnumerable<IChainableMemberBindingProvider> chainableMemberBindingProviders,
            IJsonConverter jsonConverter)
        {
            _chainableMemberBindingProviders = chainableMemberBindingProviders;
            _jsonConverter = jsonConverter;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            var bindings = _chainableMemberBindingProviders
                .SelectMany(provider => provider.GetChainableMemberBindings())
                .Distinct(new ChainableMemberBindingProviderEqualityComparer());

            var groupedBindings = bindings.GroupBy(binding => binding.ContentPartRecordType).ToDictionary(group => group.Key);

            foreach (var contentPartRecordType in groupedBindings.Keys)
            {
                var descriptor = describe.For(
                    contentPartRecordType.Name,
                    new LocalizedString(contentPartRecordType.Name.CamelFriendly()),
                    T("{0} chainable members", contentPartRecordType.Name));

                foreach (var binding in groupedBindings[contentPartRecordType])
                    descriptor.Element(binding.PropertyPath, binding.DisplayName, binding.Description,
                        context => ApplyFilter(context, binding),
                        context => DisplayFilter(context, binding),
                        TokenizedValueListFilterForm.FormName
                    );
            }
        }

        public LocalizedString DisplayFilter(FilterContext context, ChainableMemberBinding binding)
        {
            if (!binding.ContentPartRecordType.IsContentPartRecord())
                return T("Inactive filter: {0} is not a ContentPart(Version)Record!", binding.ContentPartRecordType.Name);

            if (string.IsNullOrEmpty(binding.PropertyPath))
                return T("Inactive filter: The property path is not defined!");

            var values = new TokenizedValueListFilterFormElements(context.State);

            return T("The value \"{0}\" {1} {2}.{3} with filter relationship \"{4}\".",
                string.IsNullOrEmpty(values.ValueString) ? T("{empty}").Text : values.ValueString,
                values.Matches ? T("match(es)") : T("do(es)n't match"),
                binding.ContentPartRecordType.Name,
                binding.PropertyPath,
                string.IsNullOrEmpty(values.FilterRelationshipString) ? values.FilterRelationship.ToString() : values.FilterRelationshipString);
        }

        public void ApplyFilter(FilterContext context, ChainableMemberBinding binding)
        {
            #region Validation

            // The starting point of the query needs to be a ContentPart(Version)Record.
            if (!binding.ContentPartRecordType.IsContentPartRecord()) return;

            // The property path can't be empty.
            if (string.IsNullOrEmpty(binding.PropertyPath)) return;

            var formValues = new TokenizedValueListFilterFormElements(context.State);

            var values = formValues.GetValuesFromJsonString(_jsonConverter);

            #endregion

            var recordListReferencePropertyNames = ChainableMemberBindingHelper
                .GetRecordListReferencePropertyNames(binding.ContentPartRecordType, binding.PropertyPath);

            if (recordListReferencePropertyNames == null) return;

            var filterPropertyName = string.Join(".", binding.PropertyPath
                .Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(recordListReferencePropertyNames.Count()));

            void getAlias(IAliasFactory alias, string join = "", string value = "") =>
                ChainableMemberBindingHelper.GetChainableMemberBindingAlias(
                    alias, binding.ContentPartRecordType, recordListReferencePropertyNames, filterPropertyName, join, value);

            if (values.Any())
            {
                switch (formValues.FilterRelationship)
                {
                    // Filtering on multiple values with an "Or" relationship can use the same alias.
                    case ValueFilterRelationship.Or:
                        context.Query.Where(
                            a => getAlias(a),
                            e => e.AggregateOr((ex, value, property) =>
                                formValues.GetStringOperatorFilterExpression(ex, property, value.ToString()), filterPropertyName, values));

                        break;
                    /* When filtering on multiple values with an "And" relationship, each value requires its own
                     * unique alias on the last join, otherwise the query won't give any results. */
                    case ValueFilterRelationship.And:
                        foreach (var value in values)
                            context.Query.Where(
                                a => getAlias(a, value: value),
                                e => formValues.GetStringOperatorFilterExpression(e, filterPropertyName, value));

                        break;
                    default:
                        break;
                }
            }
        }
    }
}