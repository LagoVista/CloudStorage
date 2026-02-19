using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Billing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class DbModelBase
    {
        [Key]
        public Guid Id { get; set; }

        [NotMapped]
        [Required]
        public string CreatedById { get; set; }


        [NotMapped]
        [Required]
        public string LastUpdatedById { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public DateTime LastUpdateDate { get; set; }

        [NotMapped]
        public AppUserDTO CreatedByUser { get; set; }

        [NotMapped]
        public AppUserDTO LastUpdatedByUser { get; set; }

        [NotMapped]
        public OrganizationDTO Organization { get; set; }

        protected LagoVista.Core.Validation.ValidationResult ValidateCommon()
        {
            var result = new LagoVista.Core.Validation.ValidationResult();
            if (Id == Guid.Empty) Id = Guid.NewGuid();
            if (CreationDate == default) CreationDate = DateTime.UtcNow;
            if (LastUpdateDate == default) LastUpdateDate = DateTime.UtcNow;
            if (!CreatedById.IsValidId()) result.AddSystemError("Invalid Created By Id");
            if (!LastUpdatedById.IsValidId()) result.AddSystemError("Invalid Created By Id");
            return result;
        }

        public void Map(EntityBase entity)
        {
            Id = Guid.Parse(entity.Id);
            CreatedById = entity.CreatedBy.Id;
            LastUpdatedById = entity.LastUpdatedBy.Id;
            CreationDate = DateTime.Parse(entity.CreationDate);
            LastUpdateDate = DateTime.Parse(entity.LastUpdatedDate);
        }
    }
}
