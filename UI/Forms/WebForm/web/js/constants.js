// Constants và mappings cho dashboard
// Expose globally để tương thích với script loading

// Mapping loại input và các trường cần nhập
window.INPUT_FIELD_MAP = {
    winlog: ["Tag", "Channels", "Interval_Sec", "DB"],
    syslog: ["Tag", "Listen", "Port", "Mode", "Parser"],
    http: ["Tag", "Host", "Port"],
    tail: ["Path", "Tag"],
    dummy: ["Tag", "Dummy", "Samples"],
    blob: ["Tag", "Path", "Pattern"],
    kubernetes_events: ["Tag", "Kube_Config_Path"],
    kafka: ["Tag", "Brokers", "Topics"],
    fluentbit_metrics: ["Tag", "Scrape_Interval"],
    prometheus_scrape: ["Tag", "Host", "Port", "Metrics_Path"],
    statsd: ["Tag", "Listen", "Port"],
    opentelemetry: ["Tag", "Listen", "Port"],
    elasticsearch: ["Tag", "Host", "Port", "Index"],
    splunk: ["Tag", "Host", "Port", "Token"],
    prometheus_remote_write: ["Tag", "Host", "Port", "HTTP_User", "HTTP_Passwd"],
    event_type: ["Tag", "Event_Type"],
    nginx_metrics: ["Tag", "Host", "Port", "Metrics_Path"],
    winstat: ["Tag", "Interval_Sec"],
    winevtlog: ["Tag", "Channels", "Interval_Sec"],
    windows_exporter_metrics: ["Tag", "Host", "Port", "Metrics_Path"]
};

// Màu sắc cho trạng thái
window.STATUS_COLORS = {
    running: '#4CAF50',
    stopped: '#F44336',
    error: '#FF9800',
    unknown: '#9E9E9E'
};

// Nhãn cho trạng thái
window.STATUS_LABELS = {
    running: 'Active',
    stopped: 'Inactive',
    error: 'Error',
    unknown: 'Unknown'
};

// Các loại notification
window.NOTIFICATION_TYPES = {
    SUCCESS: 'success',
    ERROR: 'error',
    WARNING: 'warning',
    INFO: 'info'
};

// Các action cho C# communication
window.ACTIONS = {
    // Collector actions
    ADD_COLLECTOR: 'add_collector',
    ADD_COLLECTOR_WITH_OUTPUT: 'add_collector_with_output',
    EDIT_COLLECTOR: 'edit_collector',
    DELETE_COLLECTOR: 'delete_collector',
    TOGGLE_COLLECTOR: 'toggle_collector',
    
    // Parser actions
    GET_PARSERS: 'get_parsers',
    ADD_PARSER: 'add_parser',
    EDIT_PARSER: 'edit_parser',
    DELETE_PARSER: 'delete_parser',
    
    // Log actions
    GET_LOG_TYPES: 'get_log_types',
    LOAD_LOGS: 'load_logs',
    VIEW_LOG_FILE: 'view_log_file',
    
    // Config actions
    GET_SERVICE_CONFIG: 'get_service_config',
    SAVE_SERVICE_CONFIG: 'save_service_config',
    CHECK_FLUENTBIT_STATUS: 'check_fluentbit_status',
    GET_CONFIG_PREVIEW: 'get_config_preview',
    BACKUP_CONFIG: 'backup_config',
    RESTORE_CONFIG: 'restore_config',
    VIEW_CONFIG: 'view_config',
    START_FLUENTBIT: 'start_fluentbit',
    STOP_FLUENTBIT: 'stop_fluentbit',
    RESTART_FLUENTBIT: 'restart_fluentbit',
    VIEW_FLUENTBIT_LOGS: 'view_fluentbit_logs',
    CLEAR_LOGS: 'clear_logs',
    EXPORT_LOGS: 'export_logs',
    GET_FILE_PATHS: 'get_file_paths',
    GET_LOG_FILE_PATH: 'get_log_file_path',
    
    // Config Sync actions
    GET_CONFIG_SYNC_STATUS: 'get_config_sync_status',
    ENABLE_CONFIG_SYNC: 'enable_config_sync',
    DISABLE_CONFIG_SYNC: 'disable_config_sync',
    MANUAL_CONFIG_SYNC: 'manual_config_sync',
    VIEW_SYNC_LOGS: 'view_sync_logs',
    REFRESH_SYNC_STATUS: 'refresh_sync_status',
    
    // Other actions
    GET_COLLECTORS: 'get_collectors',
    GET_PARSERS: 'get_parsers',
    GET_LOGS: 'get_logs'
};

// Các loại output được hỗ trợ
window.OUTPUT_TYPES = {
    FILE: 'file',
    OPENSEARCH: 'opensearch',
    HTTP: 'http',
    FORWARD: 'forward'
};

// Cấu hình mặc định cho từng loại output
window.OUTPUT_DEFAULTS = {
    file: {
        Path: '.\\logs\\',
        Format: 'plain',
        Retry_Limit: '3'
    },
    opensearch: {
        Host: 'localhost',
        Port: '9200',
        Index: 'logs'
    },
    http: {
        Host: 'localhost',
        Port: '8080',
        URI: '/logs'
    },
    forward: {
        Host: 'localhost',
        Port: '24224'
    }
};
