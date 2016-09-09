using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.FieldTypeEditors;
// Intellisense is not intelligent enough (yet) to recognise usings that are added only for class refernces in comments.
using Orchard.Projections.Services;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    /// <summary>
    /// An almost identical copy of <see cref="ContentFieldsFilter"/> from the Orchard.Projections module,
    /// except that items with null value won't be excluded from the result when the filter value is empty.
    /// This is necessary, because values from string-based fields (e.g. TextField, InputField) are indexed
    /// using <see cref="FieldIndexService"/> as null when the value of the field is an empty string.
    /// </summary>
    public class NullSafeContentFieldsFilter : IFilterProvider
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentFieldDriver> _contentFieldDrivers;
        private readonly IEnumerable<IFieldTypeEditor> _fieldTypeEditors;


        public NullSafeContentFieldsFilter(
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

                var descriptor = describe.For(part.Name + "ContentFieldsNullSafe", T("{0} Content Fields (null-safe)", part.Name.CamelFriendly()), T("Content Fields for {0} (null-safe).", part.Name.CamelFriendly()));

                foreach (var field in part.Fields)
                {
                    var localField = field;
                    var localPart = part;
                    var drivers = _contentFieldDrivers.Where(x => x.GetFieldInfo().Any(fi => fi.FieldTypeName == localField.FieldDefinition.Name)).ToList();

                    var membersContext = new DescribeMembersContext((storageName, storageType, displayName, description) =>
                    {
                        // Look for a compatible field type editor.
                        IFieldTypeEditor fieldTypeEditor = _fieldTypeEditors.FirstOrDefault(x => x.CanHandle(storageType));

                        if (fieldTypeEditor == null) return;

                        descriptor.Element(
                            type: localPart.Name + "." + localField.Name + "." + storageName,
                            name: new LocalizedString(localField.DisplayName + (displayName != null ? ":" + displayName.Text : "") + " (null-safe)"),
                            description: new LocalizedString(description != null ? description + " (null-safe)" : "No filter will be applied if the filter value is empty, so items indexed as null in the database won't be excluded from the result."),
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
            // The filter has to be applied only if there's an actual value (not null or empty) to filter with.
            if (context.State.Value != null && context.State.Value != "")
            {
                var propertyName = string.Join(".", part.Name, field.Name, storageName ?? "");

                // Use an alias with the generated property name, so that two filters on the same Field Type won't collide.
                var relationship = fieldTypeEditor.GetFilterRelationship(propertyName.ToSafeName());

                // Generate the predicate based on the editor, which has been used.
                Action<IHqlExpressionFactory> predicate = fieldTypeEditor.GetFilterPredicate(context.State);
                // Combines the predicate with a filter on the specific property name of the storage, as implemented in FieldIndexService.
                Action<IHqlExpressionFactory> andPredicate = x => x.And(y => y.Eq("PropertyName", propertyName), predicate);

                // Apply where clause.
                context.Query = context.Query.Where(relationship, andPredicate);
            }
        }
    }
}