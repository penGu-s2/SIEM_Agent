// Config module - quản lý tab Config và cấu hình Fluent Bit
// Sử dụng global variables thay vì ES6 modules để tương thích với script loading

let configManager = null;
let pendingFilePaths = null; // Lưu trữ file paths tạm thời

window.initConfigTab = function() {
    if (configManager) return;
    
    configManager = new ConfigManager();
    configManager.init();
    
    // Nếu có file paths đang chờ, cập nhật ngay
    if (pendingFilePaths) {
        console.log('📁 Applying pending file paths...');
        configManager.updateFilePaths(
            pendingFilePaths.configPath,
            pendingFilePaths.parsersPath,
            pendingFilePaths.logDir
        );
        pendingFilePaths = null;
    }
}

class ConfigManager {
    constructor() {
        this.configs = {};
        this.currentConfig = null;
    }
    
    init() {
        this.renderConfigTab();
        this.loadConfigurations();
    }
    
    renderConfigTab() {
        const container = document.getElementById('config-content');
        if (!container) return;
        
        container.innerHTML = `
            <div class="config-header" style="margin-bottom: 24px;">
                <h2 style="color: #F9FAFB; margin: 0 0 8px 0; font-size: 24px;">⚙️ Fluent Bit Configuration</h2>
                <p style="color: #D1D5DB; margin: 0; font-size: 14px;">Quản lý cấu hình Fluent Bit, backup và restore settings</p>
            </div>
            
            <div class="config-sections" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 20px;">
                <!-- Service Configuration -->
                <div class="config-section" style="background: #1F2937; border-radius: 8px; padding: 20px; border: 1px solid #374151;">
                    <h3 style="color: #F9FAFB; margin: 0 0 16px 0; font-size: 18px;">🔧 Service Configuration</h3>
                    <div class="config-item" style="margin-bottom: 12px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Flush Interval:</label>
                        <input type="number" id="flushInterval" value="1" min="1" max="3600" style="
                            width: 100%;
                            padding: 8px 12px;
                            border: 1px solid #4B5563;
                            border-radius: 6px;
                            background: #374151;
                            color: #F9FAFB;
                            font-size: 14px;
                        ">
                    </div>
                    <div class="config-item" style="margin-bottom: 12px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Log Level:</label>
                        <select id="logLevel" style="
                            width: 100%;
                            padding: 8px 12px;
                            border: 1px solid #4B5563;
                            border-radius: 6px;
                            background: #374151;
                            color: #F9FAFB;
                            font-size: 14px;
                        ">
                            <option value="error">Error</option>
                            <option value="warn">Warning</option>
                            <option value="info" selected>Info</option>
                            <option value="debug">Debug</option>
                            <option value="trace">Trace</option>
                        </select>
                    </div>
                    <div class="config-item" style="margin-bottom: 12px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Daemon Mode:</label>
                        <select id="daemonMode" style="
                            width: 100%;
                            padding: 8px 12px;
                            border: 1px solid #4B5563;
                            border-radius: 6px;
                            background: #374151;
                            color: #F9FAFB;
                            font-size: 14px;
                        ">
                            <option value="Off" selected>Off</option>
                            <option value="On">On</option>
                        </select>
                    </div>
                    <div class="config-item" style="margin-bottom: 16px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Parsers File:</label>
                        <input type="text" id="parsersFile" value="parsers.conf" style="
                            width: 100%;
                            padding: 8px 12px;
                            border: 1px solid #4B5563;
                            border-radius: 6px;
                            background: #374151;
                            color: #F9FAFB;
                            font-size: 14px;
                        ">
                    </div>
                    <button onclick="configManager.saveServiceConfig()" style="
                        width: 100%;
                        padding: 10px;
                        background: #3B82F6;
                        color: white;
                        border: none;
                        border-radius: 6px;
                        cursor: pointer;
                        font-size: 14px;
                        font-weight: 600;
                    ">💾 Lưu Service Config</button>
                </div>
                
                <!-- File Management -->
                <div class="config-section" style="background: #1F2937; border-radius: 8px; padding: 20px; border: 1px solid #374151;">
                    <h3 style="color: #F9FAFB; margin: 0 0 16px 0; font-size: 18px;">📁 File Management</h3>
                    <div class="config-item" style="margin-bottom: 12px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Config File:</label>
                        <div id="configFilePath" style="
                            padding: 8px 12px;
                            background: #374151;
                            border-radius: 6px;
                            color: #D1D5DB;
                            font-family: monospace;
                            font-size: 13px;
                        ">Đang tải...</div>
                    </div>
                    <div class="config-item" style="margin-bottom: 12px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Parsers File:</label>
                        <div id="parsersFilePath" style="
                            padding: 8px 12px;
                            background: #374151;
                            border-radius: 6px;
                            color: #D1D5DB;
                            font-family: monospace;
                            font-size: 13px;
                        ">Đang tải...</div>
                    </div>
                    <div class="config-item" style="margin-bottom: 12px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Log Directory:</label>
                        <div id="logDirectoryPath" style="
                            padding: 8px 12px;
                            background: #374151;
                            border-radius: 6px;
                            color: #D1D5DB;
                            font-family: monospace;
                            font-size: 13px;
                        ">Đang tải...</div>
                    </div>
                    <div class="config-buttons" style="display: flex; flex-direction: column; gap: 8px;">
                        <button onclick="configManager.backupConfig()" style="
                            padding: 10px;
                            background: #10B981;
                            color: white;
                            border: none;
                            border-radius: 6px;
                            cursor: pointer;
                            font-size: 14px;
                            font-weight: 600;
                        ">💾 Backup Configuration</button>
                        <button onclick="configManager.restoreConfig()" style="
                            padding: 10px;
                            background: #F59E0B;
                            color: white;
                            border: none;
                            border-radius: 6px;
                            cursor: pointer;
                            font-size: 14px;
                            font-weight: 600;
                        ">📂 Restore Configuration</button>
                        <button onclick="configManager.viewConfig()" style="
                            padding: 10px;
                            background: #8B5CF6;
                            color: white;
                            border: none;
                            border-radius: 6px;
                            cursor: pointer;
                            font-size: 14px;
                            font-weight: 600;
                        ">👁️ Xem Configuration</button>
                    </div>
                </div>
                
                <!-- Fluent Bit Control -->
                <div class="config-section" style="background: #1F2937; border-radius: 8px; padding: 20px; border: 1px solid #374151;">
                    <h3 style="color: #F9FAFB; margin: 0 0 16px 0; font-size: 18px;">🎮 Fluent Bit Control</h3>
                    <div class="config-item" style="margin-bottom: 16px;">
                        <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 8px;">Service Status:</label>
                        <div id="serviceStatus" style="
                            padding: 8px 12px;
                            background: #374151;
                            border-radius: 6px;
                            color: #D1D5DB;
                            font-size: 14px;
                            text-align: center;
                        ">🔄 Đang kiểm tra...</div>
                    </div>
                    <div class="config-buttons" style="display: flex; flex-direction: column; gap: 8px;">
                        <button onclick="configManager.startFluentBit()" style="
                            padding: 10px;
                            background: #10B981;
                            color: white;
                            border: none;
                            border-radius: 6px;
                            cursor: pointer;
                            font-size: 14px;
                            font-weight: 600;
                        ">▶️ Start Fluent Bit</button>
                        <button onclick="configManager.stopFluentBit()" style="
                            padding: 10px;
                            background: #EF4444;
                            color: white;
                            border: none;
                            border-radius: 6px;
                            cursor: pointer;
                            font-size: 14px;
                            font-weight: 600;
                        ">⏹️ Stop Fluent Bit</button>
                        <button onclick="configManager.restartFluentBit()" style="
                            padding: 10px;
                            background: #F59E0B;
                            color: white;
                            border: none;
                            border-radius: 6px;
                            cursor: pointer;
                            font-size: 14px;
                            font-weight: 600;
                        ">🔄 Restart Fluent Bit</button>
                    </div>
                </div>
                
                                 <!-- Config Sync Management -->
                 <div class="config-section" style="background: #1F2937; border-radius: 8px; padding: 20px; border: 1px solid #374151;">
                     <h3 style="color: #F9FAFB; margin: 0 0 16px 0; font-size: 18px;">🔄 Config Sync Management</h3>
                     <div class="config-item" style="margin-bottom: 12px;">
                         <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Sync Status:</label>
                         <div id="syncStatus" style="
                             padding: 8px 12px;
                             background: #374151;
                             border-radius: 6px;
                             color: #D1D5DB;
                             font-size: 14px;
                             text-align: center;
                         ">🔄 Đang kiểm tra...</div>
                     </div>
                     <div class="config-item" style="margin-bottom: 12px;">
                         <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Last Sync:</label>
                         <div id="lastSyncTime" style="
                             padding: 8px 12px;
                             background: #374151;
                             border-radius: 6px;
                             color: #D1D5DB;
                             font-family: monospace;
                             font-size: 13px;
                         ">Chưa đồng bộ</div>
                     </div>
                     <div class="config-item" style="margin-bottom: 12px;">
                         <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">API URL:</label>
                         <div id="syncApiUrl" style="
                             padding: 8px 12px;
                             background: #374151;
                             border-radius: 6px;
                             color: #D1D5DB;
                             font-family: monospace;
                             font-size: 13px;
                             word-break: break-all;
                         ">Đang tải...</div>
                     </div>
                     <div class="config-buttons" style="display: flex; flex-direction: column; gap: 8px;">
                         <button onclick="configManager.enableConfigSync()" style="
                             padding: 10px;
                             background: #10B981;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">✅ Bật Đồng Bộ</button>
                         <button onclick="configManager.disableConfigSync()" style="
                             padding: 10px;
                             background: #EF4444;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">❌ Tắt Đồng Bộ</button>
                         <button onclick="configManager.manualSync()" style="
                             padding: 10px;
                             background: #3B82F6;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">🔄 Đồng Bộ Ngay</button>
                                                   <button onclick="configManager.viewSyncLogs()" style="
                              padding: 10px;
                              background: #8B5CF6;
                              color: white;
                              border: none;
                              border-radius: 6px;
                              cursor: pointer;
                              font-size: 14px;
                              font-weight: 600;
                          ">📋 Xem Sync Logs</button>
                          <button onclick="configManager.refreshSyncStatus()" style="
                              padding: 10px;
                              background: #6B7280;
                              color: white;
                              border: none;
                              border-radius: 6px;
                              cursor: pointer;
                              font-size: 14px;
                              font-weight: 600;
                          ">🔄 Refresh Status</button>
                     </div>
                 </div>
                 
                 <!-- Logs & Monitoring -->
                 <div class="config-section" style="background: #1F2937; border-radius: 8px; padding: 20px; border: 1px solid #374151;">
                     <h3 style="color: #F9FAFB; margin: 0 0 16px 0; font-size: 18px;">📊 Logs & Monitoring</h3>
                     <div class="config-item" style="margin-bottom: 12px;">
                         <label style="color: #D1D5DB; font-size: 14px; display: block; margin-bottom: 4px;">Log Directory:</label>
                         <div style="
                             padding: 8px 12px;
                             background: #374151;
                             border-radius: 6px;
                             color: #D1D5DB;
                             font-family: monospace;
                             font-size: 13px;
                         ">.\\logs\\</div>
                     </div>
                     <div class="config-buttons" style="display: flex; flex-direction: column; gap: 8px;">
                         <button onclick="configManager.viewFluentBitLogs()" style="
                             padding: 10px;
                             background: #3B82F6;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">📋 Xem Fluent Bit Logs</button>
                         <button onclick="configManager.clearLogs()" style="
                             padding: 10px;
                             background: #EF4444;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">🗑️ Xóa Logs</button>
                         <button onclick="configManager.exportLogs()" style="
                             padding: 10px;
                             background: #10B981;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">📤 Export Logs</button>
                         <button onclick="configManager.restartApplication()" style="
                             padding: 10px;
                             background: #DC2626;
                             color: white;
                             border: none;
                             border-radius: 6px;
                             cursor: pointer;
                             font-size: 14px;
                             font-weight: 600;
                         ">🔄 Khởi động lại ứng dụng</button>
                     </div>
                 </div>
            </div>
            
            <!-- Configuration Preview -->
            <div class="config-preview" style="margin-top: 24px;">
                <h3 style="color: #F9FAFB; margin: 0 0 16px 0; font-size: 18px;">📋 Configuration Preview</h3>
                <div id="configPreview" style="
                    background: #111827;
                    border-radius: 8px;
                    padding: 20px;
                    border: 1px solid #374151;
                    font-family: 'Consolas', 'Monaco', monospace;
                    font-size: 13px;
                    color: #D1D5DB;
                    max-height: 400px;
                    overflow-y: auto;
                    white-space: pre-wrap;
                ">Đang tải configuration...</div>
            </div>
        `;
        
        this.attachEventListeners();
    }
    
    attachEventListeners() {
        // Có thể thêm các event listeners khác ở đây
    }
    
         loadConfigurations() {
         // Load service configuration
         this.loadServiceConfig();
         
         // Load Fluent Bit status
         this.checkFluentBitStatus();
         
         // Load configuration preview
         this.loadConfigPreview();
         
         // Load file paths từ cấu hình thực tế
         this.loadFilePaths();
         
         // Load config sync status
         this.loadConfigSyncStatus();
     }
    
    loadFilePaths() {
        console.log('📁 Loading file paths...');
        console.log('🔍 Config tab visible:', document.getElementById('config-content')?.style.display !== 'none');
        
        // Gửi message tới C# để lấy đường dẫn file thực tế
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'get_file_paths' });
            console.log('✅ get_file_paths message sent');
        } else {
            console.error('❌ WebView2 not available');
        }
    }
    
    // Method để cập nhật đường dẫn file từ C#
    updateFilePaths(configPath, parsersPath, logDir) {
        console.log('📁 Updating file paths:');
        console.log('- configPath:', configPath, 'Type:', typeof configPath);
        console.log('- parsersPath:', parsersPath, 'Type:', typeof parsersPath);
        console.log('- logDir:', logDir, 'Type:', typeof logDir);
        
        // Đợi một chút để đảm bảo HTML đã được render
        setTimeout(() => {
            const configPathElement = document.getElementById('configFilePath');
            const parsersPathElement = document.getElementById('parsersFilePath');
            const logDirElement = document.getElementById('logDirectoryPath');
            
            console.log('🔍 Looking for elements:');
            console.log('- configFilePath:', configPathElement);
            console.log('- parsersFilePath:', parsersPathElement);
            console.log('- logDirectoryPath:', logDirElement);
            
            if (configPathElement && configPath) {
                configPathElement.textContent = configPath;
                console.log('✅ Config path updated to:', configPath);
            } else {
                console.log('❌ Config path element not found or path is empty');
                console.log('Element:', configPathElement);
                console.log('Path:', configPath);
            }
            
            if (parsersPathElement && parsersPath) {
                parsersPathElement.textContent = parsersPath;
                console.log('✅ Parsers path updated to:', parsersPath);
            } else {
                console.log('❌ Parsers path element not found or path is empty');
                console.log('Element:', parsersPathElement);
                console.log('Path:', parsersPath);
            }
            
            if (logDirElement && logDir) {
                logDirElement.textContent = logDir;
                console.log('✅ Log directory updated to:', logDir);
            } else {
                console.log('❌ Log directory element not found or path is empty');
                console.log('Element:', logDirElement);
                console.log('Path:', logDir);
            }
        }, 100);
    }
    
    loadServiceConfig() {
        // Load từ C# hoặc từ file config
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'get_service_config' });
        }
    }
    
    checkFluentBitStatus() {
        console.log('🔍 Checking Fluent Bit status...');
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'check_fluentbit_status' });
        }
    }
    
         loadConfigPreview() {
         if (window.chrome && window.chrome.webview) {
             window.chrome.webview.postMessage({ action: 'get_config_preview' });
         }
     }
     
     // Config Sync Management Methods
     loadConfigSyncStatus() {
         if (window.chrome && window.chrome.webview) {
             window.chrome.webview.postMessage({ action: 'get_config_sync_status' });
         }
     }
     
     enableConfigSync() {
         if (confirm('Bạn có chắc muốn bật đồng bộ config từ API?\n\n⚠️ Hành động này sẽ:\n✅ Bật đồng bộ tự động\n✅ Tải config từ server\n✅ Cập nhật fluent-bit.conf')) {
             if (window.chrome && window.chrome.webview) {
                 window.chrome.webview.postMessage({ action: 'enable_config_sync' });
             }
             showNotification('Đang bật đồng bộ config...', 'info');
         }
     }
     
     disableConfigSync() {
         if (confirm('Bạn có chắc muốn tắt đồng bộ config?\n\n⚠️ Hành động này sẽ:\n❌ Tắt đồng bộ tự động\n❌ Không cập nhật config từ server')) {
             if (window.chrome && window.chrome.webview) {
                 window.chrome.webview.postMessage({ action: 'disable_config_sync' });
             }
             showNotification('Đang tắt đồng bộ config...', 'info');
         }
     }
     
     manualSync() {
         if (window.chrome && window.chrome.webview) {
             window.chrome.webview.postMessage({ action: 'manual_config_sync' });
         }
         showNotification('Đang đồng bộ config từ API...', 'info');
     }
     
           viewSyncLogs() {
          if (window.chrome && window.chrome.webview) {
              window.chrome.webview.postMessage({ action: 'view_sync_logs' });
          }
          showNotification('Đang tải sync logs...', 'info');
      }
      
      refreshSyncStatus() {
          if (window.chrome && window.chrome.webview) {
              window.chrome.webview.postMessage({ action: 'get_config_sync_status' });
          }
          showNotification('Đang cập nhật trạng thái...', 'info');
      }
     
           // Update Config Sync UI Methods
      updateConfigSyncStatus(syncData) {
          console.log('🔄 updateConfigSyncStatus called with:', syncData);
          console.log('🔄 syncData type:', typeof syncData);
          
          const statusElement = document.getElementById('syncStatus');
          const lastSyncElement = document.getElementById('lastSyncTime');
          const apiUrlElement = document.getElementById('syncApiUrl');
          
          console.log('🔍 Elements found:', {
              statusElement: !!statusElement,
              lastSyncElement: !!lastSyncElement,
              apiUrlElement: !!apiUrlElement
          });
          
          if (statusElement && lastSyncElement && apiUrlElement) {
              try {
                  const sync = typeof syncData === 'string' ? JSON.parse(syncData) : syncData;
                  console.log('🔄 Parsed sync data:', sync);
                 
                                                     // Update sync status
                  console.log('🔄 Updating sync status. enabled =', sync.enabled);
                  if (sync.enabled) {
                      statusElement.textContent = '🟢 Đang đồng bộ';
                      statusElement.style.color = '#10B981';
                      statusElement.style.fontWeight = 'bold';
                      console.log('✅ Status updated to: Đang đồng bộ');
                  } else {
                      statusElement.textContent = '🔴 Đã tắt';
                      statusElement.style.color = '#EF4444';
                      statusElement.style.fontWeight = 'bold';
                      console.log('✅ Status updated to: Đã tắt');
                  }
                  
                  // Update last sync time
                  console.log('🔄 Updating last sync time:', sync.lastSyncTime);
                  if (sync.lastSyncTime) {
                      lastSyncElement.textContent = sync.lastSyncTime;
                      console.log('✅ Last sync time updated to:', sync.lastSyncTime);
                  } else {
                      lastSyncElement.textContent = 'Chưa đồng bộ';
                      console.log('✅ Last sync time set to: Chưa đồng bộ');
                  }
                  
                  // Update API URL
                  console.log('🔄 Updating API URL:', sync.apiUrl);
                  if (sync.apiUrl) {
                      apiUrlElement.textContent = sync.apiUrl;
                      console.log('✅ API URL updated to:', sync.apiUrl);
                  } else {
                      apiUrlElement.textContent = 'Chưa cấu hình';
                      console.log('✅ API URL set to: Chưa cấu hình');
                  }
                 
                 console.log('✅ Config sync status updated:', sync);
             } catch (e) {
                 console.error('❌ Error parsing sync data:', e);
                 statusElement.textContent = '❓ Error loading status';
                 statusElement.style.color = '#EF4444';
             }
         }
     }
    
    // Service Configuration Methods
    saveServiceConfig() {
        const flushInterval = document.getElementById('flushInterval').value;
        const logLevel = document.getElementById('logLevel').value;
        const daemonMode = document.getElementById('daemonMode').value;
        const parsersFile = document.getElementById('parsersFile')?.value || 'parsers.conf';
        
        const config = {
            flushInterval: parseInt(flushInterval),
            logLevel: logLevel,
            daemonMode: daemonMode,
            parsersFile: parsersFile
        };
        
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({
                action: 'save_service_config',
                data: config
            });
        }
        showNotification('Đang lưu service configuration...', 'info');
    }
    
    // File Management Methods
    backupConfig() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'backup_config' });
        }
        showNotification('Đang tạo backup configuration...', 'info');
    }
    
    restoreConfig() {
        // Tạo file input để chọn file backup
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.conf,.backup';
        input.onchange = (e) => {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const content = e.target.result;
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage({ 
                            action: 'restore_config', 
                            content: content 
                        });
                    }
                    showNotification('Đang restore configuration...', 'info');
                };
                reader.readAsText(file);
            }
        };
        input.click();
    }
    
    viewConfig() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'view_config' });
        }
        showNotification('Đang tải configuration...', 'info');
    }
    
    // Fluent Bit Control Methods
    startFluentBit() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'start_fluentbit' });
        }
        showNotification('Đang khởi động Fluent Bit...', 'info');
    }
    
    stopFluentBit() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'stop_fluentbit' });
        }
        showNotification('Đang dừng Fluent Bit...', 'info');
    }
    
    restartFluentBit() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'restart_fluentbit' });
        }
        showNotification('Đang restart Fluent Bit...', 'info');
    }
    
    restartApplication() {
        if (confirm('Bạn có chắc muốn khởi động lại ứng dụng? Tất cả dữ liệu chưa lưu sẽ bị mất.')) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ action: 'restart_application' });
            }
            showNotification('Đang khởi động lại ứng dụng...', 'info');
        }
    }
    
    // Logs & Monitoring Methods
    viewFluentBitLogs() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'view_fluentbit_logs' });
        }
        showNotification('Đang tải Fluent Bit logs...', 'info');
    }
    
    clearLogs() {
        if (confirm('Bạn có chắc muốn xóa tất cả logs?\n\n⚠️ Hành động này sẽ:\n❌ Xóa tất cả log files\n❌ Không thể hoàn tác')) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ action: 'clear_logs' });
            }
            showNotification('Đang xóa logs...', 'info');
        }
    }
    
    exportLogs() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'export_logs' });
        }
        showNotification('Đang export logs...', 'info');
    }
    
    // Update UI Methods
    updateServiceStatus(statusData) {
        const statusElement = document.getElementById('serviceStatus');
        if (statusElement) {
            try {
                // Parse status data từ C#
                const status = typeof statusData === 'string' ? JSON.parse(statusData) : statusData;
                
                let statusText = '';
                let statusColor = '';
                
                if (status.isRunning) {
                    statusText = `🟢 Running (${status.processCount} processes)`;
                    statusColor = '#10B981';
                } else {
                    statusText = '🔴 Stopped';
                    statusColor = '#EF4444';
                }
                
                // Thêm thông tin chi tiết nếu có
                if (status.processes && status.processes.length > 0) {
                    const process = status.processes[0];
                    statusText += `\nID: ${process.id} | Memory: ${process.memoryUsage.toFixed(1)}MB`;
                }
                
                statusElement.textContent = statusText;
                statusElement.style.color = statusColor;
                
                console.log('✅ Service status updated:', status);
            } catch (e) {
                console.error('❌ Error parsing status data:', e);
                statusElement.textContent = '❓ Error loading status';
                statusElement.style.color = '#EF4444';
            }
        }
    }
    
    updateConfigPreview(content) {
        const previewElement = document.getElementById('configPreview');
        if (previewElement) {
            previewElement.textContent = content;
        }
    }
    
    // Method để cập nhật service config từ C#
    updateServiceConfig(config) {
        if (config.flushInterval) {
            document.getElementById('flushInterval').value = config.flushInterval;
        }
        if (config.logLevel) {
            document.getElementById('logLevel').value = config.logLevel;
        }
        if (config.daemonMode) {
            document.getElementById('daemonMode').value = config.daemonMode;
        }
        if (config.parsersFile) {
            document.getElementById('parsersFile').value = config.parsersFile;
        }
    }
    
    // Method để force update file paths
    forceUpdateFilePaths() {
        if (pendingFilePaths) {
            console.log('📁 Force updating file paths...');
            this.updateFilePaths(
                pendingFilePaths.configPath,
                pendingFilePaths.parsersPath,
                pendingFilePaths.logDir
            );
        }
    }
}

// Expose functions globally
window.updateServiceConfigFromCSharp = function(config) {
    if (configManager) {
        configManager.updateServiceConfig(config);
    }
};

window.updateFluentBitStatusFromCSharp = function(status) {
    if (configManager) {
        configManager.updateServiceStatus(status);
    }
};

window.updateConfigPreviewFromCSharp = function(content) {
    if (configManager) {
        configManager.updateConfigPreview(content);
    }
};

// Expose function để C# cập nhật đường dẫn file
window.updateFilePathsFromCSharp = function(filePaths) {
    if (configManager && filePaths) {
        configManager.updateFilePaths(
            filePaths.configPath,
            filePaths.parsersPath,
            filePaths.logDir
        );
    }
};

// Expose function với tên mới để tránh conflict
window.updateFilePathsFromCSharpForConfigModule = function(filePaths) {
    console.log('📁 Received file paths from C#:', filePaths);
    console.log('🔍 configManager status:', configManager);
    console.log('🔍 Current active tab:', document.querySelector('.sidebar-item.active')?.getAttribute('data-tab'));
    
    // Parse JSON nếu cần
    let parsedFilePaths = filePaths;
    if (typeof filePaths === 'string') {
        try {
            parsedFilePaths = JSON.parse(filePaths);
            console.log('📁 Parsed file paths:', parsedFilePaths);
        } catch (e) {
            console.error('❌ Error parsing file paths JSON:', e);
            return;
        }
    }
    
    if (configManager && parsedFilePaths) {
        console.log('📁 Using parsed file paths:', parsedFilePaths);
        configManager.updateFilePaths(
            parsedFilePaths.configPath,
            parsedFilePaths.parsersPath,
            parsedFilePaths.logDir
        );
    } else {
        console.log('❌ configManager not available or filePaths is empty');
        
        // Lưu file paths để sử dụng sau
        if (parsedFilePaths) {
            pendingFilePaths = parsedFilePaths;
            console.log('📁 File paths saved for later use');
        }
        
        // Nếu configManager chưa có, thử khởi tạo lại
        if (!configManager) {
            console.log('🔄 Trying to initialize configManager...');
            window.initConfigTab();
        }
    }
};

// Expose force update function globally
window.forceUpdateConfigFilePaths = function() {
    if (configManager) {
        configManager.forceUpdateFilePaths();
    }
};

// Expose config sync functions globally
window.updateConfigSyncStatusFromCSharp = function(syncData) {
    if (configManager) {
        configManager.updateConfigSyncStatus(syncData);
    }
};



// Initialize config tab when clicked
document.addEventListener('DOMContentLoaded', function() {
    const configTab = document.querySelector('.sidebar-item[data-tab="config"]');
    if (configTab) {
        configTab.addEventListener('click', function() {
            if (!configManager) {
                window.initConfigTab();
            } else {
                // Nếu configManager đã có, thử force update file paths
                configManager.forceUpdateFilePaths();
            }
        });
    }
});
