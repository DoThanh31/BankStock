using System.ComponentModel.DataAnnotations;
using BankStock.Web.Services;
using BankStock.Web.Validation;

namespace BankStock.Tests.Services;

public class TradingRulesTests
{
    [Theory]
    [InlineData(90_000_000, 0.15, 135_000)]
    [InlineData(8_370_000, 0.15, 12_555)]
    [InlineData(1_000, 0.15, 2)]   // 1,5 → làm tròn lên (AwayFromZero)
    [InlineData(100, 0.15, 0)]
    [InlineData(90_000_000, 0, 0)]
    public void CalculateFee_RoundsToWholeDong(decimal value, decimal feePercentage, decimal expected)
    {
        Assert.Equal(expected, TradingRules.CalculateFee(value, feePercentage));
    }

    [Theory]
    [InlineData("HOSE", 73_408, 78_500, 68_300)]   // 78.546,56 / 68.269,44 → bước 100đ
    [InlineData("HOSE", 20_000, 21_400, 18_600)]   // bước 50đ
    [InlineData("HOSE", 9_500, 10_150, 8_840)]     // trần bước 50đ, sàn bước 10đ
    [InlineData("HNX", 25_000, 27_500, 22_500)]    // biên độ 10 %
    [InlineData("UPCOM", 12_345, 14_100, 10_500)]  // biên độ 15 %
    public void CalculateBand_RoundsCeilingDownAndFloorUpToTickSize(string exchange, decimal refPrice, decimal ceiling, decimal floor)
    {
        Assert.Equal((ceiling, floor), PriceRules.CalculateBand(exchange, refPrice));
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(2_500, true)]
    [InlineData(0, true)] // [Range] mới chặn giá trị nhỏ nhất, LotSize chỉ kiểm tra bội số
    [InlineData(150, false)]
    [InlineData(99, false)]
    public void LotSizeAttribute_AcceptsOnlyMultiplesOfLot(int quantity, bool expected)
    {
        var attribute = new LotSizeAttribute(TradingRules.LotSize);
        var context = new ValidationContext(new object()) { DisplayName = "Quantity" };

        var result = attribute.GetValidationResult(quantity, context);

        Assert.Equal(expected, result == ValidationResult.Success);
    }
}
