using System.ComponentModel.DataAnnotations;
using AgroConecta.Api.DTOs.Requests;

namespace AgroConecta.Api.Infrastructure;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new List<ValidationResult>();
        foreach (var arg in context.Arguments.Where(a => a?.GetType().Namespace == typeof(UserRequest).Namespace))
            Validator.TryValidateObject(arg!, new ValidationContext(arg!), errors, true);
        var satellite = context.Arguments.OfType<SatelliteDataRequest>().FirstOrDefault();
        if (satellite is not null)
        {
            if (satellite.NdviMin > satellite.NdviAverage || satellite.NdviAverage > satellite.NdviMax)
                errors.Add(new("Exige NDVI mínimo <= médio <= máximo.", new[] { "NdviAverage" }));
            if (satellite.CaptureDate == default || satellite.CaptureDate > DateOnly.FromDateTime(DateTime.UtcNow))
                errors.Add(new("Data de captura deve ser válida e não futura.", new[] { "CaptureDate" }));
        }
        if (errors.Count > 0)
            return Results.ValidationProblem(errors.SelectMany(e => e.MemberNames.DefaultIfEmpty("body").Select(m => (m, e.ErrorMessage!)))
                .GroupBy(e => e.m).ToDictionary(g => g.Key, g => g.Select(e => e.Item2).ToArray()));
        return await next(context);
    }
}
