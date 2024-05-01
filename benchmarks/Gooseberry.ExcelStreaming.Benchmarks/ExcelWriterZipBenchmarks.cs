﻿using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Gooseberry.ExcelStreaming.Tests.ExternalZip;

namespace Gooseberry.ExcelStreaming.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ExcelWriterZipBenchmarks
{
    private const int ColumnBatchesCount = 10;

    [Benchmark]
    public async Task DefaultZip()
    {
        await using var outputStream = new NullStream();

        await using var writer = new ExcelWriter(new DefaultZipArchive(outputStream, CompressionLevel.Optimal));

        await writer.StartSheet("test");

        for (var row = 0; row < 100_000; row++)
        {
            await writer.StartRow();

            for (var columnBatch = 0; columnBatch < ColumnBatchesCount; columnBatch++)
            {
                writer.AddCell(row);
                writer.AddCell(DateTime.Now.Ticks);
                writer.AddCell(DateTime.Now);
                writer.AddCell(1234567.9876M);
                writer.AddCell("Tags such as <img> and <input> directly introduce content into the page.");
                writer.AddCell("The cat (Felis catus), commonly referred to as the domestic cat");
                writer.AddCellWithSharedString(
                    "The dog (Canis familiaris or Canis lupus familiaris) is a domesticated descendant of the wolf");
            }
        }

        await writer.Complete();
    }

    [Benchmark]
    public async Task SharpZipLib()
    {
        await using var outputStream = new NullStream();

        await using var writer = new ExcelWriter(new SharpZipLibArchive(outputStream));

        await writer.StartSheet("test");

        for (var row = 0; row < 100_000; row++)
        {
            await writer.StartRow();

            for (var columnBatch = 0; columnBatch < ColumnBatchesCount; columnBatch++)
            {
                writer.AddCell(row);
                writer.AddCell(DateTime.Now.Ticks);
                writer.AddCell(DateTime.Now);
                writer.AddCell(1234567.9876M);
                writer.AddCell("Tags such as <img> and <input> directly introduce content into the page.");
                writer.AddCell("The cat (Felis catus), commonly referred to as the domestic cat");
                writer.AddCellWithSharedString(
                    "The dog (Canis familiaris or Canis lupus familiaris) is a domesticated descendant of the wolf");
            }
        }

        await writer.Complete();
    }

    [Benchmark]
    public async Task SharpCompressLib()
    {
        await using var outputStream = new NullStream();

        await using var writer = new ExcelWriter(new SharpCompressZipArchive(outputStream));

        await writer.StartSheet("test");

        for (var row = 0; row < 100_000; row++)
        {
            await writer.StartRow();

            for (var columnBatch = 0; columnBatch < ColumnBatchesCount; columnBatch++)
            {
                writer.AddCell(row);
                writer.AddCell(DateTime.Now.Ticks);
                writer.AddCell(DateTime.Now);
                writer.AddCell(1234567.9876M);
                writer.AddCell("Tags such as <img> and <input> directly introduce content into the page.");
                writer.AddCell("The cat (Felis catus), commonly referred to as the domestic cat");
                writer.AddCellWithSharedString(
                    "The dog (Canis familiaris or Canis lupus familiaris) is a domesticated descendant of the wolf");
            }
        }

        await writer.Complete();
    }
}