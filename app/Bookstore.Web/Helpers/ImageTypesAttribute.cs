using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// Validates that an uploaded file is one of the allowed image types.
    /// Replaces the legacy version that used System.Web.HttpPostedFileBase.
    /// </summary>
    public class ImageTypesAttribute : ValidationAttribute
    {
        private readonly string[] _imageTypes;

        public ImageTypesAttribute(string[] imageTypes)
        {
            _imageTypes = imageTypes;
        }

        public override bool IsValid(object value)
        {
            if (value == null) return true;

            if (value is not IFormFile file) return base.IsValid(value);

            var extension = Path.GetExtension(file.FileName);

            return _imageTypes.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be a PNG or JPG image.";
        }
    }
}
