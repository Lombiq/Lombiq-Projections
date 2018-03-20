using Lombiq.Projections.Constants;
using Lombiq.Projections.Helpers;
using Lombiq.Projections.Projections.FieldTypeEditors;
using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    [OrchardFeature(FeatureNames.Fields)]
    public class ContainsContentFieldsFilter : IFilterProvider
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentFieldDriver> _contentFieldDrivers;
        private readonly IEnumerable<INullSafeFieldTypeEditor> _nullSafeFieldTypeEditors;


        public ContainsContentFieldsFilter(
            IContentDefinitionManager contentDefinitionManager,
            IEnumerable<IContentFieldDriver> contentFieldDrivers,
            IEnumerable<INullSafeFieldTypeEditor> nullSafeFieldTypeEditors)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentFieldDrivers = contentFieldDrivers;
            _nullSafeFieldTypeEditors = nullSafeFieldTypeEditors;

            T = NullLocalizer.Instance;
        }


        public Localizer T { get; set; }


        public void Describe(DescribeFilterContext describe)
        {
            foreach (var part in _contentDefinitionManager.ListPartDefinitions())
            {
                if (!part.Fields.Any()) continue;

                var descriptor = describe.For(part.Name + "ContainsContentFields", T("{0} Content Fields (equals or contains)", part.Name.CamelFriendly()), T("Content Fields for {0} (equals or contains).", part.Name.CamelFriendly()));

                foreach (var field in part.Fields)
                {
                    var localField = field;
                    var localPart = part;
                    var drivers = _contentFieldDrivers.Where(x => x.GetFieldInfo().Any(fi => fi.FieldTypeName == localField.FieldDefinition.Name)).ToList();

                    var membersContext = new DescribeMembersContext((storageName, storageType, displayName, description) =>
                    {
                        var propertyName = FieldIndexHelper.GetPropertyName(localPart.Name, field.Name, storageName);

                        descriptor.Element(
                            type: propertyName,
                            name: new LocalizedString(localField.DisplayName + (displayName != null ? ": " + displayName.Text : "") + " (equals or contains)"),
                            description: new LocalizedString(description != null ? description + " (equals or contains)" : "Returns matching items based on a single value (equals) or a list of comma-separated values (contains), but doesn't filter out anything if the filter value is null or empty."),
                            display: context => DisplayFilter(context, localPart.Name + "." + localField.DisplayName),
                            filter: context => ApplyFilter(context, storageType, propertyName),
                            form: TokenizedValueListFilterForm.FormName);
                    });

                    foreach (var driver in drivers) driver.Describe(membersContext);
                }
            }
        }

        public LocalizedString DisplayFilter(FilterContext context, string propertyName)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            return string.IsNullOrEmpty(formValues.ValueString) ?
                T("Inactive filter: You need to define the value to match with.") :
                T("{0} {1} the value \"{2}\".",
                propertyName,
                formValues.EqualsOrContainedIn ? T("is equal to or contained in") : T("is not equal to or not contained in"),
                formValues.ValueString);
        }

        public void ApplyFilter(FilterContext context, Type storageType, string propertyName)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            if (string.IsNullOrEmpty(formValues.ValueString)) return;

            var values = formValues.Values;

            if (!values.Any()) return;

            var aliasName = propertyName.ToSafeName();

            void fieldIndexAlias(IAliasFactory alias) => alias
                .ContentPartRecord<FieldIndexPartRecord>()
                .Property($"{FieldIndexHelper.GetFieldIndexRecordPropertyName(storageType)}", aliasName);

            void propertyNameExpression(IHqlExpressionFactory ex) =>
                ex.Eq(nameof(FieldIndexRecord.PropertyName), propertyName);

            Action<IHqlExpressionFactory> filterExpression;
            if (values.Skip(1).Any())
                filterExpression = ex => ex.And(propertyNameExpression, rex => rex.In("Value", values));
            else filterExpression = ex => ex.And(propertyNameExpression, rex => rex.Eq("Value", values.First()));

            if (formValues.EqualsOrContainedIn) context.Query.Where(fieldIndexAlias, filterExpression);
            else context.Query.Where(fieldIndexAlias, x => x.Not(filterExpression));
        }
    }
}