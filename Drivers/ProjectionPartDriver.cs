using Lombiq.Projections.Constants;
using Orchard.ContentManagement.Drivers;
using Orchard.Fields.Fields;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Piedone.HelpfulLibraries.Contents;

namespace Lombiq.Projections.Drivers
{
    public class ProjectionPartDriver : ContentPartDriver<ProjectionPart>
    {
        private readonly IProjectionManagerExtension _projectionManager;


        public ProjectionPartDriver(IProjectionManagerExtension projectionManager)
        {
            _projectionManager = projectionManager;
        }


        protected override DriverResult Display(ProjectionPart part, string displayType, dynamic shapeHelper) =>
            part.AsField<BooleanField>(nameof(ProjectionPart), FieldNames.ProjectionPart.ShowResultCount)?.Value ?? false ?
                ContentShape("Parts_ProjectionPart_ResultCount", () =>
                    shapeHelper.Parts_ProjectionPart_ResultCount(ResultCount: _projectionManager.GetCount(part.Record.QueryPartRecord.Id))) :
                null;
    }
}