using CleanDistrict.Domain;

namespace CleanDistrict.Application;

public record PricingInput(decimal Area, decimal CrewHours, decimal VehicleHours, decimal MaterialsCost, bool IsUrgent);
public record PricingLine(string Description, decimal Quantity, string Unit, decimal UnitPrice, decimal Amount);
public record PricingResult(decimal Subtotal, decimal VatAmount, decimal Total, List<PricingLine> Lines);

public static class PricingCalculator
{
    public static PricingResult Calculate(Tariff tariff, PricingInput input)
    {
        ArgumentNullException.ThrowIfNull(tariff);
        var lines = new List<PricingLine>
        {
            new("Базовая стоимость выезда и организации работ", 1, "услуга", tariff.BasePrice, tariff.BasePrice),
            new("Работы по площади объекта", input.Area, "м²", tariff.AreaRate, Round(input.Area * tariff.AreaRate)),
            new("Работа бригады", input.CrewHours, "чел.-ч", tariff.CrewHourlyRate, Round(input.CrewHours * tariff.CrewHourlyRate)),
            new("Работа спецтехники", input.VehicleHours, "маш.-ч", tariff.VehicleHourlyRate, Round(input.VehicleHours * tariff.VehicleHourlyRate)),
            new("Материалы по тарифу", 1, "компл.", tariff.MaterialCost, tariff.MaterialCost),
            new("Дополнительные материалы", 1, "компл.", input.MaterialsCost, input.MaterialsCost)
        };

        var beforeCoefficient = lines.Sum(x => x.Amount);
        if (input.IsUrgent)
        {
            var urgencyAmount = Round(beforeCoefficient * (tariff.UrgencyCoefficient - 1));
            lines.Add(new($"Коэффициент срочности ×{tariff.UrgencyCoefficient:0.##}", 1, "надбавка", urgencyAmount, urgencyAmount));
        }

        var subtotal = Round(lines.Sum(x => x.Amount));
        var vat = Round(subtotal * tariff.VatRate / 100m);
        return new PricingResult(subtotal, vat, Round(subtotal + vat), lines.Where(x => x.Amount != 0).ToList());
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
