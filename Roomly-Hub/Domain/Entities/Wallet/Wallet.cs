namespace Domain.Entities.Wallet
{
    public class Wallet : BaseEntity
    {
        public Guid UserId { get; private set; }
        public decimal Balance { get; private set; }
        public decimal InsuranceHeldBalance { get; private set; }

        private Wallet()
        {
        }

        public static Wallet Create(Guid userId)
        {
            return new Wallet
            {
                UserId = userId,
                Balance = 0m,
                InsuranceHeldBalance = 0m
            };
        }

        public void Credit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Credit amount must be greater than zero.", nameof(amount));

            Balance += amount;
            MarkUpdated();
        }

        public void Debit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Debit amount must be greater than zero.", nameof(amount));

            if (Balance < amount)
                throw new InvalidOperationException("Insufficient wallet balance.");

            Balance -= amount;
            MarkUpdated();
        }

        public void LockInsurance(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Lock amount must be greater than zero.", nameof(amount));

            if (Balance < amount)
                throw new InvalidOperationException("Insufficient wallet balance to lock insurance.");

            Balance -= amount;
            InsuranceHeldBalance += amount;
            MarkUpdated();
        }

        public void ReleaseInsurance(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Release amount must be greater than zero.", nameof(amount));

            if (InsuranceHeldBalance < amount)
                throw new InvalidOperationException("Insufficient held insurance balance to release.");

            InsuranceHeldBalance -= amount;
            Balance += amount;
            MarkUpdated();
        }

        public void ForfeitInsurance(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Forfeit amount must be greater than zero.", nameof(amount));

            if (InsuranceHeldBalance < amount)
                throw new InvalidOperationException("Insufficient held insurance balance to forfeit.");

            InsuranceHeldBalance -= amount;
            MarkUpdated();
        }

        public bool CanWithdraw(decimal amount)
        {
            return amount > 0 && Balance >= amount;
        }
    }
}