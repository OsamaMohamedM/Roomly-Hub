using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [NotMapped]
    public class Token
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}