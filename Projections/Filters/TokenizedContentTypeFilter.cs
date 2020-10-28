using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement.MetaData;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Filters
{
    public class TokenizedContentTypeFilter : IFilterProvider
    {
        private readonly Lazy<IContentDefinitionManager> _contentDefinitionManagerLazy;

        public Localizer T { get; set; }


        public TokenizedContentTypeFilter(Lazy<IContentDefinitionManager> contentDefinitionManagerLazy)
        {
            _contentDefinitionManagerLazy = contentDefinitionManagerLazy;

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
                if (formValues.Matches)
                {
                    context.Query = context.Query.ForType(formValues.Values);
                }
                else
                {
                    var contentTypeNames = _contentDefinitionManagerLazy
                        .Value
                        .ListTypeDefinitions()
                        .Select(typeDefinition => typeDefinition.Name);

                    context.Query = context.Query.ForType(contentTypeNames.Except(formValues.Values.ToList()).ToArray());
                }
            }
        }

        public LocalizedString DisplayFilter(FilterContext context)
        {
            var formValues = new TokenizedStringValueListFilterFormElements(context.State);

            if (formValues.Values.Any())
            {
                return formValues.Matches ?
                    T("Content with its type matching \"{0}\".", formValues.Values) :
                    T("Content without its type matching \"{0}\".", formValues.Values);
            }

            return T("Any type of content.");
        }
    }
}