using NfgoOrderApi.Common;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Models;
using NfgoOrderApi.Repositories;

namespace NfgoOrderApi.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummaryDto>> GetAllAsync();
    Task<OrderDto> GetAsync(int id);
    Task<GenerateResultDto> GenerateAsync(OrderCreateRequest request);
    Task<GenerateResultDto> RegenerateAsync(int id);
    Task<OrderDto> SetStatusAsync(int id, OrderStatus status);
    Task DeleteAsync(int id);
    Task<(byte[] Content, string FileName)> BuildDocumentAsync(int id);
}

/// <summary>
/// Главная бизнес-логика: автоматическое формирование приказа о создании НФГО.
/// Берёт текущие назначения (сотрудник → формирование → роль), по роли подтягивает нормы СИЗ
/// и сохраняет всё это в приказе как неизменяемый «снимок».
/// </summary>
public class OrderService(
    IOrderRepository orders,
    IAssignmentRepository assignments,
    IRepository<Employee> employees,
    IOrderDocumentBuilder documentBuilder) : IOrderService
{
    public const string DefaultBasis =
        "в соответствии с Федеральным законом от 12.02.1998 № 28-ФЗ «О гражданской обороне» " +
        "и нормативными правовыми актами МЧС России о порядке создания нештатных формирований " +
        "по обеспечению выполнения мероприятий по гражданской обороне";

    public async Task<IReadOnlyList<OrderSummaryDto>> GetAllAsync() =>
        (await orders.GetAllWithMembersAsync()).Select(o => o.ToSummary()).ToList();

    public async Task<OrderDto> GetAsync(int id) => (await FindAsync(id)).ToDto();

    public async Task<GenerateResultDto> GenerateAsync(OrderCreateRequest request)
    {
        var number = request.Number.Trim();
        if (await orders.NumberExistsAsync(number))
            throw new ConflictException($"Приказ с номером «{number}» уже существует.");

        var order = new NfgoOrder
        {
            Number = number,
            OrderDate = request.OrderDate == default ? DateOnly.FromDateTime(DateTime.Today) : request.OrderDate,
            Organization = request.Organization.Trim(),
            Basis = string.IsNullOrWhiteSpace(request.Basis) ? DefaultBasis : request.Basis.Trim(),
            HeadPosition = request.HeadPosition.Trim(),
            HeadFullName = request.HeadFullName.Trim(),
            Status = OrderStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        var warnings = await FillMembersAsync(order);
        await orders.AddAsync(order);
        await orders.SaveAsync();

        return new GenerateResultDto { Order = order.ToDto(), Warnings = warnings };
    }

    public async Task<GenerateResultDto> RegenerateAsync(int id)
    {
        var order = await FindAsync(id);
        if (order.Status != OrderStatus.Draft)
            throw new BusinessRuleException("Пересоздать состав можно только у приказа в статусе «Проект».");

        order.Members.Clear();
        var warnings = await FillMembersAsync(order);
        await orders.SaveAsync();

        return new GenerateResultDto { Order = (await FindAsync(id)).ToDto(), Warnings = warnings };
    }

    public async Task<OrderDto> SetStatusAsync(int id, OrderStatus status)
    {
        var order = await FindAsync(id);

        var allowed = (order.Status, status) switch
        {
            (OrderStatus.Draft, OrderStatus.Signed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Signed, OrderStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
            throw new BusinessRuleException($"Нельзя перевести приказ из статуса «{order.Status}» в «{status}».");

        order.Status = status;
        order.SignedAt = status == OrderStatus.Signed ? DateTime.UtcNow : order.SignedAt;
        await orders.SaveAsync();
        return order.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await FindAsync(id);
        if (order.Status == OrderStatus.Signed)
            throw new BusinessRuleException("Подписанный приказ нельзя удалить — только отменить.");
        orders.Remove(order);
        await orders.SaveAsync();
    }

    public async Task<(byte[] Content, string FileName)> BuildDocumentAsync(int id)
    {
        var dto = (await FindAsync(id)).ToDto();
        var content = documentBuilder.Build(dto);

        var safeNumber = string.Concat(dto.Number.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        return (content, $"Prikaz_NFGO_{safeNumber}.docx");
    }

    // ---------- внутреннее ----------

    /// <summary>Заполняет приказ участниками и СИЗ по текущим назначениям; возвращает предупреждения.</summary>
    private async Task<List<string>> FillMembersAsync(NfgoOrder order)
    {
        var all = await assignments.GetAllWithDetailsAsync();
        if (all.Count == 0)
            throw new BusinessRuleException("Нет ни одного назначения сотрудников в НФГО — в приказ нечего включать.");

        var warnings = new List<string>();

        var ordered = all
            .OrderBy(a => a.UnitId)
            .ThenBy(a => a.Role!.Priority)
            .ThenBy(a => a.Employee!.FullName);

        foreach (var a in ordered)
        {
            var member = new NfgoOrderMember
            {
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee!.FullName,
                Position = a.Employee.Position,
                UnitName = a.Unit!.Name,
                RoleName = a.Role!.Name
            };

            foreach (var norm in a.Role.Norms.OrderBy(n => n.SizType!.Name))
            {
                member.SizItems.Add(new NfgoOrderMemberSiz
                {
                    SizTypeId = norm.SizTypeId,
                    SizName = norm.SizType!.Name,
                    Unit = norm.SizType.Unit,
                    Quantity = norm.Quantity
                });
            }

            order.Members.Add(member);
        }

        foreach (var g in all.Where(a => a.Role!.Norms.Count == 0).GroupBy(a => a.Role!.Name))
            warnings.Add($"Для роли «{g.Key}» не заданы нормы СИЗ ({g.Count()} чел.) — СИЗ в приказ не попали.");

        var assignedIds = all.Select(a => a.EmployeeId).ToHashSet();
        var unassigned = (await employees.GetAllAsync()).Count(e => !assignedIds.Contains(e.Id));
        if (unassigned > 0)
            warnings.Add($"Сотрудников без назначения в НФГО: {unassigned} — в приказ они не включены.");

        return warnings;
    }

    private async Task<NfgoOrder> FindAsync(int id) =>
        await orders.GetWithDetailsAsync(id) ?? throw new NotFoundException($"Приказ с id={id} не найден.");
}
