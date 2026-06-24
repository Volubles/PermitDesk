using System.Text;
using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class PdfSharpMailRendererTests
{
    private readonly PdfSharpMailRenderer _renderer = new();

    [Fact]
    public async Task RenderAsync_produces_a_valid_pdf_byte_stream()
    {
        var mail = NewMail(
            subject: "Beslut: Spridningstillstånd beviljat - FBO-2026-0001",
            body: "Hej,\n\nLantmäteriet har beslutat att bevilja spridningstillstånd för " +
                  "det material som lämnats in i ärende FBO-2026-0001.\n\n" +
                  "Materialet får spridas i enlighet med villkoren.\n\nMed vänlig hälsning,\nLantmäteriet");

        var bytes = await _renderer.RenderAsync(mail);

        // Magic bytes
        Assert.True(bytes.Length > 1000, "PDF unexpectedly small");
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);

        // PDF trailer
        var tail = Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length));
        Assert.Contains("%%EOF", tail);
    }

    [Fact]
    public async Task RenderAsync_handles_long_bodies_by_paginating()
    {
        var longBody = string.Join("\n", Enumerable.Range(1, 200)
            .Select(i => $"Rad {i}: Lantmäteriet behandlar nu detta ärende vidare. Innehåll fortsätter."));
        var mail = NewMail("Långt mejl FBO-2026-0001", longBody);

        var bytes = await _renderer.RenderAsync(mail);

        Assert.True(bytes.Length > 1000);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task RenderAsync_handles_empty_body()
    {
        var mail = NewMail("Bara ämne, ingen kropp", "");
        var bytes = await _renderer.RenderAsync(mail);
        Assert.True(bytes.Length > 500);
    }

    [Fact]
    public async Task RenderAsync_handles_swedish_characters()
    {
        var mail = NewMail(
            subject: "Åäö ÅÄÖ — räksmörgås",
            body: "Hej Östermans,\n\nTack för ditt mejl. Spridningstillståndet beviljas.\n\nLantmäteriet");
        var bytes = await _renderer.RenderAsync(mail);
        Assert.True(bytes.Length > 500);
    }

    private static InboundMail NewMail(string subject, string body)
        => new(
            MessageId: $"render-test-{Guid.NewGuid():N}@test",
            From: "spridningstillstand@lantmateriet.se",
            Subject: subject,
            TextBody: body,
            HtmlBody: null,
            ReceivedAt: DateTimeOffset.UtcNow,
            SourcePath: "/test");
}
