using System.ComponentModel.DataAnnotations;

public class NoSpacesAttribute : ValidationAttribute

{
    public NoSpacesAttribute()
    {
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value != null)
        {
            if (((string)value).Contains(" "))
            {
                return new ValidationResult("Spaces are not allowed");
            }
        }

        return ValidationResult.Success;
    }
}