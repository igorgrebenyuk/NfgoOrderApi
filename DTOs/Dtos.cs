using System.ComponentModel.DataAnnotations;
using NfgoOrderApi.Models;

namespace NfgoOrderApi.DTOs;

// ---------- Справочники ----------

public class EmployeeDto
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string FullName { get; set; } = "";
    [Required, MaxLength(200)] public string Position { get; set; } = "";
    [MaxLength(200)] public string? Department { get; set; }
    [MaxLength(50)] public string? PersonnelNumber { get; set; }
}

public class SizTypeDto
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(20)] public string Unit { get; set; } = "шт.";
    [MaxLength(500)] public string? Description { get; set; }
}

public class UnitDto
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(500)] public string? Purpose { get; set; }
}

public class RoleDto
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    [Range(1, 1000)] public int Priority { get; set; } = 100;
    /// <summary>Нормы СИЗ (только для чтения; меняются через PUT /api/roles/{id}/siz-norms).</summary>
    public List<RoleNormDto> Norms { get; set; } = new();
}

public class RoleNormDto
{
    [Range(1, int.MaxValue)] public int SizTypeId { get; set; }
    public string SizName { get; set; } = "";
    public string Unit { get; set; } = "";
    [Range(1, 100)] public int Quantity { get; set; } = 1;
}

// ---------- Назначения ----------

public class AssignmentRequest
{
    [Range(1, int.MaxValue)] public int EmployeeId { get; set; }
    [Range(1, int.MaxValue)] public int UnitId { get; set; }
    [Range(1, int.MaxValue)] public int RoleId { get; set; }
}

public class AssignmentDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string EmployeePosition { get; set; } = "";
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
}

// ---------- Приказы ----------

public class OrderCreateRequest
{
    [Required, MaxLength(50)] public string Number { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    [Required, MaxLength(300)] public string Organization { get; set; } = "";
    /// <summary>Основание (нормативные акты). Если пусто — подставляется формулировка по умолчанию.</summary>
    [MaxLength(1000)] public string? Basis { get; set; }
    [Required, MaxLength(200)] public string HeadPosition { get; set; } = "";
    [Required, MaxLength(200)] public string HeadFullName { get; set; } = "";
}

public class OrderStatusRequest
{
    public OrderStatus Status { get; set; }
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public int MembersCount { get; set; }
    public int UnitsCount { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public string Organization { get; set; } = "";
    public string Basis { get; set; } = "";
    public string HeadPosition { get; set; } = "";
    public string HeadFullName { get; set; } = "";
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public List<OrderUnitDto> Units { get; set; } = new();
    public List<OrderMemberDto> Members { get; set; } = new();
    public List<OrderSizTotalDto> SizTotals { get; set; } = new();
}

public class OrderUnitDto
{
    public string UnitName { get; set; } = "";
    public int MembersCount { get; set; }
}

public class OrderMemberDto
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Position { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public List<OrderSizDto> SizItems { get; set; } = new();
}

public class OrderSizDto
{
    public int? SizTypeId { get; set; }
    public string SizName { get; set; } = "";
    public string Unit { get; set; } = "";
    public int Quantity { get; set; }
}

public class OrderSizTotalDto
{
    public string SizName { get; set; } = "";
    public string Unit { get; set; } = "";
    public int Total { get; set; }
}

public class GenerateResultDto
{
    public OrderDto Order { get; set; } = new();
    /// <summary>Замечания, не мешающие сформировать приказ (роль без норм СИЗ, сотрудники без назначения).</summary>
    public List<string> Warnings { get; set; } = new();
}
