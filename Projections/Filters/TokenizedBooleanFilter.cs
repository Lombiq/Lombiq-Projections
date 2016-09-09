using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Core.Common.Models;
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
    public class TokenizedBooleanFilter : IFilterProvider
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public Localizer T { get; set; }


        public TokenizedBooleanFilter(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            foreach (var part in _contentDefinitionManager.ListPartDefinitions())
            {
                var booleanFields = part.Fields.Where(field => field.FieldDefinition.Name == typeof(BooleanField).Name);

                if (!booleanFields.Any()) continue;

                var descriptor = describe.For(
                    part.Name + "ContentFields",
                    T("{0} Content Fields", part.Name.CamelFriendly()),
                    T("Content Fields for {0}", part.Name.CamelFriendly()));

                foreach (var field in booleanFields)
                    descriptor.Element(
                        typeof(TokenizedBooleanFilter).Name,
                        T("{0}: Tokenized Value", field.DisplayName),
                        T("The tokenized boolean value of the field."),
                        context => ApplyFilter(context, part, field),
                        context => DisplayFilter(context, part, field),
                        TokenizedBooleanFilterForm.FormName
                    );
            }
        }

        public void ApplyFilter(FilterContext context, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            string valueString = context.State.Value;

            if (!string.IsNullOrEmpty(valueString))
            {
                bool value;

                // Returning zero results when the value can't be parsed as a bool.
                if (!bool.TryParse(valueString, out value))
                {
                    context.Query.Where(r => r.ContentPartRecord<CommonPartRecord>(), p => p.Eq("Id", 0));

                    return;
                }

                var propertyName = $"{part.Name}.{field.Name}.";

                // Using an alias with the Join so that different filters on the same type of field won't collide.
                Action<IAliasFactory> relationship = x => x
                    .ContentPartRecord<FieldIndexPartRecord>()
                    .Property(nameof(FieldIndexPartRecord.IntegerFieldIndexRecords), propertyName.ToSafeName());

                Action<IHqlExpressionFactory> predicate = x => x.Eq("Value", value ? (long)1 : (long)0);

                Action<IHqlExpressionFactory> andPredicate = x => x.And(y => y.Eq("PropertyName", propertyName), predicate);

                context.Query.Where(relationship, andPredicate);
            }
        }

        public LocalizedString DisplayFilter(FilterContext context, ContentPartDefinition part, ContentPartFieldDefinition field)
        {
            string value = context.State.Value;

            return string.IsNullOrEmpty(value) ?
                T("Inactive filter: Undefined value for {0}.{1}.", part.Name, field.Name) :
                T("{0}.{1} equals \"{2}\".", part.Name, field.Name, value);
        }
    }
}