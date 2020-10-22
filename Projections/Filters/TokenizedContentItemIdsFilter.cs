using Lombiq.Projections.Projections.Forms;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using Piedone.HelpfulLibraries.Utilities;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class TokenizedContentItemIdsFilter : IFilterProvider
    {
        public Localizer T { get; set; }


        public TokenizedContentItemIdsFilter()
        {
            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            describe.For("Content", T("Content Item"), T("Content Item"))
                .Element(nameof(TokenizedContentItemIdsFilter), T("Tokenized Content Item Id"), T("Content items with matching content item Id."),
                    ApplyFilter, DisplayFilter, nameof(TokenizedStringValueListFilterForm));
        }


        private LocalizedString DisplayFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            return string.IsNullOrEmpty(formValues.ValueString) ?
                T("Inactive filter: You need to define the value to match with.") :
                T("Content item Id {0} the value \"{1}\".",
                formValues.Matches ? T("matches") : T("doesn't match"),
                formValues.ValueString);
        }

        private void ApplyFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);
            var values = formValues.Values;

            if (!values.Any()) return;

            var ids = values
                .Where(value => int.TryParse(value.ToString(), out var _))
                .Select(value => int.Parse(value.ToString()));

            if (formValues.Matches) context.Query.WhereIdIn(ids);
            else context.Query.WhereIdNotIn(ids);
        }
    }
}