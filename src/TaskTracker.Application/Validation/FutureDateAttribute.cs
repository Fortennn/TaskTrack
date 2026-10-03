using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute()
        : base("The {0} field must be a date in the future.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            DateTime dateTime => dateTime > DateTime.UtcNow,
            DateTimeOffset dateTimeOffset => dateTimeOffset > DateTimeOffset.UtcNow,
            _ => false
        };
    }
}
