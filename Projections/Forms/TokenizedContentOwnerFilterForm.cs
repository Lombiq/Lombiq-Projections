using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedContentOwnerFilterFormElements
    {
        public bool EqualsOrContainedIn { get; set; }
        public string Value { get; set; }


        public TokenizedContentOwnerFilterFormElements(dynamic formState)
        {
            EqualsOrContainedIn = formState[nameof(EqualsOrContainedIn)] ?? true;
            Value = formState[nameof(Value)];
        }
    }

    public class TokenizedContentOwnerFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }

        public static string FormName = nameof(TokenizedContentOwnerFilterForm);


        public TokenizedContentOwnerFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context) =>
            context.Form(FormName, shape =>
                _shapeFactory.Form(
                    Id: FormName,
                    _OwnerHint: _shapeFactory.InputHint(Description: T("The content item's Owner's User ID")),
                    _EqualsOrContainedIn: _shapeFactory.FieldSet(
                        _EqualsOrContained: _shapeFactory.Radio(
                            Id: "equalsOrContainedIn", Name: nameof(TokenizedContentOwnerFilterFormElements.EqualsOrContainedIn),
                            Title: T("Is equal to or contained in"), Value: "true", Checked: true),
                        _NotEqualsOrNotContained: _shapeFactory.Radio(
                            Id: "notEqualsOrNotContainedIn", Name: nameof(TokenizedContentOwnerFilterFormElements.EqualsOrContainedIn),
                            Title: T("Is not equal to or not contained in"), Value: "false")),
                    _Value: _shapeFactory.Textbox(
                        Id: "value", Name: nameof(TokenizedContentOwnerFilterFormElements.Value),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Value"),
                        Description: T("The optionally tokenized comma-separated list of User IDs."))));
    }
}