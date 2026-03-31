using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace BG.Web.UI.TagHelpers
{
    /// <summary>
    /// Base class for all BG Tag Helpers.
    /// Provides common functionality for attribute merging and design token application.
    /// </summary>
    public abstract class BaseTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        [SuppressMessage("Usage", "CA2227", Justification = "Framework-bound property for TagHelpers.")]
        public ViewContext ViewContext { get; set; } = default!;

        protected void MergeAttributes(TagHelperOutput output, string className, IDictionary<string, object>? additionalAttributes = null)
        {
            // Add base class
            output.Attributes.AddClass(className);

            // Merge additional attributes if provided
            if (additionalAttributes != null)
            {
                foreach (var attr in additionalAttributes)
                {
                    output.Attributes.SetAttribute(attr.Key, attr.Value);
                }
            }
        }
    }

    public static class TagHelperAttributeListExtensions
    {
        public static void AddClass(this TagHelperAttributeList attributes, string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return;

            if (attributes.TryGetAttribute("class", out var classAttr))
            {
                var existingValue = classAttr.Value?.ToString() ?? string.Empty;
                if (!existingValue.Contains(className))
                {
                    attributes.SetAttribute("class", $"{existingValue} {className}".Trim());
                }
            }
            else
            {
                attributes.SetAttribute("class", className);
            }
        }
    }
}
