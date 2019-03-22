using Lombiq.Projections.Extensions;
using Lombiq.Projections.Projections.Forms;
using Lombiq.Projections.Services;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class ChainableMemberBindingFilter : IFilterProvider
    {
        private readonly IEnumerable<IChainableMemberBindingProvider> _chainableMemberBindingProviders;

        public Localizer T { get; set; }


        public ChainableMemberBindingFilter(
            IEnumerable<IChainableMemberBindingProvider> chainableMemberBindingProviders)
        {
            _chainableMemberBindingProviders = chainableMemberBindingProviders;

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

            if (string.IsNullOrEmpty(values.ValueString))
                return T("Inactive filter: The tokenized value to match is not defined!");

            return T("The value \"{0}\" matches {1}.{2} with filter relationship \"{3}\".",
                values.ValueString,
                binding.ContentPartRecordType.Name,
                binding.PropertyPath,
                values.FilterRelationshipString);
        }

        public void ApplyFilter(FilterContext context, ChainableMemberBinding binding)
        {
            #region Validation

            // The starting point of the query needs to be a ContentPart(Version)Record.
            if (!binding.ContentPartRecordType.IsContentPartRecord()) return;

            // The property path can't be empty.
            if (string.IsNullOrEmpty(binding.PropertyPath)) return;

            var formValues = new TokenizedValueListFilterFormElements(context.State);

            // If there are no values to filter with, then do nothing.
            if (!formValues.Values.Any()) return;

            #endregion

            #region Preparation

            // The starting point of the reference chain is the ContentPartRecord defined by the binding.
            var currentRecordType = binding.ContentPartRecordType;

            // Unpack the property path defined the binding to be able to find the last property that will represent a 1-to-many connection.
            var propertyNames = binding.PropertyPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            // This will store the names of the properties that 
            var recordReferencePropertyNames = new List<string>();

            // Walking through the property path until we encounter a halting condition.
            foreach (var propertyName in propertyNames)
            {
                // If the current record type is invalid for some reason, then do nothing.
                if (currentRecordType == null) return;

                // Fetching the next property in the path from the current record type.
                var property = currentRecordType.GetProperty(propertyName);

                // If the property is not found, then do nothing.
                if (property == null) return;

                // If a property's type is a generic and it also implements IEnumerable, then it represents a 1-to-many connection.
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetInterfaces().Any(@interface => @interface.Name == typeof(IEnumerable<>).Name))
                {
                    recordReferencePropertyNames.Add(propertyName);
                    currentRecordType = property.PropertyType.GetGenericArguments().FirstOrDefault();
                }
                // If the property doesn't represent a 1-to-many connection,
                // then we'll just use it to filter (although it can still represent a table join for a single record).
                else break;
            }

            #endregion

            var lastRecordReferencePropertyName = recordReferencePropertyNames.Last();

            IAliasFactory baseAlias(IAliasFactory a)
            {
                // Start with a join on the initial ContentPart(Version)Record.
                a = a.ContentPartRecord(binding.ContentPartRecordType);
                // Go through all the properties except the last one - see why at the definion of the alias
                // used for multiple values with "And" relationship.
                foreach (var name in recordReferencePropertyNames.Take(recordReferencePropertyNames.Count - 1))
                    // And then join the current table represented by the property.
                    a = a.Property(name, name.HtmlClassify());

                return a;
            }

            void singleValueAlias(IAliasFactory a) =>
                // This alias will be used if the filter relationship is not "And",
                // so we'll just join the table represented by the last property too.
                a = baseAlias(a).Property(
                    lastRecordReferencePropertyName,
                    lastRecordReferencePropertyName.HtmlClassify());

            Action<IHqlExpressionFactory> expression = e => { };

            var filterPropertyName = string.Join(".", propertyNames.Skip(recordReferencePropertyNames.Count));
            // Building the query is more complex when multiple filter values are present.
            if (formValues.Values.Skip(1).Any())
            {
                switch (formValues.FilterRelationship)
                {
                    /* Filtering on multiple values with an "Or" relationship is very similar to filtering on a single value,
                     * so much so that filtering on a single value could use this logic instead its own,
                     * but it's easier to understand the control flow like this. */
                    case ValueFilterRelationship.Or:
                        expression = e => e.In(filterPropertyName, formValues.Values);

                        context.Query.Where(singleValueAlias, expression);

                        break;
                    /* Here comes the fun part: When filtering on multiple values with an "And" relationship,
                     * each value requires its own unique alias on the last join,
                     * otherwise the query won't give any results. */
                    case ValueFilterRelationship.And:
                        foreach (var value in formValues.Values)
                        {
                            void alias(IAliasFactory a) =>
                                a = baseAlias(a).Property(
                                    lastRecordReferencePropertyName,
                                    $"{lastRecordReferencePropertyName.CamelFriendly()}_{value.ToString().HtmlClassify()}");

                            expression = e => e.Eq(filterPropertyName, value);

                            context.Query.Where(alias, expression);
                        }

                        break;
                    default:
                        break;
                }
            }
            // Otherwise it's a lot simpler, e.g. we don't care about the filter relationship.
            else
            {
                expression = e => e.Eq(filterPropertyName, formValues.Values.First());

                context.Query.Where(singleValueAlias, expression);
            }
        }
    }
}