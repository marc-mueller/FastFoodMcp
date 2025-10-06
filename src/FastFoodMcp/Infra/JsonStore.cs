using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FastFoodMcp.Infra;

/// <summary>
/// Generic JSON file store with hot-reload capability.
/// Watches the file for changes and automatically reloads the data.
/// </summary>
/// <typeparam name="T">The type of data to store.</typeparam>
public class JsonStore<T> : IDisposable where T : class
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private T? _data;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonStore(string filePath, ILogger logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // Load initial data
        LoadData();

        // Set up file watcher
        var directory = Path.GetDirectoryName(_filePath) ?? throw new InvalidOperationException("Invalid file path");
        var fileName = Path.GetFileName(_filePath);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _logger.LogInformation("JsonStore initialized for {FilePath}", _filePath);
    }

    /// <summary>
    /// Gets the current data. Thread-safe.
    /// </summary>
    public T Data
    {
        get
        {
            return _data ?? throw new InvalidOperationException("Data not loaded");
        }
    }

    private void LoadData()
    {
        try
        {
            _reloadLock.Wait();
            
            if (!File.Exists(_filePath))
            {
                _logger.LogError("Data file not found: {FilePath}", _filePath);
                throw new FileNotFoundException($"Data file not found: {_filePath}");
            }

            var json = File.ReadAllText(_filePath);
            var newData = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (newData == null)
            {
                _logger.LogError("Failed to deserialize data from {FilePath}", _filePath);
                throw new InvalidOperationException($"Failed to deserialize data from {_filePath}");
            }

            // Thread-safe swap
            Interlocked.Exchange(ref _data, newData);
            _logger.LogInformation("Loaded data from {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from {FilePath}", _filePath);
            throw;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("File changed detected: {FilePath}", e.FullPath);
        
        // Debounce: wait a bit for file write to complete
        Task.Delay(100).ContinueWith(_ =>
        {
            try
            {
                LoadData();
                _logger.LogInformation("Hot-reloaded data from {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hot-reloading data from {FilePath}", _filePath);
            }
        });
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _reloadLock?.Dispose();
    }
}
