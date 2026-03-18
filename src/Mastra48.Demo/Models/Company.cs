namespace Mastra48.Demo.Models
{
    /// <summary>
    /// Represents a business company (client or partner).
    /// </summary>
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NIP { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Industry { get; set; }

        public override string ToString()
            => $"Firma#{Id,3} | {Name,-30} | {Industry,-20} | {City,-15} | {Email}";
    }
}
