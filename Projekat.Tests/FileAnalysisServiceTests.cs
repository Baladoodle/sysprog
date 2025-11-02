using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Projekat.Services;

namespace Projekat.Tests;

public class FileAnalysisServiceTests : IDisposable
{
    private readonly FileAnalysisService _svc;
    private readonly Mock<ILogger<FileAnalysisService>> _logMock;
  private readonly string _tmpDir;

    public FileAnalysisServiceTests()
    {
    // Setup za testove - privremeni direktorijum
    _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
 Directory.CreateDirectory(_tmpDir);
  _logMock = new Mock<ILogger<FileAnalysisService>>();
   _svc = new FileAnalysisService(_logMock.Object);
    }

  [Fact]
    public async Task AnalyzeFile_ReturnsError_WhenFileNotFound()
    {
// Arrangemenet
      var fileName = "nepostojeci.txt";

   // Act
        var result = await _svc.AnalyzeFileAsync(fileName, _tmpDir);

   // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("nije prona?en", result.Error);
  }

    [Fact]
    public async Task AnalyzeFile_ReturnsEmpty_WhenFileIsEmpty()
    {
        // Arrangement
        var fileName = "prazan.txt";
        var filePath = Path.Combine(_tmpDir, fileName);
  await File.WriteAllTextAsync(filePath, "");

        // Act
  var result = await _svc.AnalyzeFileAsync(fileName, _tmpDir);

   // Assert
      Assert.True(result.IsSuccess);
   Assert.True(result.IsEmpty);
        Assert.Contains("prazan", result.Message, StringComparison.OrdinalIgnoreCase);
    }

  [Fact]
  public async Task AnalyzeFile_CalculatesCorrectAverage()
    {
  // Arrangement
var fileName = "test.txt";
 var content = "Ovo je test fajl sa nekoliko reci"; // 6 re?i
        var filePath = Path.Combine(_tmpDir, fileName);
  await File.WriteAllTextAsync(filePath, content);
        // O?ekivana prose?na dužina: (3+2+4+4+3+7+4) / 7 = 3.85...

        // Act
        var result = await _svc.AnalyzeFileAsync(fileName, _tmpDir);

    // Assert
        Assert.True(result.IsSuccess);
  Assert.False(result.IsEmpty);
    Assert.True(result.AverageWordLength > 0);
  Assert.True(result.WordCount > 0);
    }

    [Fact]
    public async Task AnalyzeFile_FindsFileInSubfolder()
    {
// Arrangement
   var subFolder = Path.Combine(_tmpDir, "podfolder");
 Directory.CreateDirectory(subFolder);
 var fileName = "skriveni.txt";
        var filePath = Path.Combine(subFolder, fileName);
  await File.WriteAllTextAsync(filePath, "Test sadrzaj");

    // Act
      var result = await _svc.AnalyzeFileAsync(fileName, _tmpDir);

        // Assert
Assert.True(result.IsSuccess);
  Assert.Equal(fileName, result.FileName);
    }

[Fact]
    public async Task AnalyzeFile_ReturnsError_WhenFileNameIsEmpty()
{
        // Act
     var result = await _svc.AnalyzeFileAsync("", _tmpDir);

     // Assert
      Assert.False(result.IsSuccess);
        Assert.Contains("prazan", result.Error, StringComparison.OrdinalIgnoreCase);
    }

[Fact]
    public async Task AnalyzeFile_ReturnsError_OnPathTraversalAttempt()
    {
        // Arrangement
  var maliciousFileName = "../../sensitive.txt";

        // Act
  var result = await _svc.AnalyzeFileAsync(maliciousFileName, _tmpDir);

        // Assert
  Assert.False(result.IsSuccess);
   Assert.Contains("Nevaže?e", result.Error);
    }

    // Cleanup nakon testova
    public void Dispose()
    {
        try
  {
 if (Directory.Exists(_tmpDir))
       Directory.Delete(_tmpDir, true);
     }
  catch { /* Ignoriši greške pri brisanju */ }
    }
}
