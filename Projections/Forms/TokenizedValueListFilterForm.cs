using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedValueListFilterFormElements
    {
        public bool EqualsOrContainedIn { get; set; }
        public string ValueString { get; set; }
        public object[] Values { get; set; }


        public TokenizedValueListFilterFormElements(dynamic formState)
        {
            EqualsOrContainedIn = formState[nameof(EqualsOrContainedIn)] ?? true;
            ValueString = formState[nameof(ValueString)];
            Values = string.IsNullOrEmpty(ValueString) ?
                new object[] { } :
                ValueString
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => (object)value.ToString().Trim())
                    .Where(value => value.ToString() != "")
                    .ToArray();
        }
    }

    public class TokenizedValueListFilterForm : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }

        public static string FormName = nameof(TokenizedValueListFilterForm);


        public TokenizedValueListFilterForm(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        public void Describe(DescribeContext context) =>
            context.Form(FormName, shape =>
                _shapeFactory.Form(
                    Id: FormName,
                    _EqualsOrContainedIn: _shapeFactory.FieldSet(
                        _EqualsOrContained: _shapeFactory.Radio(
                            Id: "equalsToOrContainedIn", Name: nameof(TokenizedValueListFilterFormElements.EqualsOrContainedIn),
                            Title: T("Value is equal to or contained in"), Value: "true", Checked: true),
                        _NotEqualsOrNotContained: _shapeFactory.Radio(
                            Id: "notEqualsToOrNotContainedIn", Name: nameof(TokenizedValueListFilterFormElements.EqualsOrContainedIn),
                            Title: T("Value is not equal to or not contained in"), Value: "false")),
                    _Value: _shapeFactory.Textbox(
                        Id: "valueString", Name: nameof(TokenizedValueListFilterFormElements.ValueString),
                        Classes: new[] { "text", "medium", "tokenized" },
                        Title: T("Value(s)"),
                        Description: T("The optionally tokenized comma-separated list of values."))));
    }
}