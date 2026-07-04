namespace RidePR.Domain.Entities;

public class FareSettings
{
    public Guid Id { get; set; }

    // Nome da categoria
    public string Name { get; set; } = "Econômico";

    // Valor mínimo da corrida
    public decimal MinimumFare { get; set; }

    // Quantos km estão incluídos na tarifa mínima
    public decimal IncludedDistanceKm { get; set; }

    // Valor por km excedente
    public decimal PricePerKm { get; set; }

    // Valor por minuto
    public decimal PricePerMinute { get; set; }

    // Tempo de espera gratuito
    public int FreeWaitingMinutes { get; set; }

    // Valor por minuto de espera
    public decimal WaitingMinutePrice { get; set; }

    // Taxa de cancelamento
    public decimal CancellationFee { get; set; }

    // Comissão da plataforma (%)
    public decimal PlatformCommission { get; set; }

    // Tarifa dinâmica habilitada
    public bool DynamicPricing { get; set; }

    // Multiplicador da tarifa dinâmica
    public decimal DynamicMultiplier { get; set; } = 1;

    // Ativa
    public bool Active { get; set; } = true;
}