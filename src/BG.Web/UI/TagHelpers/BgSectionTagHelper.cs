using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BG.Web.UI.TagHelpers
{
    [HtmlTargetElement("bg-section")]
    public class BgSectionTagHelper : BaseTagHelper
    {
        public string? Title { get; set; }
        
        public string? Icon { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "section";
            
            // Apply base section architecture class
            MergeAttributes(output, "bg-section-shell");

            if (!string.IsNullOrWhiteSpace(Title))
            {
                var header = $@"
                    <header class='bg-section-header'>
                        {(string.IsNullOrEmpty(Icon) ? "" : $"<i class='{Icon}'></i>")}
                        <h3 class='bg-section-title'>{Title}</h3>
                    </header>";
                
                output.PreContent.SetHtmlContent(header);
            }

            // Wrap the inner content (after header) in the container div
            output.PreContent.AppendHtml("<div class='bg-section-container'>");
            output.PostContent.SetHtmlContent("</div>");
        }
    }
}
