using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QueueReader;

internal sealed class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;

    public Worker(ILogger<Worker> logger, IConfiguration config, IHostApplicationLifetime applicationLifetime, HttpClient httpClient, TelemetryClient telemetryClient)
    {
        _logger = logger;
        _config = config;
        _applicationLifetime = applicationLifetime;
        _httpClient = httpClient;
        _telemetryClient = telemetryClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var client = GetQueueClient();

            var storeUrl = GetStoreUrl();

            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    using var operation = _telemetryClient.StartOperation<DependencyTelemetry>("receive-message");
                        
                    QueueMessage message = await client.ReceiveMessageAsync(cancellationToken: stoppingToken);

                    if (message == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation($"Message ID: '{message.MessageId}', contents: '{message.Body?.ToString()}'");
                        
                        
                    await _httpClient.PostAsync(storeUrl, JsonContent.Create(new { Id = message.MessageId, Message = message.Body?.ToString() }), stoppingToken);

                    await client.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

                }
                catch (Azure.RequestFailedException rfe)
                {
                    if (rfe.ErrorCode == "QueueNotFound")
                    {
                        _logger.LogInformation($"Queue '{client.Name}' does not exist. Waiting..");
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    }
                    else
                    {
                        _logger.LogError($"Something went wrong connecting to the queue: {rfe}");
                    }
                }
                catch (HttpRequestException hre)
                {
                    _logger.LogError($"Something went wrong writing to the store: {hre.Message}");
                }
            }

        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Queue reader is shutting down..");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
        finally
        {
            _applicationLifetime.StopApplication();
        }
    }


    /// <summary>
    /// Creates a QueueClient or throws if there are input errors.
    /// </summary>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="FormatException" />
    /// <returns></returns>
    private QueueClient GetQueueClient()
    {
        string connectionString = this._config["QueueConnectionString"];
        string queueName = this._config["QueueName"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException("'QueueConnectionString' config value is required. Please add an environemnt variable or app setting.");
        }

        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentNullException("'QueueName' config value is required. Please add an environemnt variable or app setting.");
        }

        _logger.LogInformation($"Waiting for messages on '{queueName}'.");

        return new QueueClient(connectionString, queueName);
    }

    /// <summary>
    /// Gets the URL for the orders app.
    /// </summary>
    /// <returns></returns>
    private Uri GetStoreUrl()
    {
        string daprPort = this._config["DAPR_HTTP_PORT"];
        string targetApp = this._config["TargetApp"];

        if (string.IsNullOrEmpty(daprPort))
        {
            throw new ArgumentNullException("'DaprPort' config value is required. Please add an environment variable or app setting.");
        }

        if (string.IsNullOrEmpty(targetApp))
        {
            throw new ArgumentNullException("'TargetApp' config value is required. Please add an environment variable or app setting.");
        }

        Uri storeUrl = new Uri($"http://localhost:{daprPort}/v1.0/invoke/{targetApp}/method/store");

        _logger.LogInformation($"Ready to send messages to '{storeUrl}'.");

        return storeUrl;
    }
}
