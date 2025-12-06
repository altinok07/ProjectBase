using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectBase.Core.Entities;

// Id içermeyen, sadece audit + soft delete tabanı
public abstract class BaseEntity
{
    [Column(Order = 95)]
    public DateTime? CreatedDate { get; set; }

    [Column(Order = 96)]
    public string? CreatedBy { get; set; }

    [Column(Order = 97)]
    public DateTime? UpdatedDate { get; set; }

    [Column(Order = 98)]
    public string? UpdatedBy { get; set; }

    [Column(Order = 99)]
    public bool IsDeleted { get; set; }

    [Column(Order = 100)]
    public DateTime? DeletedDate { get; set; }

    [Column(Order = 101)]
    public string? DeletedBy { get; set; }
}

// Id'yi generic yapan asıl base
// TKey: int, Guid, long, string gibi primary key tipleri için kullanılır
public abstract class BaseEntity<TKey> : BaseEntity
    where TKey : IEquatable<TKey>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 0)]
    public TKey Id { get; set; } = default!;
}

