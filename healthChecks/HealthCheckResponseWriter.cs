public static class HealthCheckResponseWriter
{
    public static Task WriteEnhancedHtmlResponse(HttpContext context, HealthReport report)
    {
        var statusColor = report.Status switch
        {
            HealthStatus.Healthy => "#4CAF50",
            HealthStatus.Degraded => "#FFC107",
            HealthStatus.Unhealthy => "#F44336",
            _ => "#9E9E9E"
        };

        var checksHtml = new StringBuilder();
        foreach (var entry in report.Entries)
        {
            var entryColor = entry.Value.Status switch
            {
                HealthStatus.Healthy => "#4CAF50",
                HealthStatus.Degraded => "#FFC107",
                HealthStatus.Unhealthy => "#F44336",
                _ => "#9E9E9E"
            };

            checksHtml.Append($@"
            <div class='check-card'>
                <div class='check-status' style='background-color:{entryColor}'></div>
                <div class='check-details'>
                    <h3>{entry.Key}</h3>
                    <div class='metrics'>
                        <span class='metric'><i class='fas fa-clock'></i> {entry.Value.Duration.TotalMilliseconds} ms</span>
                        <span class='metric'><i class='fas fa-info-circle'></i> {entry.Value.Description ?? "No description"}</span>
                    </div>
                    {(entry.Value.Exception != null ? $@"
                    <div class='error-details'>
                        <button class='toggle-error' onclick='toggleErrorDetails(this)'>Show Error</button>
                        <div class='error-message' style='display:none'>
                            <pre>{System.Web.HttpUtility.HtmlEncode(entry.Value.Exception.ToString())}</pre>
                        </div>
                    </div>" : "")}
                </div>
            </div>");
        }

        var html = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Health Status Dashboard</title>
            <link href='https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap' rel='stylesheet'>
            <link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0-beta3/css/all.min.css' rel='stylesheet'>
            <style>
                :root {{
                    --healthy: #4CAF50;
                    --degraded: #FFC107;
                    --unhealthy: #F44336;
                    --text-primary: #2C3E50;
                    --text-secondary: #7F8C8D;
                    --bg-primary: #FFFFFF;
                    --bg-secondary: #F5F7FA;
                    --border: #E0E0E0;
                }}

                body {{
                    font-family: 'Roboto', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: var(--bg-secondary);
                    color: var(--text-primary);
                    line-height: 1.6;
                }}

                .dashboard {{
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 20px;
                }}

                .header {{
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 30px;
                    padding-bottom: 20px;
                    border-bottom: 1px solid var(--border);
                }}

                .status-badge {{
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }}

                .status-indicator {{
                    width: 20px;
                    height: 20px;
                    border-radius: 50%;
                    background-color: {statusColor};
                    box-shadow: 0 0 10px {statusColor};
                }}

                h1, h2, h3 {{
                    margin: 0;
                    font-weight: 500;
                }}

                h1 {{
                    font-size: 24px;
                }}

                h2 {{
                    font-size: 18px;
                    color: var(--text-secondary);
                }}

                .stats-container {{
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                    gap: 20px;
                    margin-bottom: 30px;
                }}

                .stat-card {{
                    background-color: var(--bg-primary);
                    border-radius: 8px;
                    padding: 20px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.05);
                    text-align: center;
                }}

                .stat-value {{
                    font-size: 32px;
                    font-weight: 700;
                    margin: 10px 0;
                }}

                .stat-label {{
                    color: var(--text-secondary);
                    font-size: 14px;
                }}

                .checks-container {{
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
                    gap: 20px;
                }}

                .check-card {{
                    background-color: var(--bg-primary);
                    border-radius: 8px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.05);
                    display: flex;
                    overflow: hidden;
                }}

                .check-status {{
                    width: 8px;
                    flex-shrink: 0;
                }}

                .check-details {{
                    padding: 15px;
                    flex-grow: 1;
                }}

                .metrics {{
                    display: flex;
                    gap: 15px;
                    margin-top: 10px;
                    font-size: 14px;
                    color: var(--text-secondary);
                }}

                .metric i {{
                    margin-right: 5px;
                }}

                .error-details {{
                    margin-top: 10px;
                }}

                .toggle-error {{
                    background: none;
                    border: none;
                    color: #3498DB;
                    cursor: pointer;
                    padding: 0;
                    font-size: 14px;
                }}

                .error-message {{
                    margin-top: 10px;
                    padding: 10px;
                    background-color: #F8F9FA;
                    border-radius: 4px;
                    border-left: 3px solid var(--unhealthy);
                    font-family: monospace;
                    font-size: 13px;
                    white-space: pre-wrap;
                    word-break: break-all;
                }}

                .history-chart {{
                    background-color: var(--bg-primary);
                    border-radius: 8px;
                    padding: 20px;
                    margin-top: 30px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.05);
                    height: 200px;
                }}

                .last-updated {{
                    text-align: right;
                    font-size: 12px;
                    color: var(--text-secondary);
                    margin-top: 20px;
                }}

                @media (max-width: 768px) {{
                    .stats-container {{
                        grid-template-columns: 1fr;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='dashboard'>
                <div class='header'>
                    <div>
                        <h1>Service Health Dashboard</h1>
                        <h2>{context.Request.Path}</h2>
                    </div>
                    <div class='status-badge'>
                        <div class='status-indicator'></div>
                        <span>{report.Status}</span>
                    </div>
                </div>

                <div class='stats-container'>
                    <div class='stat-card'>
                        <div class='stat-label'>Overall Status</div>
                        <div class='stat-value' style='color:{statusColor}'>{report.Status}</div>
                    </div>
                    <div class='stat-card'>
                        <div class='stat-label'>Total Checks</div>
                        <div class='stat-value'>{report.Entries.Count}</div>
                    </div>
                    <div class='stat-card'>
                        <div class='stat-label'>Healthy Checks</div>
                        <div class='stat-value' style='color:var(--healthy)'>{report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy)}</div>
                    </div>
                    <div class='stat-card'>
                        <div class='stat-label'>Response Time</div>
                        <div class='stat-value'>{report.TotalDuration.TotalMilliseconds} ms</div>
                    </div>
                </div>

                <div class='checks-container'>
                    {checksHtml}
                </div>

                <div class='history-chart'>
                    <h3>Health History (Last 10 Checks)</h3>
                    <canvas id='healthHistoryChart'></canvas>
                </div>

                <div class='last-updated'>
                    Last updated: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} UTC
                </div>
            </div>

            <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
            <script>
                // Store current health status in history
                const healthHistory = JSON.parse(localStorage.getItem('healthHistory') || '{{""timestamps"": [], ""statuses"": []}}');
                
                // Add current status
                healthHistory.timestamps.push(new Date().toLocaleTimeString());
                healthHistory.statuses.push('{report.Status}' === 'Healthy' ? 1 : ('{report.Status}' === 'Degraded' ? 0.5 : 0));
                
                // Keep only last 10 entries
                if (healthHistory.timestamps.length > 10) {{
                    healthHistory.timestamps.shift();
                    healthHistory.statuses.shift();
                }}
                
                localStorage.setItem('healthHistory', JSON.stringify(healthHistory));
                
                // Render chart
                const ctx = document.getElementById('healthHistoryChart').getContext('2d');
                new Chart(ctx, {{
                    type: 'line',
                    data: {{
                        labels: healthHistory.timestamps,
                        datasets: [{{
                            label: 'Health Status',
                            data: healthHistory.statuses,
                            borderColor: '{statusColor}',
                            backgroundColor: '{statusColor}33',
                            borderWidth: 2,
                            tension: 0.1,
                            pointRadius: 4,
                            pointBackgroundColor: '{statusColor}'
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {{
                            y: {{
                                min: 0,
                                max: 1,
                                ticks: {{
                                    callback: function(value) {{
                                        return value === 1 ? 'Healthy' : (value === 0.5 ? 'Degraded' : 'Unhealthy');
                                    }}
                                }}
                            }}
                        }}
                    }}
                }});
                
                function toggleErrorDetails(button) {{
                    const errorDetails = button.nextElementSibling;
                    if (errorDetails.style.display === 'none') {{
                        errorDetails.style.display = 'block';
                        button.textContent = 'Hide Error';
                    }} else {{
                        errorDetails.style.display = 'none';
                        button.textContent = 'Show Error';
                    }}
                }}
            </script>
        </body>
        </html>";

        context.Response.ContentType = "text/html";
        return context.Response.WriteAsync(html);
    }
}