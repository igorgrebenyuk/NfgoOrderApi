using NfgoOrderApi.Models;

namespace NfgoOrderApi.Data;

/// <summary>Демо-данные для первого запуска (отключаются через "SeedDemoData": false).</summary>
public static class DemoDataSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Employees.Any() || db.Roles.Any() || db.SizTypes.Any()) return;

        // --- СИЗ ---
        var gp7 = new SizType { Name = "Противогаз ГП-7", Unit = "шт.", Description = "Фильтрующий противогаз" };
        var r2 = new SizType { Name = "Респиратор Р-2", Unit = "шт.", Description = "Противопылевой респиратор" };
        var l1 = new SizType { Name = "Костюм защитный Л-1", Unit = "компл.", Description = "Лёгкий защитный костюм" };
        var ai2 = new SizType { Name = "Аптечка индивидуальная АИ-2", Unit = "шт." };
        var ipp = new SizType { Name = "Пакет противохимический ИПП-11", Unit = "шт." };

        // --- Роли и нормы ---
        var commander = new NfgoRole
        {
            Name = "Командир формирования", Priority = 1,
            Description = "Руководит формированием",
            Norms =
            {
                new RoleSizNorm { SizType = gp7, Quantity = 1 },
                new RoleSizNorm { SizType = ai2, Quantity = 1 },
                new RoleSizNorm { SizType = ipp, Quantity = 1 }
            }
        };
        var rescuer = new NfgoRole
        {
            Name = "Спасатель", Priority = 10,
            Norms =
            {
                new RoleSizNorm { SizType = gp7, Quantity = 1 },
                new RoleSizNorm { SizType = l1, Quantity = 1 },
                new RoleSizNorm { SizType = ai2, Quantity = 1 },
                new RoleSizNorm { SizType = ipp, Quantity = 1 }
            }
        };
        var medic = new NfgoRole
        {
            Name = "Санитар", Priority = 20,
            Norms =
            {
                new RoleSizNorm { SizType = gp7, Quantity = 1 },
                new RoleSizNorm { SizType = ai2, Quantity = 2 }
            }
        };
        var signaller = new NfgoRole
        {
            Name = "Связист", Priority = 30,
            Norms =
            {
                new RoleSizNorm { SizType = gp7, Quantity = 1 },
                new RoleSizNorm { SizType = r2, Quantity = 1 }
            }
        };

        // --- Формирования ---
        var rescueUnit = new NfgoUnit { Name = "Спасательное звено", Purpose = "Проведение аварийно-спасательных работ" };
        var medUnit = new NfgoUnit { Name = "Санитарный пост", Purpose = "Первая помощь пострадавшим" };
        var commUnit = new NfgoUnit { Name = "Звено связи и оповещения", Purpose = "Оповещение и связь" };

        // --- Сотрудники (вымышленные) ---
        var e1 = new Employee { FullName = "Петров Сергей Николаевич", Position = "Начальник отдела безопасности", Department = "Безопасность", PersonnelNumber = "0001" };
        var e2 = new Employee { FullName = "Смирнов Алексей Игоревич", Position = "Инженер", Department = "Эксплуатация", PersonnelNumber = "0012" };
        var e3 = new Employee { FullName = "Кузнецов Дмитрий Олегович", Position = "Слесарь", Department = "Эксплуатация", PersonnelNumber = "0023" };
        var e4 = new Employee { FullName = "Соколова Марина Викторовна", Position = "Медицинская сестра", Department = "Здравпункт", PersonnelNumber = "0031" };
        var e5 = new Employee { FullName = "Орлова Анна Павловна", Position = "Специалист по охране труда", Department = "Безопасность", PersonnelNumber = "0034" };
        var e6 = new Employee { FullName = "Васильев Игорь Андреевич", Position = "Электромонтёр", Department = "Эксплуатация", PersonnelNumber = "0045" };
        var e7 = new Employee { FullName = "Морозов Павел Сергеевич", Position = "Системный администратор", Department = "ИТ", PersonnelNumber = "0052" };
        var e8 = new Employee { FullName = "Лебедева Ольга Ивановна", Position = "Бухгалтер", Department = "Бухгалтерия", PersonnelNumber = "0061" };

        db.AddRange(gp7, r2, l1, ai2, ipp, commander, rescuer, medic, signaller, rescueUnit, medUnit, commUnit,
            e1, e2, e3, e4, e5, e6, e7, e8);

        // e8 намеренно без назначения — чтобы при формировании приказа было видно предупреждение
        db.AddRange(
            new NfgoAssignment { Employee = e1, Unit = rescueUnit, Role = commander },
            new NfgoAssignment { Employee = e2, Unit = rescueUnit, Role = rescuer },
            new NfgoAssignment { Employee = e3, Unit = rescueUnit, Role = rescuer },
            new NfgoAssignment { Employee = e4, Unit = medUnit, Role = commander },
            new NfgoAssignment { Employee = e5, Unit = medUnit, Role = medic },
            new NfgoAssignment { Employee = e6, Unit = commUnit, Role = signaller },
            new NfgoAssignment { Employee = e7, Unit = commUnit, Role = signaller });

        db.SaveChanges();
    }
}
