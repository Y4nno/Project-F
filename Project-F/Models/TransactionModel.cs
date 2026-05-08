namespace Project_F.Models
{
    public class TransactionModel
    {
        public string Title { get; set; }
        public double Amount { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }

        public string DisplayAmount =>
            Type == "Income"
                ? $"+₱{Amount:F2}"
                : $"-₱{Amount:F2}";
    }
}