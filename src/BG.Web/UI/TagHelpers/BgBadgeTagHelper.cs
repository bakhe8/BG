using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BG.Web.UI.TagHelpers
{
    [HtmlTargetElement("bg-badge")]
    public class BgBadgeTagHelper : BaseTagHelper
    {
        /// <summary>
        /// Visual variant: success, pending, warning, info, error.
        /// </summary>
        public string Variant { get; set; } = "info";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            
            // Apply base badge class
            MergeAttributes(output, "bg-badge");

            // Apply variant-specific status class
            if (!string.IsNullOrWhiteSpace(Variant))
            {
                MergeAttributes(output, $"bg-status-{Variant}");
            }
        }
    }
}
