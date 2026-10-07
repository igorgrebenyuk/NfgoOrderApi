using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NfgoOrderApi.DTOs;

namespace NfgoOrderApi.Services;

public interface IOrderDocumentBuilder
{
    /// <summary>Строит Word-документ (.docx) приказа и возвращает его байты.</summary>
    byte[] Build(OrderDto order);
}

/// <summary>Формирует текст приказа о создании НФГО в формате .docx (библиотека DocumentFormat.OpenXml).</summary>
public class OrderDocumentBuilder : IOrderDocumentBuilder
{
    private const string FontName = "Times New Roman";
    private const string BodySize = "28";   // 14 pt (размер в half-points)
    private const string TableSize = "20";  // 10 pt

    public byte[] Build(OrderDto o)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body!;

            // --- шапка ---
            body.Append(Para(o.Organization.ToUpperInvariant(), bold: true, align: JustificationValues.Center, after: 240));
            body.Append(Para("ПРИКАЗ", bold: true, align: JustificationValues.Center, after: 120));
            body.Append(Para($"от {o.OrderDate:dd.MM.yyyy} № {o.Number}", align: JustificationValues.Center, after: 240));
            body.Append(Para("О создании нештатных формирований гражданской обороны",
                bold: true, align: JustificationValues.Center, after: 360));

            // --- преамбула ---
            body.Append(Para(
                $"В целях обеспечения выполнения мероприятий по гражданской обороне и поддержания сил в готовности, " +
                $"{o.Basis},",
                align: JustificationValues.Both, firstLineIndent: 709, after: 120));
            body.Append(Para("ПРИКАЗЫВАЮ:", bold: true, align: JustificationValues.Left, after: 120));

            // --- п. 1: состав формирований ---
            body.Append(Para(
                $"1. Создать в {o.Organization} следующие нештатные формирования гражданской обороны (НФГО):",
                align: JustificationValues.Both, firstLineIndent: 709, after: 60));
            foreach (var u in o.Units)
                body.Append(Para($"– {u.UnitName} — {u.MembersCount} чел.",
                    align: JustificationValues.Both, leftIndent: 1134, after: 40));

            // --- п. 2: персональный состав ---
            body.Append(Para(
                "2. Утвердить персональный состав НФГО и закрепление за личным составом средств индивидуальной защиты (таблица 1).",
                align: JustificationValues.Both, firstLineIndent: 709, before: 120, after: 120));
            body.Append(Para("Таблица 1 — Персональный состав НФГО и закреплённые СИЗ",
                align: JustificationValues.Left, size: TableSize, after: 60, keepNext: true));
            body.Append(MembersTable(o));

            // --- п. 3: потребность в СИЗ ---
            body.Append(Para(
                "3. Выдать личному составу НФГО средства индивидуальной защиты по нормам, установленным для соответствующих ролей. " +
                "Общая потребность в СИЗ приведена в таблице 2.",
                align: JustificationValues.Both, firstLineIndent: 709, before: 200, after: 120));
            body.Append(Para("Таблица 2 — Общая потребность в СИЗ",
                align: JustificationValues.Left, size: TableSize, after: 60, keepNext: true));
            body.Append(TotalsTable(o));

            // --- п. 4–6 ---
            body.Append(Para(
                "4. Руководителям формирований обеспечить подготовку личного состава и поддержание формирований в готовности.",
                align: JustificationValues.Both, firstLineIndent: 709, before: 200, after: 120));
            body.Append(Para(
                "5. Ответственному за учёт СИЗ поставить выданные средства индивидуальной защиты на учёт в карточках учёта СИЗ " +
                "и осуществлять контроль сроков их годности.",
                align: JustificationValues.Both, firstLineIndent: 709, after: 120));
            body.Append(Para(
                "6. Контроль за исполнением настоящего приказа оставляю за собой.",
                align: JustificationValues.Both, firstLineIndent: 709, after: 480));

            // --- подпись ---
            body.Append(SignatureBlock(o));

            body.Append(new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 },
                new PageMargin
                {
                    Top = 1134, Right = 850, Bottom = 1134, Left = 1701,
                    Header = 708, Footer = 708, Gutter = 0
                }));

            main.Document.Save();
        }
        return ms.ToArray();
    }

    // ---------- таблицы ----------

    private static Table MembersTable(OrderDto o)
    {
        int[] widths = { 500, 2000, 1700, 1500, 1500, 2155 }; // сумма = 9355 (ширина области текста)
        var table = NewTable(widths);

        table.Append(HeaderRow(widths, "№", "ФИО", "Должность", "Формирование", "Роль в НФГО", "Закреплённые СИЗ"));

        var n = 1;
        foreach (var m in o.Members)
        {
            var siz = m.SizItems.Count == 0
                ? "—"
                : string.Join("; ", m.SizItems.Select(s => $"{s.SizName} — {s.Quantity} {s.Unit}"));
            table.Append(Row(widths, JustificationValues.Left,
                n++.ToString(), m.EmployeeName, m.Position, m.UnitName, m.RoleName, siz));
        }
        return table;
    }

    private static Table TotalsTable(OrderDto o)
    {
        int[] widths = { 600, 5455, 1500, 1800 };
        var table = NewTable(widths);
        table.Append(HeaderRow(widths, "№", "Наименование СИЗ", "Ед. изм.", "Количество"));

        var n = 1;
        foreach (var t in o.SizTotals)
            table.Append(Row(widths, JustificationValues.Left, n++.ToString(), t.SizName, t.Unit, t.Total.ToString()));

        if (o.SizTotals.Count == 0)
            table.Append(Row(widths, JustificationValues.Left, "—", "СИЗ не закреплены", "—", "—"));
        return table;
    }

    private static Table NewTable(int[] widths)
    {
        var table = new Table();
        table.Append(new TableProperties(
            new TableWidth { Width = widths.Sum().ToString(), Type = TableWidthUnitValues.Dxa },
            new TableBorders(
                Border<TopBorder>(), Border<LeftBorder>(), Border<BottomBorder>(),
                Border<RightBorder>(), Border<InsideHorizontalBorder>(), Border<InsideVerticalBorder>()),
            new TableLayout { Type = TableLayoutValues.Fixed }));

        var grid = new TableGrid();
        foreach (var w in widths) grid.Append(new GridColumn { Width = w.ToString() });
        table.Append(grid);
        return table;
    }

    private static T Border<T>() where T : BorderType, new() =>
        new() { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" };

    private static TableRow HeaderRow(int[] widths, params string[] cells)
    {
        var row = new TableRow(new TableRowProperties(new TableHeader()));
        for (var i = 0; i < cells.Length; i++)
            row.Append(Cell(widths[i], cells[i], JustificationValues.Center, bold: true));
        return row;
    }

    private static TableRow Row(int[] widths, JustificationValues align, params string[] cells)
    {
        var row = new TableRow(new TableRowProperties(new CantSplit()));
        for (var i = 0; i < cells.Length; i++)
            row.Append(Cell(widths[i], cells[i], i == 0 ? JustificationValues.Center : align, bold: false));
        return row;
    }

    private static TableCell Cell(int width, string text, JustificationValues align, bool bold) =>
        new(new TableCellProperties(new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa }),
            Para(text, bold: bold, align: align, size: TableSize, after: 0));

    private static Table SignatureBlock(OrderDto o)
    {
        int[] widths = { 4677, 4678 };
        var table = new Table();
        table.Append(new TableProperties(
            new TableWidth { Width = "9355", Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed }));
        var grid = new TableGrid();
        foreach (var w in widths) grid.Append(new GridColumn { Width = w.ToString() });
        table.Append(grid);

        table.Append(new TableRow(
            new TableCell(new TableCellProperties(new TableCellWidth { Width = "4677", Type = TableWidthUnitValues.Dxa }),
                Para(o.HeadPosition, align: JustificationValues.Left, after: 0)),
            new TableCell(new TableCellProperties(new TableCellWidth { Width = "4678", Type = TableWidthUnitValues.Dxa }),
                Para($"__________________ / {o.HeadFullName} /", align: JustificationValues.Right, after: 0))));
        return table;
    }

    // ---------- абзацы ----------

    private static Paragraph Para(
        string text,
        bool bold = false,
        JustificationValues? align = null,
        string size = BodySize,
        int before = 0,
        int after = 0,
        int firstLineIndent = 0,
        int leftIndent = 0,
        bool keepNext = false)
    {
        var props = new ParagraphProperties();
        if (keepNext) props.Append(new KeepNext());
        props.Append(new SpacingBetweenLines { Before = before.ToString(), After = after.ToString(), Line = "276", LineRule = LineSpacingRuleValues.Auto });
        if (firstLineIndent != 0 || leftIndent != 0)
            props.Append(new Indentation
            {
                Left = leftIndent.ToString(),
                FirstLine = firstLineIndent != 0 ? firstLineIndent.ToString() : null
            });
        props.Append(new Justification { Val = align ?? JustificationValues.Left });

        var runProps = new RunProperties(new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName });
        if (bold) runProps.Append(new Bold());
        runProps.Append(new FontSize { Val = size });

        var run = new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return new Paragraph(props, run);
    }
}
