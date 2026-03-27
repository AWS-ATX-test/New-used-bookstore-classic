using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// Validates that an uploaded file does not exceed a maximum size.
    /// Replaces the legacy version that used System.Web.HttpPostedFileBase.
    /// </summary>
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;

        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        public override bool IsValid(object value)
        {
            if (value == null) return true;

            if (value is not IFormFile file) return base.IsValid(value);

            return file.Length <= _maxFileSize;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} cannot exceed {_maxFileSize.ToStorageSize()}";
        }
    }
}
