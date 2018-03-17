using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class TokenizedContentOwnerFilter : IFilterProvider
    {
        public Localizer T { get; set; }


        public TokenizedContentOwnerFilter()
        {
            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            describe.For(nameof(CommonPartRecord), T("Common Part Record"), T("Common Part Record"))
                .Element(nameof(TokenizedContentOwnerFilter), T("Tokenized Content Owner"), T("Content items with matching content Owner ID."),
                    ApplyFilter, DisplayFilter, TokenizedValueListFilterForm.FormName);
        }


        private LocalizedString DisplayFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            return string.IsNullOrEmpty(formValues.ValueString) ?
                T("Inactive filter: You need to define the value to match with.") :
                T("Content items where the Owner's User ID {0} the value \"{1}\".",
                formValues.EqualsOrContainedIn ? T("is equal to or contained in") : T("is not equal to or not contained in"),
                formValues.ValueString);
        }

        private void ApplyFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);
            var values = formValues.Values;

            if (!values.Any()) return;

            // Returning zero results when at least one of the values can't be parsed as an integer.
            if (values.Any(value => !int.TryParse(value.ToString(), out _)))
            {
                context.Query.Where(r => r.ContentPartRecord<CommonPartRecord>(), p => p.Eq("Id", 0));

                return;
            }

            void commmonPartAlias(IAliasFactory alias) => alias.ContentPartRecord<CommonPartRecord>();
            Action<IHqlExpressionFactory> ownerIdExpression;
            if (values.Skip(1).Any())
                ownerIdExpression = expression => expression.In(nameof(CommonPartRecord.OwnerId), values);
            else ownerIdExpression = expression => expression.Eq(nameof(CommonPartRecord.OwnerId), values.First());

            if (formValues.EqualsOrContainedIn) context.Query.Where(commmonPartAlias, ownerIdExpression);
            else context.Query.Where(commmonPartAlias, x => x.Not(ownerIdExpression));
        }
    }
}