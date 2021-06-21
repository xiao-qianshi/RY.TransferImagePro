using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace RY.TransferImagePro.Domain.Entity
{
    public class ImageInformation
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }

        [StringLength(40)]
        public virtual string FileName { get; set; }
        [StringLength(200)]
        public virtual string FullName { get; set; }
        [StringLength(10)]
        public virtual string FileExtension { get; set; }
        public virtual long FileSize { get; set; }
        [StringLength(200)]
        public virtual string Location { get; set; }

        public DateTime CreateTime { get; set; }
        public DateTime UploadTime { get; set; }

        public bool HasUploaded { get; set; }
    }
}
