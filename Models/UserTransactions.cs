using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NurchureAPI.Models
{
    public class UserTransactions
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public int PaymentId { get; set; }
        public string MerchantId { get; set; }
        public int PackageId { get; set; }
        public int EventId { get; set; }
        public int TransactionTypeId { get; set; }
        public int PaymentTypeId { get; set; }
        public int UserTransactionsId { get; set; }
        public int SquareCustomerCardDetailsId { get; set; }
        public decimal InitialRevenue { get; set; }
        public double DiscountPercent { get; set; }
        public decimal Discount { get; set; }
        public decimal RevenueReceived { get; set; }
        public int CostTypeId { get; set; }
        public bool IsRefunded { get; set; }
        public string RefundDetails { get; set; }
        public string Notes { get; set; }
        public string SquareRefNo { get; set; }
        public string ResponseText { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}