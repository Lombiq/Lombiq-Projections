using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedContentTypeFilterFormElements
    {
        public string ContentTypes { get; set; }


        public TokenizedContentTypeFilterFormElements(dynamic formState)
        {
            ContentTypes = formState[nameof(ContentTypes)];
        }
    }


    public class TokenizedContentTypeFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }

        public static string FormName = nameof(TokenizedContentTypeFilterForm);


        public TokenizedContentTypeFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context)
        {
            object filterForm(IShapeFactory shape)
            {
                var form = _shapeFactory.Form(
                    Id: FormName,
                    _Terms: _shapeFactory.Textbox(
                        Id: "tokenizedContentTypes", Name: nameof(TokenizedContentTypeFilterFormElements.ContentTypes),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Content Types"),
                        Description: T("The optionally tokenized list of Content Types to filter content items with."))
                    );

                return form;
            }

            context.Form(FormName, filterForm);
        }
    }
}