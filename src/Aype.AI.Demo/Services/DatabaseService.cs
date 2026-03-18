using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aype.AI.Demo.Models;

namespace Aype.AI.Demo.Services
{
    /// <summary>
    /// In-memory data store containing sample data for Orders, Companies, Contacts and Offers.
    /// Provides natural-language query translation to LINQ-based execution.
    /// Mirrors the SQL Agent pattern: user intent → structured query → results.
    /// </summary>
    public class DatabaseService
    {
        // ----------------------------------------------------------------
        // In-memory tables
        // ----------------------------------------------------------------

        public static readonly List<Company> Companies = new List<Company>();
        public static readonly List<Order> Orders = new List<Order>();
        public static readonly List<Contact> Contacts = new List<Contact>();
        public static readonly List<Offer> Offers = new List<Offer>();

        // ----------------------------------------------------------------
        // Initialise sample data
        // ----------------------------------------------------------------

        static DatabaseService()
        {
            SeedCompanies();
            SeedOrders();
            SeedContacts();
            SeedOffers();
        }

        private static void SeedCompanies()
        {
            var data = new[]
            {
                new { Name="TechNova Sp. z o.o.",    NIP="1234567890", Email="contact@technova.pl",    Phone="22-100-2000", Address="ul. Innowacyjna 1",  City="Warszawa",  Industry="IT" },
                new { Name="BuildPro S.A.",           NIP="2345678901", Email="biuro@buildpro.pl",       Phone="12-200-3000", Address="ul. Murarska 5",    City="Kraków",    Industry="Budownictwo" },
                new { Name="GreenFood Sp. z o.o.",   NIP="3456789012", Email="info@greenfood.pl",       Phone="61-300-4000", Address="ul. Zielona 12",    City="Poznań",    Industry="Spożywczy" },
                new { Name="AutoParts Polska S.A.",  NIP="4567890123", Email="handel@autoparts.pl",     Phone="71-400-5000", Address="ul. Motoryczna 8",  City="Wrocław",   Industry="Motoryzacja" },
                new { Name="MediCare Group",          NIP="5678901234", Email="office@medicare.pl",      Phone="58-500-6000", Address="ul. Zdrowia 3",     City="Gdańsk",    Industry="Medycyna" },
                new { Name="LogiTrans Sp. z o.o.",   NIP="6789012345", Email="info@logitrans.pl",       Phone="32-600-7000", Address="ul. Transportowa 2",City="Katowice",  Industry="Logistyka" },
                new { Name="EduSmart Sp. z o.o.",    NIP="7890123456", Email="kontakt@edusmart.pl",     Phone="85-700-8000", Address="ul. Uczniowska 7",  City="Białystok", Industry="Edukacja" },
                new { Name="FinanceHub S.A.",         NIP="8901234567", Email="info@financehub.pl",      Phone="22-800-9000", Address="ul. Bankowa 15",    City="Warszawa",  Industry="Finanse" },
                new { Name="CleanEnergy Sp. z o.o.", NIP="9012345678", Email="biuro@cleanenergy.pl",    Phone="91-900-1000", Address="ul. Słoneczna 22",  City="Rzeszów",   Industry="Energia" },
                new { Name="RetailMax S.A.",          NIP="0123456789", Email="sklep@retailmax.pl",      Phone="42-001-2000", Address="ul. Handlowa 9",    City="Łódź",      Industry="Handel" },
                new { Name="DataSoft Sp. z o.o.",    NIP="1122334455", Email="dev@datasoft.pl",         Phone="22-111-2222", Address="ul. Cyfrowa 4",     City="Warszawa",  Industry="IT" },
                new { Name="AgriPol S.A.",            NIP="2233445566", Email="biuro@agripol.pl",        Phone="17-222-3333", Address="ul. Polna 1",       City="Lublin",    Industry="Rolnictwo" },
                new { Name="CityRent Sp. z o.o.",    NIP="3344556677", Email="wynajem@cityrent.pl",     Phone="12-333-4444", Address="ul. Mieszkaniowa 6",City="Kraków",    Industry="Nieruchomości" },
                new { Name="SteelWorks S.A.",         NIP="4455667788", Email="info@steelworks.pl",      Phone="32-444-5555", Address="ul. Stalowa 3",     City="Gliwice",   Industry="Metalurgia" },
                new { Name="PharmaPlus Sp. z o.o.",  NIP="5566778899", Email="apteka@pharmaplus.pl",    Phone="48-555-6666", Address="ul. Farmaceutyczna 11",City="Radom",  Industry="Farmacja" },
                new { Name="TravelStar S.A.",         NIP="6677889900", Email="biuro@travelstar.pl",     Phone="22-666-7777", Address="ul. Turystyczna 18",City="Warszawa",  Industry="Turystyka" },
                new { Name="PrintMaster Sp. z o.o.", NIP="7788990011", Email="druk@printmaster.pl",     Phone="61-777-8888", Address="ul. Drukarska 5",   City="Poznań",    Industry="Poligrafia" },
                new { Name="NetSecure S.A.",          NIP="8899001122", Email="security@netsecure.pl",   Phone="22-888-9999", Address="ul. Bezpieczna 7",  City="Warszawa",  Industry="Cyberbezpieczeństwo" },
                new { Name="FoodChain Polska",        NIP="9900112233", Email="info@foodchain.pl",       Phone="71-999-0000", Address="ul. Gastronomiczna 2",City="Wrocław", Industry="Gastronomia" },
                new { Name="ChemLab Sp. z o.o.",     NIP="1023456780", Email="lab@chemlab.pl",          Phone="91-100-1111", Address="ul. Chemiczna 14",  City="Szczecin",  Industry="Chemia" },
                new { Name="SmartHome S.A.",          NIP="2034567891", Email="smart@smarthome.pl",      Phone="22-101-2121", Address="ul. Automatyki 3",  City="Warszawa",  Industry="Domotyka" },
                new { Name="TextilePro Sp. z o.o.",  NIP="3045678902", Email="moda@textilepro.pl",      Phone="42-202-3232", Address="ul. Włókiennicza 8",City="Łódź",      Industry="Odzież" },
                new { Name="AquaTech S.A.",           NIP="4056789013", Email="woda@aquatech.pl",        Phone="58-303-4343", Address="ul. Wodna 1",       City="Gdynia",    Industry="Gospodarka wodna" },
                new { Name="ArchDesign Sp. z o.o.",  NIP="5067890124", Email="projekt@archdesign.pl",   Phone="12-404-5454", Address="ul. Architektoniczna 6",City="Kraków", Industry="Architektura" },
                new { Name="ColdChain Logistics",    NIP="6078901235", Email="cold@coldchain.pl",       Phone="22-505-6565", Address="ul. Chłodnicza 9",  City="Warszawa",  Industry="Logistyka chłodnicza" },
            };
            for (int i = 0; i < data.Length; i++)
            {
                var d = data[i];
                Companies.Add(new Company
                {
                    Id = i + 1, Name = d.Name, NIP = d.NIP, Email = d.Email,
                    Phone = d.Phone, Address = d.Address, City = d.City, Industry = d.Industry
                });
            }
        }

        private static void SeedOrders()
        {
            var now = DateTime.Today;
            var rng = new Random(42);
            var statuses = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
            var descs = new[]
            {
                "Dostawa sprzętu IT", "Licencja oprogramowania", "Usługi konsultingowe",
                "Szkolenie pracowników", "Materiały biurowe", "Sprzęt budowlany",
                "Produkty spożywcze", "Części zamienne", "Usługi serwisowe",
                "Wyposażenie laboratorium", "Transport towarów", "Usługi marketingowe",
                "Oprogramowanie ERP", "Meble biurowe", "Sprzęt medyczny",
                "Usługi ochrony danych", "Wynajem powierzchni", "Usługi projektowe",
                "Dostawy chemiczne", "System smart home"
            };
            for (int i = 1; i <= 30; i++)
            {
                Orders.Add(new Order
                {
                    Id = i,
                    CompanyId = rng.Next(1, Companies.Count + 1),
                    Date = now.AddDays(-rng.Next(0, 365)),
                    Amount = Math.Round((decimal)(rng.NextDouble() * 50000 + 500), 2),
                    Status = statuses[rng.Next(statuses.Length)],
                    Description = descs[rng.Next(descs.Length)]
                });
            }
        }

        private static void SeedContacts()
        {
            var firstNames = new[] { "Anna", "Piotr", "Katarzyna", "Michał", "Agnieszka", "Tomasz", "Marta", "Marek", "Joanna", "Krzysztof" };
            var lastNames  = new[] { "Kowalski", "Nowak", "Wiśniewski", "Wójcik", "Kowalczyk", "Kamiński", "Lewandowski", "Zieliński", "Szymański", "Woźniak" };
            var positions  = new[] { "Dyrektor Handlowy", "Kierownik Zakupów", "Specjalista ds. Sprzedaży", "Prezes Zarządu", "CFO", "CTO", "Menedżer Projektu", "Analityk Biznesowy", "Koordynator", "Asystent Zarządu" };
            var rng = new Random(99);
            for (int i = 1; i <= 25; i++)
            {
                var fn = firstNames[rng.Next(firstNames.Length)];
                var ln = lastNames[rng.Next(lastNames.Length)];
                var pos = positions[rng.Next(positions.Length)];
                var cid = rng.Next(1, Companies.Count + 1);
                Contacts.Add(new Contact
                {
                    Id = i,
                    CompanyId = cid,
                    FirstName = fn,
                    LastName = ln,
                    Email = $"{fn.ToLower()}.{ln.ToLower()}@{Companies[cid - 1].Name.Split(' ')[0].ToLower().Replace(".", "")}.pl",
                    Phone = $"{rng.Next(10, 99)}-{rng.Next(100, 999)}-{rng.Next(1000, 9999)}",
                    Position = pos
                });
            }
        }

        private static void SeedOffers()
        {
            var now = DateTime.Today;
            var rng = new Random(77);
            var statuses = new[] { "Draft", "Sent", "Accepted", "Rejected", "Expired" };
            var titles = new[]
            {
                "Wdrożenie systemu CRM", "Dostawa sprzętu serwerowego", "Umowa serwisowa roczna",
                "Projekt sieci LAN", "Licencja na oprogramowanie analityczne", "Szkolenie z cyberbezpieczeństwa",
                "Audyt IT", "Migracja do chmury", "Wdrożenie ERP", "Projekt strony internetowej",
                "Kampania marketingowa online", "Outsourcing działu IT", "Usługi chmurowe AWS",
                "Kontrakt na dostawy materiałów", "Modernizacja infrastruktury", "Wdrożenie systemu BI",
                "Usługi call center", "Projekt aplikacji mobilnej", "Doradztwo strategiczne", "Usługi księgowe"
            };
            for (int i = 1; i <= 25; i++)
            {
                var date = now.AddDays(-rng.Next(10, 300));
                Offers.Add(new Offer
                {
                    Id = i,
                    CompanyId = rng.Next(1, Companies.Count + 1),
                    Title = titles[rng.Next(titles.Length)],
                    Value = Math.Round((decimal)(rng.NextDouble() * 200000 + 5000), 2),
                    Status = statuses[rng.Next(statuses.Length)],
                    Date = date,
                    ExpiryDate = date.AddDays(30 + rng.Next(60))
                });
            }
        }

        // ----------------------------------------------------------------
        // Query execution
        // ----------------------------------------------------------------

        /// <summary>
        /// Translates a natural-language query to a structured intent and executes it.
        /// Returns a tuple: (generatedSQL, results as formatted string).
        /// </summary>
        public (string sql, string results) ExecuteNaturalQuery(string query)
        {
            var q = (query ?? "").ToLower();

            // ----- Orders -----
            if (ContainsAny(q, "zamówieni", "zamówień", "order"))
            {
                return QueryOrders(q);
            }

            // ----- Companies -----
            if (ContainsAny(q, "firm", "compan", "przedsiębiorstw"))
            {
                return QueryCompanies(q);
            }

            // ----- Contacts -----
            if (ContainsAny(q, "kontakt", "contact", "osob", "pracownik"))
            {
                return QueryContacts(q);
            }

            // ----- Offers -----
            if (ContainsAny(q, "ofert", "offer", "propozycj"))
            {
                return QueryOffers(q);
            }

            return ("-- (brak dopasowania)", "Nie rozpoznano zapytania. Spróbuj zapytać o zamówienia, firmy, kontakty lub oferty.");
        }

        // ----------------------------------------------------------------
        // Order queries
        // ----------------------------------------------------------------

        private (string sql, string results) QueryOrders(string q)
        {
            var now = DateTime.Today;
            IEnumerable<Order> filtered = Orders;
            var sb = new StringBuilder("SELECT * FROM Orders");
            var conditions = new List<string>();

            // Time-based filters
            if (ContainsAny(q, "ostatni miesiąc", "ostatniego miesiąca", "this month", "last month"))
            {
                var cutoff = now.AddDays(-30);
                filtered = filtered.Where(o => o.Date >= cutoff);
                conditions.Add($"Date >= '{cutoff:yyyy-MM-dd}'");
            }
            else if (ContainsAny(q, "ostatni tydzień", "last week"))
            {
                var cutoff = now.AddDays(-7);
                filtered = filtered.Where(o => o.Date >= cutoff);
                conditions.Add($"Date >= '{cutoff:yyyy-MM-dd}'");
            }
            else if (ContainsAny(q, "dzisiaj", "today"))
            {
                filtered = filtered.Where(o => o.Date.Date == now);
                conditions.Add($"Date = '{now:yyyy-MM-dd}'");
            }

            // Status filters
            if (ContainsAny(q, "pending", "oczekując"))
            {
                filtered = filtered.Where(o => o.Status == "Pending");
                conditions.Add("Status = 'Pending'");
            }
            else if (ContainsAny(q, "delivered", "dostarczon", "zrealizowa"))
            {
                filtered = filtered.Where(o => o.Status == "Delivered");
                conditions.Add("Status = 'Delivered'");
            }
            else if (ContainsAny(q, "cancelled", "anulowa"))
            {
                filtered = filtered.Where(o => o.Status == "Cancelled");
                conditions.Add("Status = 'Cancelled'");
            }

            // Amount-based filters
            if (ContainsAny(q, "duż", "wysok", "wielk", "powyżej 10000"))
            {
                filtered = filtered.Where(o => o.Amount > 10000);
                conditions.Add("Amount > 10000");
            }

            // Sorting
            if (ContainsAny(q, "suma", "łączna wartość", "total"))
            {
                var total = filtered.Sum(o => o.Amount);
                var sql2 = BuildSql("SELECT SUM(Amount) FROM Orders", conditions);
                return (sql2, $"Łączna wartość zamówień: {total:C}");
            }

            var ordered = filtered.OrderByDescending(o => o.Date).ToList();

            if (conditions.Any())
                sb.Append(" WHERE " + string.Join(" AND ", conditions));
            sb.Append(" ORDER BY Date DESC");

            if (!ordered.Any())
                return (sb.ToString(), "Brak zamówień spełniających kryteria.");

            var output = new StringBuilder();
            output.AppendLine($"Znaleziono {ordered.Count} zamówień:");
            output.AppendLine(new string('-', 80));
            foreach (var o in ordered.Take(20))
                output.AppendLine(o.ToString());
            if (ordered.Count > 20)
                output.AppendLine($"... i {ordered.Count - 20} więcej.");

            return (sb.ToString(), output.ToString().TrimEnd());
        }

        // ----------------------------------------------------------------
        // Company queries
        // ----------------------------------------------------------------

        private (string sql, string results) QueryCompanies(string q)
        {
            IEnumerable<Company> filtered = Companies;
            var sb = new StringBuilder("SELECT * FROM Companies");
            var conditions = new List<string>();

            // Industry filter
            var industries = new[] { "IT", "Finanse", "Logistyka", "Medycyna", "Budownictwo", "Edukacja", "Handel" };
            foreach (var ind in industries)
            {
                if (q.Contains(ind.ToLower()))
                {
                    filtered = filtered.Where(c => c.Industry.ToLower().Contains(ind.ToLower()));
                    conditions.Add($"Industry LIKE '%{ind}%'");
                    break;
                }
            }

            // City filter
            var cities = new[] { "Warszawa", "Kraków", "Wrocław", "Poznań", "Gdańsk", "Łódź" };
            foreach (var city in cities)
            {
                if (q.Contains(city.ToLower()))
                {
                    filtered = filtered.Where(c => c.City.ToLower() == city.ToLower());
                    conditions.Add($"City = '{city}'");
                    break;
                }
            }

            if (conditions.Any())
                sb.Append(" WHERE " + string.Join(" AND ", conditions));

            var list = filtered.OrderBy(c => c.Name).ToList();

            if (!list.Any())
                return (sb.ToString(), "Brak firm spełniających kryteria.");

            var output = new StringBuilder();
            output.AppendLine($"Znaleziono {list.Count} firm:");
            output.AppendLine(new string('-', 80));
            foreach (var c in list.Take(20))
                output.AppendLine(c.ToString());

            return (sb.ToString(), output.ToString().TrimEnd());
        }

        // ----------------------------------------------------------------
        // Contact queries
        // ----------------------------------------------------------------

        private (string sql, string results) QueryContacts(string q)
        {
            IEnumerable<Contact> filtered = Contacts;
            var sb = new StringBuilder("SELECT * FROM Contacts");
            var conditions = new List<string>();

            // Position filter
            if (ContainsAny(q, "dyrektor", "director"))
            {
                filtered = filtered.Where(c => c.Position.ToLower().Contains("dyrektor"));
                conditions.Add("Position LIKE '%Dyrektor%'");
            }
            else if (ContainsAny(q, "kierownik", "manager"))
            {
                filtered = filtered.Where(c => c.Position.ToLower().Contains("kierownik"));
                conditions.Add("Position LIKE '%Kierownik%'");
            }
            else if (ContainsAny(q, "prezes", "ceo"))
            {
                filtered = filtered.Where(c => c.Position.ToLower().Contains("prezes"));
                conditions.Add("Position LIKE '%Prezes%'");
            }

            if (conditions.Any())
                sb.Append(" WHERE " + string.Join(" AND ", conditions));

            var list = filtered.OrderBy(c => c.LastName).ToList();

            if (!list.Any())
                return (sb.ToString(), "Brak kontaktów spełniających kryteria.");

            var output = new StringBuilder();
            output.AppendLine($"Znaleziono {list.Count} kontaktów:");
            output.AppendLine(new string('-', 80));
            foreach (var c in list.Take(20))
                output.AppendLine(c.ToString());

            return (sb.ToString(), output.ToString().TrimEnd());
        }

        // ----------------------------------------------------------------
        // Offer queries
        // ----------------------------------------------------------------

        private (string sql, string results) QueryOffers(string q)
        {
            var now = DateTime.Today;
            IEnumerable<Offer> filtered = Offers;
            var sb = new StringBuilder("SELECT * FROM Offers");
            var conditions = new List<string>();

            if (ContainsAny(q, "przyjęt", "zaakceptowa", "accepted"))
            {
                filtered = filtered.Where(o => o.Status == "Accepted");
                conditions.Add("Status = 'Accepted'");
            }
            else if (ContainsAny(q, "odrzucon", "rejected"))
            {
                filtered = filtered.Where(o => o.Status == "Rejected");
                conditions.Add("Status = 'Rejected'");
            }
            else if (ContainsAny(q, "wysłan", "sent"))
            {
                filtered = filtered.Where(o => o.Status == "Sent");
                conditions.Add("Status = 'Sent'");
            }
            else if (ContainsAny(q, "wygasł", "expired"))
            {
                filtered = filtered.Where(o => o.Status == "Expired" || o.ExpiryDate < now);
                conditions.Add($"ExpiryDate < '{now:yyyy-MM-dd}'");
            }

            if (conditions.Any())
                sb.Append(" WHERE " + string.Join(" AND ", conditions));

            var list = filtered.OrderByDescending(o => o.Date).ToList();

            if (!list.Any())
                return (sb.ToString(), "Brak ofert spełniających kryteria.");

            var output = new StringBuilder();
            output.AppendLine($"Znaleziono {list.Count} ofert:");
            output.AppendLine(new string('-', 80));
            foreach (var o in list.Take(20))
                output.AppendLine(o.ToString());

            return (sb.ToString(), output.ToString().TrimEnd());
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (text.Contains(kw)) return true;
            return false;
        }

        private static string BuildSql(string baseQuery, List<string> conditions)
        {
            if (conditions.Any())
                return baseQuery + " WHERE " + string.Join(" AND ", conditions);
            return baseQuery;
        }

        /// <summary>Returns all data as a formatted text report for file export.</summary>
        public string GenerateReport(string reportType)
        {
            var sb = new StringBuilder();
            var now = DateTime.Now;
            sb.AppendLine($"# Raport: {reportType}");
            sb.AppendLine($"# Wygenerowano: {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 80));

            switch ((reportType ?? "").ToLower())
            {
                case "orders":
                case "zamówienia":
                    sb.AppendLine($"Zamówienia ({Orders.Count} rekordów):");
                    foreach (var o in Orders.OrderByDescending(x => x.Date))
                        sb.AppendLine(o.ToString());
                    break;

                case "companies":
                case "firmy":
                    sb.AppendLine($"Firmy ({Companies.Count} rekordów):");
                    foreach (var c in Companies.OrderBy(x => x.Name))
                        sb.AppendLine(c.ToString());
                    break;

                case "contacts":
                case "kontakty":
                    sb.AppendLine($"Kontakty ({Contacts.Count} rekordów):");
                    foreach (var c in Contacts.OrderBy(x => x.LastName))
                        sb.AppendLine(c.ToString());
                    break;

                case "offers":
                case "oferty":
                    sb.AppendLine($"Oferty ({Offers.Count} rekordów):");
                    foreach (var o in Offers.OrderByDescending(x => x.Date))
                        sb.AppendLine(o.ToString());
                    break;

                default:
                    sb.AppendLine($"Zamówienia ({Orders.Count} rekordów):");
                    foreach (var o in Orders.OrderByDescending(x => x.Date))
                        sb.AppendLine(o.ToString());
                    sb.AppendLine();
                    sb.AppendLine($"Firmy ({Companies.Count} rekordów):");
                    foreach (var c in Companies.OrderBy(x => x.Name))
                        sb.AppendLine(c.ToString());
                    break;
            }

            return sb.ToString();
        }
    }
}
