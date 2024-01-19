using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UMNPhotographers.Distribution.Domain.Interfaces;

namespace UMNPhotographers.Distribution.Domain.Entities;

public class BaseEntity : IEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
}