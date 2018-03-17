using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class TokenizedTitleFilter : IFilterProvider
    {
        public Localizer T { get; set; }


        public TokenizedTitleFilter()
        {
            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            describe.For(nameof(TitlePartRecord), T("Title Part Record"), T("Title Part Record"))
                .Element(nameof(TokenizedTitleFilter), T("Tokenized Title(s)"), T("Content items with matching Title(s)."),
                    ApplyFilter, DisplayFilter, TokenizedValueListFilterForm.FormName);
        }


        private LocalizedString DisplayFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            return string.IsNullOrEmpty(formValues.ValueString) ?
                T("Inactive filter: You need to define the value to match with.") :
                T("Content items where the Title {0} the value \"{1}\".",
                formValues.EqualsOrContainedIn ? T("is equal to or contained in") : T("is not equal to or not contained in"),
                formValues.ValueString);
        }

        private void ApplyFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);
            var values = formValues.Values;

            if (!values.Any()) return;

            void titlePartAlias(IAliasFactory alias) => alias.ContentPartRecord<TitlePartRecord>();
            Action<IHqlExpressionFactory> titleExpression;
            if (values.Skip(1).Any())
                titleExpression = expression => expression.In(nameof(TitlePartRecord.Title), values);
            else titleExpression = expression => expression.Eq(nameof(TitlePartRecord.Title), values.First());

            if (formValues.EqualsOrContainedIn) context.Query.Where(titlePartAlias, titleExpression);
            else context.Query.Where(titlePartAlias, x => x.Not(titleExpression));
        }
    }
}