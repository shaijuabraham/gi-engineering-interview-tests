using System;
namespace Test1.Models
{
  // PesonDto class represents basic personal information
  // Inherits from PersonDto to include personal details
  // MemberDto class represents a member with additional membership-related properties
  public class MemberDto : PersonDto
  {
    public int UID { get; set; }              
      public string Guid { get; set; }           
      public int AccountUid { get; set; }       
      public int LocationUid { get; set; }      
      public DateTime CreatedUtc { get; set; }   
      public DateTime? UpdatedUtc { get; set; }  
      public int Primary { get; set; }     
      public DateTime JoinedDateUtc { get; set; } 
      public DateTime? CancelDateUtc { get; set; }
       public int Cancelled { get; set; }   
  }

}