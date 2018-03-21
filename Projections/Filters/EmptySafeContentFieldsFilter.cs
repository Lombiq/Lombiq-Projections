using Lombiq.Projections.Constants;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.FieldTypeEditors;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    /// <summary>
    /// An almost identical copy of <see cref="Orchard.Projections.Providers.Filters.ContentFieldsFilter"/>
    /// from the Orchard.Projections module, except that the filter will not be applied
    /// if the filter value evaluates to empty string (e.g. when using a value from the Query String).
    /// </summary>
    [OrchardFeature(FeatureNames.Fields)]
    [OrchardSuppressDependency("Orchard.Projections.Providers.Filters.ContentFieldsFilter")]
    public class EmptySafeContentFieldsFilter : IFilterProvider
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentFieldDriver> _contentFieldDrivers;
        private readonly IEnumerable<IFieldTypeEditor> _fieldTypeEditors;


        public EmptySafeContentFieldsFilter(
            IContentDefinitionManager contentDefinitionManager,
            IEnumerable<IContentFieldDriver> contentFieldDrivers,
            IEnumerable<IFieldTypeEditor> fieldTypeEditors)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentFieldDrivers = contentFieldDrivers;
            _fieldTypeEditors = fieldTypeEditors;

            T = NullLocalizer.Instance;
        }


        public Localizer T { get; set; }


        public void Describe(DescribeFilterContext describe)
        {
            foreach (var part in _contentDefinitionManager.ListPartDefinitions())
            {
                if (!part.Fields.Any()) continue;

                var descriptor = describe.For(part.Name + "ContentFields", T("{0} Content Fields", part.Name.CamelFriendly()), T("Content Fields for {0}.", part.Name.CamelFriendly()));

                foreach (var field in part.Fields)
                {
                    var localField = field;
                    var localPart = part;
                    var drivers = _contentFieldDrivers.Where(x => x.GetFieldInfo().Any(fi => fi.FieldTypeName == localField.FieldDefinition.Name)).ToList();

                    var membersContext = new DescribeMembersContext((storageName, storageType, displayName, description) =>
                    {
                        // Look for a compatible field type editor.
                        var fieldTypeEditor = _fieldTypeEditors.FirstOrDefault(x => x.CanHandle(storageType));

                        if (fieldTypeEditor == null) return;

                        descriptor.Element(
                            type: localPart.Name + "." + localField.Name + "." + storageName,
                            name: new LocalizedString(localField.DisplayName + (displayName != null ? ": " + displayName.Text : "") + ""),
                            description: new LocalizedString(description.Text ?? "No filter will be applied if the filter value evaluates to empty string."),
                            filter: context => ApplyFilter(context, fieldTypeEditor, storageName, storageType, localPart, localField),
                            display: context => fieldTypeEditor.DisplayFilter(localPart.Name + "." + localField.DisplayName, storageName, context.State),
                            form: fieldTypeEditor.FormName);
                    });

                    foreach (var driver in drivers) driver.Describe(membersContext);
                }
            }
        }

        public void ApplyFilter(FilterContext context, IFieldTypeEditor fieldTypeEditor, string storageName, Type storageType, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            // This early return is the only difference compared to the original ContentFieldsFilter.
            if (string.IsNullOrEmpty(Convert.ToString(context.State.Value))) return;

            var propertyName = string.Join(".", part.Name, field.Name, storageName ?? "");

            // Use an alias with the generated property name, so that two filters on the same Field Type won't collide.
            var relationship = fieldTypeEditor.GetFilterRelationship(propertyName.ToSafeName());

            // Generate the predicate based on the editor, which has been used.
            Action<IHqlExpressionFactory> predicate = fieldTypeEditor.GetFilterPredicate(context.State);
            // Combines the predicate with a filter on the specific property name of the storage, as implemented in FieldIndexService.
            void andPredicate(IHqlExpressionFactory x) => x.And(y => y.Eq(nameof(FieldIndexRecord.PropertyName), propertyName), predicate);

            // Apply where clause.
            context.Query = context.Query.Where(relationship, andPredicate);
        }
    }
}