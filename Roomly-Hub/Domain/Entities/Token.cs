using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    [NotMapped]
    public class Token
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}