using Lombiq.Projections.Constants;
using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Core.Common.Models;
using Orchard.Environment.Extensions;
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Orchard.Utility.Extensions;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    [OrchardFeature(FeatureNames.Fields)]
    public class TokenizedBooleanFieldFilter : IFilterProvider
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public Localizer T { get; set; }


        public TokenizedBooleanFieldFilter(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            foreach (var part in _contentDefinitionManager.ListPartDefinitions())
            {
                var booleanFields = part.Fields.Where(field => field.FieldDefinition.Name == nameof(BooleanField));

                if (!booleanFields.Any()) continue;

                var descriptor = describe.For(
                    part.Name + "ContentFields",
                    T("{0} Content Fields", part.Name.CamelFriendly()),
                    T("Content Fields for {0}", part.Name.CamelFriendly()));

                foreach (var field in booleanFields)
                    descriptor.Element(
                        nameof(TokenizedBooleanFieldFilter),
                        T("{0}: Tokenized Value", field.DisplayName),
                        T("The tokenized boolean value of the field."),
                        context => ApplyFilter(context, part, field),
                        context => DisplayFilter(context, part, field),
                        nameof(TokenizedStringValueListFilterForm)
                    );
            }
        }

        public void ApplyFilter(FilterContext context, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            string valueString = context.State.Value;

            var formValues = new TokenizedStringValueListFilterFormElements(context.State);
            var values = formValues.Values;

            if (!values.Any()) return;

            // Returning zero results when at least one of the values can't be parsed as a bool.
            if (values.Any(value => !bool.TryParse(value.ToString(), out _)))
            {
                context.Query.Where(r => r.ContentPartRecord<CommonPartRecord>(), p => p.Eq("Id", 0));

                return;
            }

            var booleanValues = values.Select(value => bool.Parse(value.ToString()) ? 1 : 0).ToArray();
            var propertyName = $"{part.Name}.{field.Name}.";

            // Using an alias with the Join so that different filters on the same type of field won't collide.
            void relationship(IAliasFactory x) => x
                .ContentPartRecord<FieldIndexPartRecord>()
                .Property(nameof(FieldIndexPartRecord.IntegerFieldIndexRecords), propertyName.ToSafeName());

            Action<IHqlExpressionFactory> valueExpression;
            if (values.Skip(1).Any())
                valueExpression = expression => expression.In("Value", booleanValues);
            else valueExpression = expression => expression.Eq("Value", booleanValues.First());

            Action<IHqlExpressionFactory> equalsOrNotExpression;
            if (formValues.Matches)
                equalsOrNotExpression = valueExpression;
            else equalsOrNotExpression = expression => expression.Not(valueExpression);

            context.Query.Where(relationship, x => x.And(y => y.Eq("PropertyName", propertyName), equalsOrNotExpression));
        }

        public LocalizedString DisplayFilter(FilterContext context, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            var formValues = new TokenizedStringValueListFilterFormElements(context.State);

            return string.IsNullOrEmpty(formValues.ValueString) ?
                T("Inactive filter: Undefined value for {0}.{1}.", part.Name, field.Name) :
                T("{0}.{1} {2} \"{3}\".",
                    part.Name,
                    field.Name,
                    formValues.Matches ? T("matches") : T("doesn't match"),
                    formValues.ValueString);
        }
    }
}