using System;

namespace Mastra48.Demo.Models
{
    /// <summary>
    /// Represents a commercial offer/proposal sent to a company.
    /// </summary>
    public class Offer
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Title { get; set; }
        public decimal Value { get; set; }
        /// <summary>Status: Draft, Sent, Accepted, Rejected, Expired.</summary>
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public DateTime ExpiryDate { get; set; }

        public override string ToString()
            => $"Oferta#{Id,3} | Firma#{CompanyId} | {Title,-35} | {Value,10:C} | {Status,-8} | {Date:yyyy-MM-dd}";
    }
}
