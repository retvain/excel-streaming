﻿using System.Drawing;
using System.Reflection;
using System.Text;
using Gooseberry.ExcelStreaming.Styles;
using Gooseberry.ExcelStreaming.Tests.ExternalZip;
using Xunit;

namespace Gooseberry.ExcelStreaming.Tests;

public sealed class ExcelFilesGenerator
{
    private const string? Skip = "Null me for manual run";
    private const string? IgnoreZip = "ignore";

    const string BasePath = "c:\\temp\\excelWriter\\";

    private static readonly Stream Picture = Assembly.GetExecutingAssembly().GetManifestResourceStream(
        "Gooseberry.ExcelStreaming.Tests.Resources.Images.Humpty_Dumpty.jpg")!;

    [Theory(Skip = Skip)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Basic(bool async)
    {
        var suffix = !async ? "_sync" : "";
        await using var outputStream = new FileStream(BasePath + $"Basic{suffix}.xlsx", FileMode.Create);

        await GenerateContent(outputStream);
    }

    [Fact(Skip = IgnoreZip)]
    public async Task Basic_SharpZipLib()
    {
        await using var outputStream = new FileStream(BasePath + "Basic_SharpZipLib.xlsx", FileMode.Create);

        await GenerateContent(archive: new SharpZipLibArchive(outputStream));
    }

    [Fact(Skip = IgnoreZip)]
    public async Task Basic_SharpCompress()
    {
        await using var outputStream = new FileStream(BasePath + "Basic_SharpCompress.xlsx", FileMode.Create);

        await GenerateContent(archive: new SharpCompressZipArchive(outputStream));
    }

    private static async Task GenerateContent(
        Stream? stream = null,
        IZipArchive? archive = null,
        bool async = true)
    {
        var sharedStringTableBuilder = new SharedStringTableBuilder();
        var sharedStringRef1 = sharedStringTableBuilder.GetOrAdd("‰>>> Shared string with specialЪ & symbols <<‰< Я");
        var sharedStringRef2 = sharedStringTableBuilder.GetOrAdd("“Tell us a story!” said the March Hare.‰");
        var sharedStringTable = sharedStringTableBuilder.Build();

        await using var writer = stream != null
            ? new ExcelWriter(stream!, sharedStringTable: sharedStringTable, async: async)
            : new ExcelWriter(archive!, sharedStringTable: sharedStringTable, async: async);

        for (var sheetIndex = 0; sheetIndex < 3; sheetIndex++)
        {
            await writer.StartSheet($"test#{sheetIndex}");

            for (var i = 0; i < 10; i++)
                await writer.StartRow();

            writer.AddPicture(Picture, PictureFormat.Jpeg, new AnchorCell(3, 1), new Size(100, 130));
            writer.AddPicture(Picture, PictureFormat.Jpeg, new AnchorCell(10, 1), new AnchorCell(15, 10));


            for (var row = 0; row < 10_000; row++)
            {
                await writer.StartRow();

                for (var columnBatch = 0; columnBatch < 2; columnBatch++)
                {
                    writer.AddEmptyCell();
                    writer.AddCell(row);
                    writer.AddCell(DateTime.Now.Ticks);
                    writer.AddCell(DateTime.Now);
                    writer.AddCell(1234567.9876M);
                    writer.AddCell("Tags such as ‰<img> and <input>‰ directly introduce content into the page.");
                    writer.AddCell("The cat (Felis catus), commonly referred to as the domestic cat");
                    writer.AddEmptyCell();
                    writer.AddCellWithSharedString(
                        "The dog (Canis familiaris or Canis lupus familiaris) is a domesticated descendant of the wolf");
                    writer.AddUtf8Cell("Utf8 string with <tags>"u8);
                    writer.AddCell("String as chars".AsSpan());
                    writer.AddCell(new Hyperlink("https://github.com/gooseberrysoft/excel-streaming", "Excel Streaming"));
                    writer.AddCell(sharedStringRef1);
                    writer.AddCell(sharedStringRef2);
                }
            }
        }

        await writer.Complete();
    }

    [Fact(Skip = Skip)]
    public async Task Primitive()
    {
        await using var outputStream = new FileStream(BasePath + "Primitive.xlsx", FileMode.Create);

        await using var writer = new ExcelWriter(outputStream);

        for (var sheetIndex = 0; sheetIndex < 3; sheetIndex++)
        {
            await writer.StartSheet($"test#{sheetIndex}");

            writer.AddEmptyRows(3);
            for (var row = 0; row < 10_000; row++)
            {
                await writer.StartRow();

                for (var columnBatch = 0; columnBatch < 2; columnBatch++)
                {
                    writer.AddEmptyCells(2);
                    writer.AddCell(row);
                    writer.AddCell(DateTime.Now.Ticks);
                    writer.AddCell(DateTime.Now);
                    writer.AddCell(1234567.9876M);
                    writer.AddCell("‰Tags such as <img> and <input> directly introduce content into the page.");
                    writer.AddUtf8Cell("Utf8 string with <tags>‰"u8);
                    writer.AddCell("String as chars".AsSpan());
                    writer.AddEmptyCells(2);
                    writer.AddCell('&');
                    writer.AddCell('!');
                }
            }

            writer.AddEmptyRows(3);
        }

        await writer.Complete();
    }

    [Fact(Skip = null)]
    public async Task IncreaseColumns()
    {
        await using var outputStream = new FileStream(BasePath + "IncreaseColumns.xlsx", FileMode.Create);

        await using var writer = new ExcelWriter(outputStream);

        await writer.StartSheet($"test#1", new SheetConfiguration(FrozenRows: 3));

        for (var row = 0; row < 1000; row++)
        {
            await writer.StartRow();

            for (var column = 0; column < row % 16_384; column++)
            {
                writer.AddCell(column + row);
            }
        }

        await writer.Complete();
    }

    [Theory(Skip = Skip)]
    [InlineData('‰', "ThreeBytes")]
    [InlineData('Я', "TwoBytes")]
    [InlineData('x', "OneByte")]
    public async Task IncreaseValue(char symbol, string suffix)
    {
        var symbolBytes = Encoding.UTF8.GetBytes(new[] { symbol });
        await using var outputStream = new FileStream(BasePath + $"IncreaseValue_{suffix}.xlsx", FileMode.Create);

        await using var writer = new ExcelWriter(outputStream);

        await writer.StartSheet($"test#1");

        for (var row = 0; row < 15; row++)
        {
            await writer.StartRow();

            for (var column = 0; column < 1_024; column++)
            {
                WriteUtf8Cell(writer, symbolBytes, column);
            }
        }

        await writer.Complete();
    }

    private static void WriteUtf8Cell(ExcelWriter writer, ReadOnlySpan<byte> symbolBytes, int column)
    {
        Span<byte> span = new byte[symbolBytes.Length * (column + 1)];

        for (int i = 0; i <= column; i++)
        {
            symbolBytes.CopyTo(span.Slice(i * symbolBytes.Length));
        }

        writer.AddUtf8Cell(span);
    }


    [Fact(Skip = Skip)]
    public async Task Styles()
    {
        await using var outputStream = new FileStream(BasePath + "Styles.xlsx", FileMode.Create);

        var styleBuilder = new StylesSheetBuilder();
        var style1 = styleBuilder.GetOrAdd(
            new Style(
                Format: "0.00%",
                Font: new Font(Size: 24, Color: Color.DodgerBlue),
                Fill: new Fill(Color: Color.Crimson, FillPattern.Gray125),
                Borders: new Borders(
                    Left: new Border(BorderStyle.Thick, Color.BlueViolet),
                    Right: new Border(BorderStyle.MediumDashed, Color.Coral)),
                Alignment: new Alignment(HorizontalAlignment.Center, VerticalAlignment.Center, false)
            ));

        var style2 = styleBuilder.GetOrAdd(
            new Style(
                Font: new Font(Size: 16),
                Fill: new Fill(Color: Color.Bisque, FillPattern.Solid),
                Borders: new Borders(
                    Top: new Border(BorderStyle.Thin, Color.CornflowerBlue),
                    Bottom: new Border(BorderStyle.Thin, Color.Crimson)),
                Alignment: new Alignment(HorizontalAlignment.Right, VerticalAlignment.Bottom, false)
            ));

        var styleSheet = styleBuilder.Build();

        await using var writer = new ExcelWriter(outputStream, styleSheet);

        await writer.StartSheet("Stylish");

        for (var row = 0; row < 10_000; row++)
        {
            await writer.StartRow();

            writer.AddCell(row, style1);
            writer.AddCell(DateTime.Now.Ticks);
            writer.AddCell(DateTime.Now);
            writer.AddCell(1234567.9876M);
            writer.AddCell("Tags such as <img> and <input> directly introduce content into the page.", style2);
            writer.AddCell("The cat (Felis catus), commonly referred to as the domestic cat");
            writer.AddCellWithSharedString(
                "The dog (Canis familiaris or Canis lupus familiaris) is a domesticated descendant of the wolf");
            writer.AddUtf8Cell("Utf8 string with <tags>"u8);
            writer.AddCell("String as chars".AsSpan());
            writer.AddCell(new Hyperlink("https://github.com/gooseberrysoft/excel-streaming", "Excel Streaming"), style1);
        }

        writer.AddPicture(Picture, PictureFormat.Jpeg, new AnchorCell(3, 10_001), new Size(100, 130));


        await writer.Complete();
    }
}