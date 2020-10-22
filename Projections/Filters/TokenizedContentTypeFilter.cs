using Lombiq.Projections.Projections.Forms;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class TokenizedContentTypeFilter : IFilterProvider
    {
        public Localizer T { get; set; }


        public TokenizedContentTypeFilter()
        {
            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeFilterContext describe)
        {
            describe.For("Content", T("Content"), T("Content"))
                .Element("TokenizedContentTypes", T("Tokenized Content Types"), T("Tokenized Content Types"),
                    ApplyFilter, DisplayFilter, nameof(TokenizedStringValueListFilterForm));

        }

        public void ApplyFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            if (formValues.Values.Any()) context.Query = context.Query.ForType(formValues.Values);
        }

        public LocalizedString DisplayFilter(FilterContext context)
        {
            var formValues = new TokenizedValueListFilterFormElements(context.State);

            return formValues.Values.Any() ?
                T("Content with its type matching \"{0}\".", formValues.Values) :
                T("Any type of content.");
        }
    }
}