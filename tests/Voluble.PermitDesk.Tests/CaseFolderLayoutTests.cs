using Voluble.PermitDesk.Web.Files;

namespace Voluble.PermitDesk.Tests;

public class CaseFolderLayoutTests
{
    [Fact]
    public void SubFolders_in_documented_order()
    {
        Assert.Equal(
            ["01_Material", "02_Ansokan", "03_Bekraftelser", "04_Beslut", "05_Ovrigt"],
            CaseFolderLayout.SubFolders);
    }

    [Theory]
    [InlineData("Storgatan 4 Borgholm", "Storgatan 4 Borgholm")]
    [InlineData("Drottninggatan 1/3", "Drottninggatan 1 3")]
    [InlineData("Vägen: Norra\\Södra", "Vägen Norra Södra")]
    [InlineData("Åsa Östergatan 12", "Åsa Östergatan 12")]
    [InlineData("  spaces   collapse  ", "spaces collapse")]
    [InlineData("trailing dots...", "trailing dots")]
    public void SanitizeFolderName_removes_invalid_chars_and_collapses_whitespace(string input, string expected)
    {
        Assert.Equal(expected, CaseFolderLayout.SanitizeFolderName(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\\\\:::***")]
    public void SanitizeFolderName_returns_Untitled_for_empty_or_only_invalid(string input)
    {
        Assert.Equal("Untitled", CaseFolderLayout.SanitizeFolderName(input));
    }

    [Theory]
    [InlineData("DJI_0001.jpg", "DJI_0001.jpg")]
    [InlineData("My\\drone/photo:final.MP4", "My_drone_photo_final.MP4")]
    [InlineData("Åsa Östermans drönarbild.jpg", "Åsa Östermans drönarbild.jpg")]
    [InlineData("trailing space .pdf", "trailing space .pdf")]
    public void SanitizeFileName_replaces_invalid_chars(string input, string expected)
    {
        Assert.Equal(expected, CaseFolderLayout.SanitizeFileName(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeFileName_falls_back_to_untitled(string input)
    {
        Assert.Equal("untitled", CaseFolderLayout.SanitizeFileName(input));
    }

    [Fact]
    public void SubFolderFor_maps_each_category()
    {
        Assert.Equal("01_Material", CaseFolderLayout.SubFolderFor(Web.Domain.CaseFileCategory.Material));
        Assert.Equal("02_Ansokan", CaseFolderLayout.SubFolderFor(Web.Domain.CaseFileCategory.Application));
        Assert.Equal("03_Bekraftelser", CaseFolderLayout.SubFolderFor(Web.Domain.CaseFileCategory.SubmissionConfirmation));
        Assert.Equal("04_Beslut", CaseFolderLayout.SubFolderFor(Web.Domain.CaseFileCategory.Decision));
        Assert.Equal("05_Ovrigt", CaseFolderLayout.SubFolderFor(Web.Domain.CaseFileCategory.Other));
    }
}
