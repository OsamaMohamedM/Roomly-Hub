using Application.Common.Pagination;

namespace Application.DTOs.Wallet
{
    public class WalletHistoryResponseDto
    {
        public WalletResponseDto Wallet { get; set; }
        public PagedResult<TransactionDto> Transactions { get; set; }
    }
}