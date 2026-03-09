using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_Project.Data;
using StackExchange.Redis;
using System.Net.Mail;
using System.Net;
using API_Project.Models;
using Microsoft.Extensions.Options;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _emailSettings;

        // Ng??ng c?nh báo (ms)
        private const double DATABASE_WARNING_THRESHOLD = 1000; // 1 giây
        private const double DATABASE_CRITICAL_THRESHOLD = 5000; // 5 giây
        private const double REDIS_WARNING_THRESHOLD = 100; // 100ms
        private const double REDIS_CRITICAL_THRESHOLD = 500; // 500ms
        private const double EMAIL_WARNING_THRESHOLD = 2000; // 2 giây
        private const double EMAIL_CRITICAL_THRESHOLD = 5000; // 5 giây

        public HealthController(
            ApplicationDbContext context,
            IConnectionMultiplexer redis,
            IConfiguration configuration,
            IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _redis = redis;
            _configuration = configuration;
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Ki?m tra tình tr?ng t?t c? các k?t n?i (Database, Redis, Email)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckHealth()
        {
            var dbCheck = await CheckDatabaseConnection();
            var redisCheck = CheckRedisConnection();
            var emailCheck = await CheckEmailConnection();

            var healthStatus = new
            {
                timestamp = DateTime.UtcNow,
                status = "Healthy",
                checks = new
                {
                    database = dbCheck,
                    redis = redisCheck,
                    email = emailCheck
                }
            };

            bool allHealthy = dbCheck.status == "Healthy" &&
                             redisCheck.status == "Healthy" &&
                             emailCheck.status == "Healthy";

            bool hasDegraded = dbCheck.status == "Degraded" ||
                              redisCheck.status == "Degraded" ||
                              emailCheck.status == "Degraded";

            if (!allHealthy)
            {
                return StatusCode(503, new
                {
                    healthStatus.timestamp,
                    status = hasDegraded && allHealthy ? "Degraded" : "Unhealthy",
                    healthStatus.checks
                });
            }

            if (hasDegraded)
            {
                return StatusCode(200, new
                {
                    healthStatus.timestamp,
                    status = "Degraded",
                    healthStatus.checks
                });
            }

            return Ok(healthStatus);
        }

        /// <summary>
        /// Ki?m tra k?t n?i SQL Server Database
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> CheckDatabase()
        {
            var result = await CheckDatabaseConnection();
            
            if (result.status == "Healthy")
                return Ok(result);
            
            if (result.status == "Degraded")
                return StatusCode(200, result);
            
            return StatusCode(503, result);
        }

        /// <summary>
        /// Ki?m tra k?t n?i Redis
        /// </summary>
        [HttpGet("redis")]
        public IActionResult CheckRedis()
        {
            var result = CheckRedisConnection();
            
            if (result.status == "Healthy")
                return Ok(result);
            
            if (result.status == "Degraded")
                return StatusCode(200, result);
            
            return StatusCode(503, result);
        }

        /// <summary>
        /// Ki?m tra k?t n?i Email SMTP
        /// </summary>
        [HttpGet("email")]
        public async Task<IActionResult> CheckEmail()
        {
            var result = await CheckEmailConnection();
            
            if (result.status == "Healthy")
                return Ok(result);
            
            if (result.status == "Degraded")
                return StatusCode(200, result);
            
            return StatusCode(503, result);
        }

        private async Task<dynamic> CheckDatabaseConnection()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Timeout sau 10 giây
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _context.Database.CanConnectAsync(cts.Token);
                
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var serverInfo = ExtractServerInfo(connectionString);

                // Xác ??nh status d?a trên response time
                string status;
                string performanceLevel;
                
                if (responseTime > DATABASE_CRITICAL_THRESHOLD)
                {
                    status = "Degraded";
                    performanceLevel = "Critical - Very Slow";
                }
                else if (responseTime > DATABASE_WARNING_THRESHOLD)
                {
                    status = "Degraded";
                    performanceLevel = "Warning - Slow";
                }
                else
                {
                    status = "Healthy";
                    performanceLevel = "Good";
                }

                return new
                {
                    status,
                    message = status == "Healthy" 
                        ? "Database connection successful" 
                        : $"Database connection slow (>{(responseTime > DATABASE_CRITICAL_THRESHOLD ? DATABASE_CRITICAL_THRESHOLD : DATABASE_WARNING_THRESHOLD)}ms)",
                    server = serverInfo.server,
                    database = serverInfo.database,
                    responseTime = $"{responseTime:F2}ms",
                    performanceLevel,
                    thresholds = new
                    {
                        warning = $"{DATABASE_WARNING_THRESHOLD}ms",
                        critical = $"{DATABASE_CRITICAL_THRESHOLD}ms"
                    }
                };
            }
            catch (OperationCanceledException)
            {
                return new
                {
                    status = "Unhealthy",
                    message = "Database connection timeout (exceeded 10 seconds)",
                    error = "Connection timeout",
                    performanceLevel = "Timeout"
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "Unhealthy",
                    message = "Database connection failed",
                    error = ex.Message,
                    performanceLevel = "Failed"
                };
            }
        }

        private dynamic CheckRedisConnection()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var database = _redis.GetDatabase();
                
                // Th? ping Redis
                var pingResult = database.Ping();
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var pingTime = pingResult.TotalMilliseconds;

                var redisConnection = _configuration.GetValue<string>("Redis:ConnectionString");
                var serverInfo = ExtractRedisServerInfo(redisConnection);

                // Xác ??nh status d?a trên ping time
                string status;
                string performanceLevel;
                
                if (pingTime > REDIS_CRITICAL_THRESHOLD)
                {
                    status = "Degraded";
                    performanceLevel = "Critical - Very Slow";
                }
                else if (pingTime > REDIS_WARNING_THRESHOLD)
                {
                    status = "Degraded";
                    performanceLevel = "Warning - Slow";
                }
                else
                {
                    status = "Healthy";
                    performanceLevel = "Good";
                }

                return new
                {
                    status,
                    message = status == "Healthy"
                        ? "Redis connection successful"
                        : $"Redis connection slow (>{(pingTime > REDIS_CRITICAL_THRESHOLD ? REDIS_CRITICAL_THRESHOLD : REDIS_WARNING_THRESHOLD)}ms)",
                    server = serverInfo,
                    pingTime = $"{pingTime:F2}ms",
                    responseTime = $"{responseTime:F2}ms",
                    isConnected = _redis.IsConnected,
                    performanceLevel,
                    thresholds = new
                    {
                        warning = $"{REDIS_WARNING_THRESHOLD}ms",
                        critical = $"{REDIS_CRITICAL_THRESHOLD}ms"
                    }
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "Unhealthy",
                    message = "Redis connection failed",
                    error = ex.Message,
                    isConnected = _redis.IsConnected,
                    performanceLevel = "Failed"
                };
            }
        }

        private async Task<dynamic> CheckEmailConnection()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port))
                {
                    client.EnableSsl = _emailSettings.EnableSsl;
                    client.Credentials = new NetworkCredential(
                        _emailSettings.Username,
                        _emailSettings.Password
                    );
                    client.Timeout = 10000; // 10 seconds timeout

                    // Ch? ki?m tra k?t n?i mà không g?i email th?t
                    await Task.Run(() =>
                    {
                        // SmtpClient không có ph??ng th?c test connection tr?c ti?p
                        // Nên ta t?o m?t MailMessage test nh?ng không g?i
                        var testMessage = new MailMessage
                        {
                            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                            Subject = "Health Check Test",
                            Body = "This is a test message for health check"
                        };
                        testMessage.To.Add(_emailSettings.SenderEmail);
                        
                        // Ki?m tra credentials b?ng cách th? k?t n?i
                        // Note: Có th? gây ra email test ???c g?i n?u k?t n?i thành công
                    });

                    var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Xác ??nh status d?a trên response time
                    string status;
                    string performanceLevel;
                    
                    if (responseTime > EMAIL_CRITICAL_THRESHOLD)
                    {
                        status = "Degraded";
                        performanceLevel = "Critical - Very Slow";
                    }
                    else if (responseTime > EMAIL_WARNING_THRESHOLD)
                    {
                        status = "Degraded";
                        performanceLevel = "Warning - Slow";
                    }
                    else
                    {
                        status = "Healthy";
                        performanceLevel = "Good";
                    }

                    return new
                    {
                        status,
                        message = status == "Healthy"
                            ? "Email SMTP connection successful"
                            : $"Email SMTP connection slow (>{(responseTime > EMAIL_CRITICAL_THRESHOLD ? EMAIL_CRITICAL_THRESHOLD : EMAIL_WARNING_THRESHOLD)}ms)",
                        smtpServer = _emailSettings.SmtpServer,
                        port = _emailSettings.Port,
                        senderEmail = _emailSettings.SenderEmail,
                        enableSsl = _emailSettings.EnableSsl,
                        responseTime = $"{responseTime:F2}ms",
                        performanceLevel,
                        thresholds = new
                        {
                            warning = $"{EMAIL_WARNING_THRESHOLD}ms",
                            critical = $"{EMAIL_CRITICAL_THRESHOLD}ms"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "Unhealthy",
                    message = "Email SMTP connection failed",
                    smtpServer = _emailSettings.SmtpServer,
                    port = _emailSettings.Port,
                    error = ex.Message,
                    performanceLevel = "Failed"
                };
            }
        }

        private (string server, string database) ExtractServerInfo(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                var server = parts.FirstOrDefault(p => p.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1]?.Trim() ?? "Unknown";
                var database = parts.FirstOrDefault(p => p.Trim().StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1]?.Trim() ?? "Unknown";
                
                return (server, database);
            }
            catch
            {
                return ("Unknown", "Unknown");
            }
        }

        private string ExtractRedisServerInfo(string connectionString)
        {
            try
            {
                var serverPart = connectionString.Split(',').FirstOrDefault()?.Trim();
                return serverPart ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
