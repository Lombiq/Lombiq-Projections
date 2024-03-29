﻿using Lombiq.Projections.Projections.Forms;
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
            var formValues = new TokenizedStringValueListFilterFormElements(context.State);

            if (formValues.Values.Any())
            {
                if (formValues.Matches) context.Query.ForType(formValues.Values);
                else context.Query.Where(
                    alias => alias.ContentItem(),
                    filter => filter.Not(ex => ex.InG("ContentType.Name", formValues.Values)));
            }
        }

        public LocalizedString DisplayFilter(FilterContext context)
        {
            var formValues = new TokenizedStringValueListFilterFormElements(context.State);

            return formValues.Values.Any()
                ? formValues.Matches
                    ? T("Content with its type matching \"{0}\".", formValues.Values)
                    : T("Content without its type matching \"{0}\".", formValues.Values)
                : T("Any type of content (inactive filter).");
        }
    }
}