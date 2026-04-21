using Domain.Entities;

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
            Balance += amount;
            MarkUpdated();
        }

        public void Debit(decimal amount)
        {
            Balance -= amount;
            MarkUpdated();
        }

        public void LockInsurance(decimal amount)
        {
            Balance -= amount;
            InsuranceHeldBalance += amount;
            MarkUpdated();
        }

        public void ReleaseInsurance(decimal amount)
        {
            InsuranceHeldBalance -= amount;
            Balance += amount;
            MarkUpdated();
        }

        public void ForfeitInsurance(decimal amount)
        {
            InsuranceHeldBalance -= amount;
            MarkUpdated();
        }

        public bool CanWithdraw(decimal amount)
        {
            return amount > 0 && Balance >= amount;
        }
    }
}