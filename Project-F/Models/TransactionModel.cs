namespace Project_F.Models;

public class TransactionModel
{
    public string? Title { get; set; }
    public string Icon { get; set; } = "💰";
    public double Amount { get; set; }
    public string? Type { get; set; }
    public string? UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;

    public string DisplayAmount => Type == "Income"
        ? $"+₱{Amount:F2}"
        : $"-₱{Amount:F2}";

    public string AmountColor => Type == "Income" ? "#4CAF50" : "#FF5252";

    public string TypeIcon => Icon ?? (Type == "Income" ? "💰" : "💸");
}