using Lombiq.Projections.Extensions;
using Lombiq.Projections.Helpers;
using Lombiq.Projections.Providers;
using Orchard.Localization;
using Orchard.Projections.Descriptors.SortCriterion;
using Orchard.Projections.Providers.SortCriteria;
using Orchard.Projections.Services;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.SortCriteria
{
    public class ChainableMemberBindingSortCriteria : ISortCriterionProvider
    {
        private readonly IEnumerable<IChainableMemberBindingProvider> _chainableMemberBindingProviders;

        public Localizer T { get; set; }


        public ChainableMemberBindingSortCriteria(
            IEnumerable<IChainableMemberBindingProvider> chainableMemberBindingProviders)
        {
            _chainableMemberBindingProviders = chainableMemberBindingProviders;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeSortCriterionContext describe)
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
                        context => ApplySortCriterion(context, binding),
                        context => DisplaySortCriterion(context, binding),
                        SortCriterionFormProvider.FormName
                    );
            }
        }

        public LocalizedString DisplaySortCriterion(SortCriterionContext context, ChainableMemberBinding binding)
        {
            if (!binding.ContentPartRecordType.IsContentPartRecord())
                return T("Inactive sorting: {0} is not a ContentPart(Version)Record!", binding.ContentPartRecordType.Name);

            if (string.IsNullOrEmpty(binding.PropertyPath))
                return T("Inactive filter: The property path is not defined!");

            if (!bool.TryParse(Convert.ToString(context.State.Sort), out bool ascending)) ascending = true;

            return T("Sort items on {0}.{1} {2}.",
                binding.ContentPartRecordType.Name,
                binding.PropertyPath,
                ascending ? T("ascending") : T("descending"));
        }

        public void ApplySortCriterion(SortCriterionContext context, ChainableMemberBinding binding)
        {
            #region Validation

            // The starting point of the query needs to be a ContentPart(Version)Record.
            if (!binding.ContentPartRecordType.IsContentPartRecord()) return;

            // The property path can't be empty.
            if (string.IsNullOrEmpty(binding.PropertyPath)) return;

            #endregion

            var recordListReferencePropertyNames = ChainableMemberBindingHelper
                .GetRecordListReferencePropertyNames(binding.ContentPartRecordType, binding.PropertyPath);

            if (recordListReferencePropertyNames == null) return;

            if (!bool.TryParse(Convert.ToString(context.State.Sort), out bool ascending)) ascending = true;

            var filterPropertyName = string.Join(".", binding.PropertyPath
                .Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(recordListReferencePropertyNames.Count()));


            context.Query.OrderBy(
                alias => ChainableMemberBindingHelper.GetChainableMemberBindingAlias(
                    alias, binding.ContentPartRecordType, recordListReferencePropertyNames, filterPropertyName),
                order =>
                {
                    if (ascending) order.Asc(filterPropertyName);
                    else order.Desc(filterPropertyName);
                });
        }
    }
}