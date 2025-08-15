// Main entry point - import và khởi tạo tất cả modules
console.log('🚀 Loading SIEM Agent Dashboard...');

document.addEventListener('DOMContentLoaded', () => {
    console.log('🚀 Khởi tạo SIEM Agent Dashboard...');
    try {
        // Expose functions cho C# gọi
        window.updateCollectorsFromCSharp = function(jsonData) {
            console.log('📊 updateCollectorsFromCSharp called with:', jsonData);
            console.log('📊 JSON Data type:', typeof jsonData);
            console.log('📊 JSON Data length:', jsonData ? jsonData.length : 'null');
            console.log('📊 JSON Data preview:', jsonData ? jsonData.substring(0, 100) + '...' : 'null');
            
            try {
                // Gọi trực tiếp hàm renderCollectors từ dashboard.js
                if (typeof window.renderCollectorsFromDashboardModule === 'function') {
                    console.log('🔄 Calling renderCollectors function from dashboard module...');
                    const data = JSON.parse(jsonData);
                    window.renderCollectorsFromDashboardModule(data);
                    console.log('✅ Collectors rendered successfully');
                } else {
                    console.error('❌ renderCollectorsFromDashboardModule function not available');
                }
            } catch (e) {
                console.error('Error in updateCollectorsFromCSharp:', e);
                console.error('Error details:', e.message);
                console.error('Stack trace:', e.stack);
            }
        };
        
        // Expose parser functions cho C# gọi
        window.updateParsersFromCSharp = function(jsonData) {
            console.log('📊 updateParsersFromCSharp called with:', jsonData);
            try {
                if (typeof window.updateParsersFromCSharpForParserModule === 'function') {
                    console.log('🔄 Calling updateParsersFromCSharpForParserModule...');
                    window.updateParsersFromCSharpForParserModule(jsonData);
                    console.log('✅ Parsers updated successfully');
                } else if (typeof window.parserManager !== 'undefined' && window.parserManager) {
                    console.log('🔄 Calling parserManager.renderParsers...');
                    window.parserManager.renderParsers(jsonData);
                    console.log('✅ Parsers updated successfully');
                } else {
                    console.error('❌ Parser functions not available');
                }
            } catch (e) {
                console.error('❌ Error in updateParsersFromCSharp:', e);
            }
        };
        
        // Expose logs functions cho C# gọi
        window.updateLogsFromCSharp = function(jsonData) {
            console.log('📊 updateLogsFromCSharp called with:', jsonData);
            try {
                if (typeof window.updateLogsFromCSharpForLogsModule === 'function') {
                    window.updateLogsFromCSharpForLogsModule(jsonData);
                } else {
                    console.error('❌ Logs functions not available');
                }
            } catch (e) {
                console.error('❌ Error in updateLogsFromCSharp:', e);
            }
        };
        
        // Expose log types functions cho C# gọi
        window.updateLogTypesFromCSharp = function(jsonData) {
            console.log('📊 updateLogTypesFromCSharp called with:', jsonData);
            try {
                if (typeof window.updateLogTypesFromCSharpForLogsModule === 'function') {
                    window.updateLogTypesFromCSharpForLogsModule(jsonData);
                } else {
                    console.error('❌ updateLogTypesFromCSharpForLogsModule function not available');
                }
            } catch (e) {
                console.error('❌ Error in updateLogTypesFromCSharp:', e);
            }
        };
        
        // Expose Fluent Bit status functions cho C# gọi
        window.updateFluentBitStatus = function(status) {
            console.log('📊 updateFluentBitStatus called with:', status);
            try {
                // Cập nhật status cho logs module
                if (typeof window.updateFluentBitStatusFromLogsModule === 'function') {
                    window.updateFluentBitStatusFromLogsModule(status);
                }
                // Cập nhật status cho config tab nếu đang mở
                if (typeof window.updateFluentBitStatusFromCSharp === 'function') {
                    window.updateFluentBitStatusFromCSharp(status);
                }
                console.log('✅ Fluent Bit status updated');
            } catch (e) {
                console.error('❌ Error in updateFluentBitStatus:', e);
            }
        };
        
        // Expose debug info functions cho C# gọi
        window.showDebugInfo = function(jsonData) {
            console.log('📊 showDebugInfo called with:', jsonData);
            try {
                // Gọi trực tiếp hàm showDebugInfo từ logs.js
                if (typeof window.showDebugInfoFromLogsModule === 'function') {
                    window.showDebugInfoFromLogsModule(jsonData);
                    console.log('✅ Debug info shown successfully');
                } else {
                    console.error('❌ showDebugInfoFromLogsModule function not available');
                }
            } catch (e) {
                console.error('Error in showDebugInfo:', e);
            }
        };
        
        // Expose parser popup functions cho C# gọi
        window.openEditParserPopup = function(parserJson, idx) {
            console.log('📊 openEditParserPopup called with:', parserJson, idx);
            try {
                const parser = typeof parserJson === 'string' ? JSON.parse(parserJson) : parserJson;
                // Gọi trực tiếp hàm openEditParserPopup từ parsers.js
                if (typeof window.openEditParserPopupFromParsersModule === 'function') {
                    window.openEditParserPopupFromParsersModule(parser, idx);
                    console.log('✅ Parser popup opened successfully');
                } else {
                    console.error('❌ openEditParserPopupFromParsersModule function not available');
                }
            } catch (e) {
                console.error('Parser JSON không hợp lệ:', e);
            }
        };
        
        // Expose parser names functions cho C# gọi
        window.setParserNameOptions = function(parserNames) {
            try {
                window.availableParserNames = parserNames;
                console.log('✅ Parser names set successfully');
            } catch (e) {
                console.error('Error in setParserNameOptions:', e);
            }
        };

        // Expose collector action functions cho C# gọi
        window.showAddCollectorPopup = function() {
            if (typeof window.showAddCollectorPopupFromActionsModule === 'function') {
                window.showAddCollectorPopupFromActionsModule();
            } else {
                console.error('❌ showAddCollectorPopupFromActionsModule function not available');
            }
        };

        window.showEditCollectorPopup = function(collector, idx) {
            if (typeof window.showEditCollectorPopupFromActionsModule === 'function') {
                window.showEditCollectorPopupFromActionsModule(collector, idx);
            } else {
                console.error('❌ showEditCollectorPopupFromActionsModule function not available');
            }
        };

        window.deleteCollector = function(idx) {
            if (typeof window.deleteCollectorFromActionsModule === 'function') {
                window.deleteCollectorFromActionsModule(idx);
            } else {
                console.error('❌ deleteCollectorFromActionsModule function not available');
            }
        };

        // Expose sendMessageToCSharp function cho C# gọi
        window.sendMessageToCSharp = function(action, data = {}) {
            if (typeof window.sendMessageToCSharpFromActionsModule === 'function') {
                window.sendMessageToCSharpFromActionsModule(action, data);
            } else {
                console.error('❌ sendMessageToCSharpFromActionsModule function not available');
            }
        };

        // Expose function để C# cập nhật đường dẫn file
        window.updateFilePathsFromCSharp = function(filePaths) {
            if (typeof window.updateFilePathsFromCSharpForConfigModule === 'function') {
                window.updateFilePathsFromCSharpForConfigModule(filePaths);
            } else {
                console.error('❌ updateFilePathsFromCSharpForConfigModule function not available');
            }
        };

        // Expose function để C# cập nhật đường dẫn log file
        window.updateLogFilePathFromCSharp = function(logType, filePath) {
            if (typeof window.updateLogFilePathFromCSharpForLogsModule === 'function') {
                window.updateLogFilePathFromCSharpForLogsModule(logType, filePath);
            } else {
                console.error('❌ updateLogFilePathFromCSharpForLogsModule function not available');
            }
        };

        console.log('✅ Dashboard đã sẵn sàng với đầy đủ chức năng!');

    } catch (error) {
        console.error('❌ Lỗi khởi tạo dashboard:', error);
        alert('Có lỗi khởi tạo dashboard: ' + error.message);
    }
});
