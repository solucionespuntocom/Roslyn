using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RoslynMetadata.LocalConsole
{
    public class Customer
    {
        [DisplayName("Nombre")]
        public string Name { get; set; }

        [DisplayName("Fecha Nacimiento")]
        public DateTime BirthDate { get; set; }

        //[DisplayName("Crédito Máximo")]
        public Decimal MaxLoan { get; set; }

        [DisplayName("Ciudad")]
        public string City { get; set; }
    }
}
