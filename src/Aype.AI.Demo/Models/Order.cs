using System;

namespace Aype.AI.Demo.Models
{
    /// <summary>
    /// Represents a customer order.
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Status: Pending, Confirmed, Shipped, Delivered, Cancelled.</summary>
        public string Status { get; set; }
        public string Description { get; set; }

        public override string ToString()
            => $"Order#{Id,3} | Firma#{CompanyId} | {Date:yyyy-MM-dd} | {Amount,10:C} | {Status,-10} | {Description}";
    }
}
