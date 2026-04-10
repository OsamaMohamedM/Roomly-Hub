namespace Domain.Entities.Payment
{
    public class PaymentMethoodModel
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }

        public string NameEn { get; set; }

        public string NameAr { get; set; }

        public string Redirect { get; set; }

        public string Logo { get; set; }
    }
}