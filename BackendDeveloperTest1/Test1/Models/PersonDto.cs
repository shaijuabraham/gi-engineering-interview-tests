

// Person DTO class representing basic personal information
// This class is used as a base class for MemberDto to include personal details
public class PersonDto
{
    // Basic personal information properties
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Locale { get; set; }
    public string PostalCode { get; set; }
}
