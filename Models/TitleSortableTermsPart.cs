using Orchard.ContentManagement;
using System.Collections.Generic;

namespace Lombiq.Projections.Models
{
    /// <summary>
    /// Similar to <see cref="Orchard.Taxonomies.Models.TermsPart"/>, this class links
    /// content items to Terms using <see cref="TitleSortableTermContentItem"/>,
    /// which includes additional information necessary for sorting content items.
    /// </summary>
    public class TitleSortableTermsPart : ContentPart<TitleSortableTermsPartRecord>
    {
        public IList<TitleSortableTermContentItem> Terms
        {
            get { return Record.Terms; }
            set { Record.Terms = value; }
        }
    }
}