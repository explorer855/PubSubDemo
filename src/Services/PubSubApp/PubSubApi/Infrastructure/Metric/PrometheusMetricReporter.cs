﻿using Microsoft.Extensions.Logging;
using Prometheus;
using System;

namespace PubSubApi.Infrastructure.Metric
{
    public class PrometheusMetricReporter
    {
        private readonly ILogger<PrometheusMetricReporter> _logger;
        private readonly Counter _requestCounter;
        private readonly Histogram _responseTimeHistogram;

        public PrometheusMetricReporter(ILogger<PrometheusMetricReporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _requestCounter =
                Metrics.CreateCounter("total_requests", "The total number of requests serviced by this API.", new CounterConfiguration
                {
                    LabelNames = new[] { "method", "endpoint" }
                });

            _responseTimeHistogram = Metrics.CreateHistogram("request_duration_seconds",
                "The duration in seconds between the response to a request.", new HistogramConfiguration
                {
                    Buckets = Histogram.ExponentialBuckets(0.01, 2, 10),
                    LabelNames = new[] { "status_code", "method" }
                });
        }

        public void RegisterRequest()
        {
            _requestCounter.Inc();
        }

        public void RegisterResponseTime(int statusCode, string method, TimeSpan elapsed)
        {
            _responseTimeHistogram.Labels(statusCode.ToString(), method).Observe(elapsed.TotalSeconds);
        }
    }
}
