// Utility functions cho dashboard
// Expose globally để tương thích với script loading

// Gửi message sang C# qua WebView2
window.sendMessageToCSharp = function(action, data = {}) {
    console.log('📤 Sending message to C#:', action, data);
    
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action, ...data });
        console.log('✅ Message sent via WebView2');
    } else {
        console.warn('⚠️ WebView2 not available, using fallback');
        // Fallback cho development
        console.log('📤 Fallback message:', { action, ...data });
    }
}

// Tạo element với attributes và children
window.createElement = function(tag, attributes = {}, children = []) {
    const element = document.createElement(tag);
    
    // Set attributes
    Object.keys(attributes).forEach(key => {
        if (key === 'style' && typeof attributes[key] === 'object') {
            Object.assign(element.style, attributes[key]);
        } else {
            element.setAttribute(key, attributes[key]);
        }
    });
    
    // Add children
    children.forEach(child => {
        if (typeof child === 'string') {
            element.appendChild(document.createTextNode(child));
        } else {
            element.appendChild(child);
        }
    });
    
    return element;
}

// Hiển thị alert với styling
window.showAlert = function(message, type = 'info') {   
    console.log('📢 Alert:', message, type);
    
    const alertDiv = document.createElement('div');
    alertDiv.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 12px 24px;
        border-radius: 8px;
        color: #fff;
        z-index: 10000;
        background: ${type === 'error' ? '#F44336' : 
                   type === 'success' ? '#4CAF50' : 
                   type === 'warning' ? '#FF9800' : '#2196F3'};
        max-width: 300px;
        word-wrap: break-word;
    `;
    alertDiv.textContent = message;
    
    document.body.appendChild(alertDiv);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.parentNode.removeChild(alertDiv);
        }
    }, 3000);
}

// Xác nhận action
window.confirmAction = function(message) {
    return confirm(message);
}

// Debounce function
window.debounce = function(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Format date
window.formatDate = function(date) {
    if (!date) return '';
    
    const d = new Date(date);
    return d.toLocaleString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
}

// Format file size
window.formatFileSize = function(bytes) {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Validate input fields
window.validateFields = function(fields, data) {
    const errors = [];
    
    fields.forEach(field => {
        if (!data[field] || data[field].toString().trim() === '') {
            errors.push(`Trường ${field} không được để trống`);
        }
    });
    
    return errors;
}

// Parse JSON safely
window.safeJsonParse = function(jsonString, defaultValue = null) {
    try {
        return JSON.parse(jsonString);
    } catch (error) {
        console.error('❌ JSON parse error:', error);
        return defaultValue;
    }
}

// Stringify JSON safely
window.safeJsonStringify = function(obj, defaultValue = '{}') {
    try {
        return JSON.stringify(obj);
    } catch (error) {
        console.error('❌ JSON stringify error:', error);
        return defaultValue;
    }
}

// Generate unique ID
window.generateId = function() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
}

// Copy to clipboard
window.copyToClipboard = function(text) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            showAlert('Đã sao chép vào clipboard!', 'success');
        }).catch(() => {
            showAlert('Không thể sao chép vào clipboard', 'error');
        });
    } else {
        // Fallback cho older browsers
        const textArea = document.createElement('textarea');
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.select();
        try {
            document.execCommand('copy');
            showAlert('Đã sao chép vào clipboard!', 'success');
        } catch (err) {
            showAlert('Không thể sao chép vào clipboard', 'error');
        }
        document.body.removeChild(textArea);
    }
}
