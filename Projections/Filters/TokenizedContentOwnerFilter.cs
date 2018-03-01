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
                    ApplyFilter, DisplayFilter, TokenizedContentOwnerFilterForm.FormName);
        }


        private LocalizedString DisplayFilter(FilterContext context)
        {
            var values = new TokenizedContentOwnerFilterFormElements(context.State);

            return string.IsNullOrEmpty(values.Value) ?
                T("Inactive filter: You need to define the value to match with.") :
                T("Content items where the Owner's User ID {0} the value \"{1}\".",
                values.EqualsOrContainedIn ? T("is equal to or contained in") : T("is not equal to or not contained in"),
                values.Value);
        }

        private void ApplyFilter(FilterContext context)
        {
            var values = new TokenizedContentOwnerFilterFormElements(context.State);

            if (string.IsNullOrEmpty(values.Value)) return;

            var userIds = values.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (!userIds.Any()) return;

            void commmonPartAlias(IAliasFactory alias) => alias.ContentPartRecord<CommonPartRecord>();
            void ownerIdExpression(IHqlExpressionFactory expression) => expression.InG(nameof(CommonPartRecord.OwnerId), userIds);

            if (values.EqualsOrContainedIn) context.Query.Where(commmonPartAlias, ownerIdExpression);
            else context.Query.Where(commmonPartAlias, x => x.Not(ownerIdExpression));
        }
    }
}