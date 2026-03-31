using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BG.Web.UI.TagHelpers
{
    [HtmlTargetElement("bg-button")]
    public class BgButtonTagHelper : BaseTagHelper
    {
        /// <summary>
        /// Visual variant: primary, secondary, danger, outline-primary, etc.
        /// Defaults to primary.
        /// </summary>
        public string Variant { get; set; } = "primary";

        /// <summary>
        /// Sizing: sm, lg. Defaults to medium.
        /// </summary>
        public string? Size { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "button";
            
            // Apply base button class
            MergeAttributes(output, "bg-btn");

            // Apply variant
            if (!string.IsNullOrWhiteSpace(Variant))
            {
                MergeAttributes(output, $"bg-btn-{Variant}");
            }

            // Apply size
            if (!string.IsNullOrWhiteSpace(Size))
            {
                MergeAttributes(output, $"bg-btn-{Size}");
            }

            // Ensure type="button" if not specified to prevent accidental sumbits
            if (!output.Attributes.ContainsName("type"))
            {
                output.Attributes.SetAttribute("type", "button");
            }
        }
    }
}
