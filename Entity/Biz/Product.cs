﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumos.Entity
{
    [Table("Product")]
    public class Product
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Retailer { get; set; }

        public string Category { get; set; }

        public string Material { get; set; }

        public string Color { get; set; }

        public string Style { get; set; }

        public decimal Price { get; set; }

        public string Size { get; set; }

        public string SmallImg { get; set; }

        public string BigImg { get; set; }
    }
}
