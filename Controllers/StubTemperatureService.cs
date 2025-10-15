using Models.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Controllers;

internal class StubTemperatureService : ITemperatureService
{
    private readonly TemperatureValidationResult _result;

    public StubTemperatureService(TemperatureValidationResult result)
    {
        _result = result;
    }

    public Task<TemperatureValidationResult> ValidateAsync(TemperatureReading reading, CancellationToken ct)
    {
        return Task.FromResult(_result);
    }

    public Task<TemperatureValidationResult> CheckThresholdAsync(TemperatureReading reading, CancellationToken cancellationToken)
    {
        return Task.FromResult(_result);
    }
}