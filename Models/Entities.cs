using System.ComponentModel.DataAnnotations;

namespace NfgoOrderApi.Models;

public interface IEntity
{
    int Id { get; set; }
}

/// <summary>Статус приказа: проект → подписан / отменён.</summary>
public enum OrderStatus
{
    Draft,
    Signed,
    Cancelled
}

/// <summary>Сотрудник организации.</summary>
public class Employee : IEntity
{
    public int Id { get; set; }
    [MaxLength(200)] public string FullName { get; set; } = "";
    [MaxLength(200)] public string Position { get; set; } = "";
    [MaxLength(200)] public string? Department { get; set; }
    [MaxLength(50)] public string? PersonnelNumber { get; set; }
}

/// <summary>Вид СИЗ из справочника (противогаз ГП-7, костюм Л-1 и т.п.).</summary>
public class SizType : IEntity
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Unit { get; set; } = "шт.";
    [MaxLength(500)] public string? Description { get; set; }
}

/// <summary>Нештатное формирование гражданской обороны (спасательное звено, санитарный пост...).</summary>
public class NfgoUnit : IEntity
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(500)] public string? Purpose { get; set; }
}

/// <summary>Роль в НФГО (командир, спасатель, санитар...). Для роли заданы нормы выдачи СИЗ.</summary>
public class NfgoRole : IEntity
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    /// <summary>Порядок сортировки в приказе (меньше — выше, например командир = 1).</summary>
    public int Priority { get; set; } = 100;
    public List<RoleSizNorm> Norms { get; set; } = new();
}

/// <summary>Норма: сколько единиц СИЗ данного вида положено человеку в этой роли.</summary>
public class RoleSizNorm : IEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public NfgoRole? Role { get; set; }
    public int SizTypeId { get; set; }
    public SizType? SizType { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>Назначение: сотрудник → формирование + роль. Один сотрудник состоит в одном формировании.</summary>
public class NfgoAssignment : IEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int UnitId { get; set; }
    public NfgoUnit? Unit { get; set; }
    public int RoleId { get; set; }
    public NfgoRole? Role { get; set; }
}

/// <summary>Приказ о создании НФГО. Состав и СИЗ в нём — «снимок» на момент формирования.</summary>
public class NfgoOrder : IEntity
{
    public int Id { get; set; }
    [MaxLength(50)] public string Number { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    [MaxLength(300)] public string Organization { get; set; } = "";
    [MaxLength(1000)] public string? Basis { get; set; }
    [MaxLength(200)] public string HeadPosition { get; set; } = "";
    [MaxLength(200)] public string HeadFullName { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
    public List<NfgoOrderMember> Members { get; set; } = new();
}

/// <summary>Строка приказа: сотрудник, его формирование и роль (копия данных на момент формирования).</summary>
public class NfgoOrderMember
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    /// <summary>Ссылка на сотрудника (без внешнего ключа: снимок остаётся, даже если сотрудника удалят).</summary>
    public int? EmployeeId { get; set; }
    [MaxLength(200)] public string EmployeeName { get; set; } = "";
    [MaxLength(200)] public string Position { get; set; } = "";
    [MaxLength(200)] public string UnitName { get; set; } = "";
    [MaxLength(200)] public string RoleName { get; set; } = "";
    public List<NfgoOrderMemberSiz> SizItems { get; set; } = new();
}

/// <summary>СИЗ, закреплённое за участником приказа.</summary>
public class NfgoOrderMemberSiz
{
    public int Id { get; set; }
    public int OrderMemberId { get; set; }
    public int? SizTypeId { get; set; }
    [MaxLength(200)] public string SizName { get; set; } = "";
    [MaxLength(20)] public string Unit { get; set; } = "шт.";
    public int Quantity { get; set; }
}
