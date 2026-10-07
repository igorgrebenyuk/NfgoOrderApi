using NfgoOrderApi.DTOs;
using NfgoOrderApi.Models;

namespace NfgoOrderApi.Services;

public static class Mappers
{
    public static EmployeeDto ToDto(this Employee e) => new()
    {
        Id = e.Id, FullName = e.FullName, Position = e.Position,
        Department = e.Department, PersonnelNumber = e.PersonnelNumber
    };

    public static SizTypeDto ToDto(this SizType s) => new()
    {
        Id = s.Id, Name = s.Name, Unit = s.Unit, Description = s.Description
    };

    public static UnitDto ToDto(this NfgoUnit u) => new()
    {
        Id = u.Id, Name = u.Name, Purpose = u.Purpose
    };

    public static RoleDto ToDto(this NfgoRole r) => new()
    {
        Id = r.Id, Name = r.Name, Description = r.Description, Priority = r.Priority,
        Norms = r.Norms.OrderBy(n => n.SizType?.Name).Select(n => new RoleNormDto
        {
            SizTypeId = n.SizTypeId,
            SizName = n.SizType?.Name ?? "",
            Unit = n.SizType?.Unit ?? "",
            Quantity = n.Quantity
        }).ToList()
    };

    public static AssignmentDto ToDto(this NfgoAssignment a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId, EmployeeName = a.Employee?.FullName ?? "", EmployeePosition = a.Employee?.Position ?? "",
        UnitId = a.UnitId, UnitName = a.Unit?.Name ?? "",
        RoleId = a.RoleId, RoleName = a.Role?.Name ?? ""
    };

    public static OrderSummaryDto ToSummary(this NfgoOrder o) => new()
    {
        Id = o.Id, Number = o.Number, OrderDate = o.OrderDate, Status = o.Status,
        MembersCount = o.Members.Count,
        UnitsCount = o.Members.Select(m => m.UnitName).Distinct().Count()
    };

    public static OrderDto ToDto(this NfgoOrder o)
    {
        var members = o.Members.OrderBy(m => m.Id).Select(m => new OrderMemberDto
        {
            Id = m.Id, EmployeeId = m.EmployeeId, EmployeeName = m.EmployeeName,
            Position = m.Position, UnitName = m.UnitName, RoleName = m.RoleName,
            SizItems = m.SizItems.OrderBy(s => s.Id).Select(s => new OrderSizDto
            {
                SizTypeId = s.SizTypeId, SizName = s.SizName, Unit = s.Unit, Quantity = s.Quantity
            }).ToList()
        }).ToList();

        return new OrderDto
        {
            Id = o.Id, Number = o.Number, OrderDate = o.OrderDate,
            Organization = o.Organization, Basis = o.Basis ?? "",
            HeadPosition = o.HeadPosition, HeadFullName = o.HeadFullName,
            Status = o.Status, CreatedAt = o.CreatedAt, SignedAt = o.SignedAt,
            Members = members,
            Units = members.GroupBy(m => m.UnitName)
                .Select(g => new OrderUnitDto { UnitName = g.Key, MembersCount = g.Count() }).ToList(),
            SizTotals = members.SelectMany(m => m.SizItems)
                .GroupBy(s => new { s.SizName, s.Unit })
                .Select(g => new OrderSizTotalDto { SizName = g.Key.SizName, Unit = g.Key.Unit, Total = g.Sum(x => x.Quantity) })
                .OrderBy(t => t.SizName).ToList()
        };
    }
}
