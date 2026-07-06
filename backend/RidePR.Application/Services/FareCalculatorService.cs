using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;

namespace RidePR.Application.Services;

public class FareCalculatorService
{
    private readonly IFareSettingsRepository _fareRepository;

    public FareCalculatorService(IFareSettingsRepository fareRepository)
    {
        _fareRepository = fareRepository;
    }

    public async Task<decimal> CalculateAsync(decimal distanceKm, decimal durationMinutes)
    {
        return await CalculateAsync(distanceKm, durationMinutes, null);
    }

    public async Task<decimal> CalculateAsync(
        decimal distanceKm,
        decimal durationMinutes,
        Guid? branchId)
    {
        var fare = await _fareRepository.GetActiveAsync(branchId);

        if (fare == null)
            throw new Exception("Nenhuma tarifa ativa encontrada.");

        decimal total = fare.BaseFare > 0 ? fare.BaseFare : fare.MinimumFare;

        // KM excedente
        if (distanceKm > fare.IncludedDistanceKm)
        {
            var extraKm = distanceKm - fare.IncludedDistanceKm;

            total += extraKm * fare.PricePerKm;
        }

        // Tempo
        total += durationMinutes * fare.PricePerMinute;

        // Tarifa dinâmica
        if (fare.DynamicPricing)
        {
            total *= fare.DynamicMultiplier;
        }

        return Math.Round(total, 2);
    }
}
