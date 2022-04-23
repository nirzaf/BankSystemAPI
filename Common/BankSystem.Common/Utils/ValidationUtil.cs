namespace BankSystem.Common.Utils
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public static class ValidationUtil
    {
        public static bool IsObjectValid(object model)
        {
            ValidationContext validationContext = new ValidationContext(model);
            List<ValidationResult> validationResults = new List<ValidationResult>();

            return Validator.TryValidateObject(model, validationContext, validationResults,
                true);
        }
    }
}