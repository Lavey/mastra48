namespace Mastra48.Demo.Models
{
    /// <summary>
    /// Represents a person contact at a company.
    /// </summary>
    public class Contact
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }

        public override string ToString()
            => $"Kontakt#{Id,3} | {FirstName} {LastName,-25} | {Position,-20} | Firma#{CompanyId} | {Email}";
    }
}
