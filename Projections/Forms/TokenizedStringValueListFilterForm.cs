using Orchard.DisplayManagement;
using Orchard.Forms.Services;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedStringValueListFilterForm : TokenizedValueListFilterFormBase
    {
        private readonly dynamic _shapeFactory;


        public TokenizedStringValueListFilterForm(IShapeFactory shapeFactory) : base(shapeFactory)
        {
            _shapeFactory = shapeFactory;
        }


        public override void Describe(DescribeContext context) =>
            context.Form(nameof(TokenizedStringValueListFilterForm), shape =>
                _shapeFactory.Form(
                    Id: nameof(TokenizedStringValueListFilterForm),
                    _MatchOrNoMatch: GetMatchRadioFieldSet(),
                    _StringOperator: _shapeFactory.FieldSet(
                        _Equals: _shapeFactory.Radio(
                            Id: "equals", Name: nameof(TokenizedValueListFilterFormElements.StringOperatorString),
                            Title: T("Value(s) equal(s)"), Value: StringOperator.Equals, Checked: true),
                        _ContainedIn: _shapeFactory.Radio(
                            Id: "containedIn", Name: nameof(TokenizedValueListFilterFormElements.StringOperatorString),
                            Title: T("Value(s) is/are contained in"), Value: StringOperator.ContainedIn)),
                    _Value: GetValueTextbox(),
                    _Relationship: GetFilterRelationshipTextbox()));
    }


    public enum StringOperator
    {
        Equals,
        ContainedIn
    }
}