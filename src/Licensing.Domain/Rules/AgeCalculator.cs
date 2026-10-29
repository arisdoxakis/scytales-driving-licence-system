namespace Licensing.Domain.Rules;

public static class AgeCalculator
{
    public static int AgeOn(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-age)) age--;
        
        return age;
    }
}