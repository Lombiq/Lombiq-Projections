using Lombiq.Projections.Projections.Forms;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using System;
using System.Linq;
using IFilterProvider = Orchard.Projections.Services.IFilterProvider;

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
                    ApplyFilter, DisplayFilter, TokenizedContentTypeFilterForm.FormName);

        }

        public void ApplyFilter(FilterContext context)
        {
            var values = new TokenizedContentTypeFilterFormElements(context.State);

            if (!string.IsNullOrEmpty(values.ContentTypes))
                context.Query = context.Query.ForType(values.ContentTypes
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(type => type.Trim()).ToArray());
        }

        public LocalizedString DisplayFilter(FilterContext context)
        {
            var values = new TokenizedContentTypeFilterFormElements(context.State);

            return string.IsNullOrEmpty(values.ContentTypes) ?
                T("Any type of content.") :
                T("Content with the type matching with \"{0}\".", values.ContentTypes);
        }
    }
}