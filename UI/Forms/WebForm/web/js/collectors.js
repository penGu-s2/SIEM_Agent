// Quản lý collectors cho dashboard
// Sử dụng global variables thay vì ES6 modules để tương thích với script loading

// Global variables - sẽ được set sau khi các script khác load
let STATUS_LABELS = window.STATUS_LABELS || {};
// Không khai báo lại sendMessageToCSharp vì đã có từ utils.js
let showAddCollectorPopup = window.showAddCollectorPopup || function() {};
let showEditCollectorPopup = window.showEditCollectorPopup || function() {};

// Expose functions globally
window.CollectorManager = class CollectorManager {
    constructor() {
        this.selectedCollectorIndex = null;
        this.collectors = []; // Lưu trữ dữ liệu collector
        this.initEventListeners();
    }

    initEventListeners() {
        // Gắn sự kiện cho các nút chức năng
        document.addEventListener('DOMContentLoaded', () => {
            const btnAdd = document.getElementById('btnAdd');
            const btnEdit = document.getElementById('btnEdit');
            const btnDelete = document.getElementById('btnDelete');
            const btnShowEvents = document.getElementById('btnShowEvents');
            const btnEnableLog = document.getElementById('btnEnableLog');
            const btnDisableLog = document.getElementById('btnDisableLog');
            const searchInput = document.getElementById('searchInput');

            if (btnAdd) btnAdd.onclick = () => this.showAddCollectorPopup();
            if (btnEdit) btnEdit.onclick = () => this.showEditCollectorPopup();
            if (btnDelete) btnDelete.onclick = () => this.deleteCollector();
            if (btnShowEvents) btnShowEvents.onclick = () => this.showEvents();
            if (btnEnableLog) btnEnableLog.onclick = () => this.enableLog();
            if (btnDisableLog) btnDisableLog.onclick = () => this.disableLog();
            if (searchInput) searchInput.oninput = () => this.filterCollectors();
        });
    }

    renderCollectors(data) {
        console.log('🔄 Rendering collectors:', data);
        console.log('📊 Data type:', typeof data);
        console.log('📊 Data length:', Array.isArray(data) ? data.length : 'Not array');
        
        if (Array.isArray(data) && data.length > 0) {
            console.log('📊 First collector item:', data[0]);
            console.log('📊 First collector keys:', Object.keys(data[0]));
            console.log('📊 All collectors:');
            data.forEach((item, idx) => {
                console.log(`  [${idx}] ${JSON.stringify(item, null, 2)}`);
            });
        }
        
        this.collectors = data; // Lưu trữ dữ liệu
        const tbody = document.getElementById('collector-table-body');
        if (!tbody) {
            console.error('❌ Không tìm thấy collector-table-body');
            return;
        }

        tbody.innerHTML = '';
        
        data.forEach((item, idx) => {
            const isRunning = item.status === 'running';
            const statusDot = `<span class="status-indicator status-${item.status}"></span>`;
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td><input type="checkbox" class="row-checkbox" data-idx="${idx}"></td>
                <td>${item.name || item.Name || 'Unknown'}</td>
                <td>${item.tag || item.Tag || 'Unknown'}</td>
                <td>${statusDot} ${isRunning ? 'Active' : 'Inactive'}</td>
                <td>
                    <button class="btn-toggle" data-tag="${item.tag || item.Tag}" data-enable="${!isRunning}">
                        ${isRunning ? 'Tắt' : 'Bật'}
                    </button>
                </td>
            `;
            
            // Gắn sự kiện cho checkbox
            const checkbox = tr.querySelector('.row-checkbox');
            checkbox.onclick = (e) => this.handleCheckboxClick(e, idx, tr);
            
            // Gắn sự kiện cho nút bật/tắt
            const toggleBtn = tr.querySelector('.btn-toggle');
            toggleBtn.onclick = (e) => this.handleToggleClick(e, item);
            
            tbody.appendChild(tr);
        });

        // Gắn sự kiện cho checkbox "Chọn tất cả"
        this.initSelectAllCheckbox();
        
        console.log('✅ Collectors rendered successfully');
        console.log('📊 Table rows created:', data.length);
    }

    handleCheckboxClick(e, idx, tr) {
        e.stopPropagation();
        const checkbox = e.target;
        
        if (checkbox.checked) {
            this.selectedCollectorIndex = idx;
            // Bỏ chọn các checkbox khác
            document.querySelectorAll('.row-checkbox').forEach(cb => {
                if (cb !== checkbox) cb.checked = false;
            });
            // Highlight dòng được chọn
            document.querySelectorAll('#collector-table-body tr').forEach(row => 
                row.classList.remove('selected')
            );
            tr.classList.add('selected');
        } else {
            this.selectedCollectorIndex = null;
            tr.classList.remove('selected');
        }
    }

    handleToggleClick(e, item) {
        e.stopPropagation();
        const tag = item.tag || item.Tag;
        const enable = e.target.getAttribute('data-enable') === 'true';
        
        console.log('🔄 Toggling collector:', tag, 'enable:', enable);
        sendMessageToCSharp('toggle', { tag, enable });
    }

    initSelectAllCheckbox() {
        const selectAllCheckbox = document.getElementById('selectAllCheckbox');
        if (selectAllCheckbox) {
            selectAllCheckbox.onchange = (e) => {
                const checkboxes = document.querySelectorAll('.row-checkbox');
                checkboxes.forEach((checkbox, idx) => {
                    checkbox.checked = e.target.checked;
                    if (e.target.checked) {
                        this.selectedCollectorIndex = idx;
                        checkbox.closest('tr').classList.add('selected');
                    } else {
                        this.selectedCollectorIndex = null;
                        checkbox.closest('tr').classList.remove('selected');
                    }
                });
            };
        }
    }

    showAddCollectorPopup() {
        console.log('🔄 Opening add collector popup');
        showAddCollectorPopup();
    }

    showEditCollectorPopup() {
        if (this.selectedCollectorIndex === null) {
            alert('Vui lòng chọn một log source để sửa!');
            return;
        }
        console.log('🔄 Opening edit collector popup for index:', this.selectedCollectorIndex);
        showEditCollectorPopup(this.collectors[this.selectedCollectorIndex], this.selectedCollectorIndex);
    }

    deleteCollector() {
        if (this.selectedCollectorIndex === null) {
            alert('Vui lòng chọn một log source để xóa!');
            return;
        }
        if (confirm('Bạn có chắc muốn xóa log source này?')) {
            console.log('🔄 Deleting collector at index:', this.selectedCollectorIndex);
            sendMessageToCSharp('delete_collector', { idx: this.selectedCollectorIndex });
        }
    }

    showEvents() {
        if (this.selectedCollectorIndex === null) {
            alert('Vui lòng chọn một log source để xem sự kiện!');
            return;
        }
        console.log('🔄 Showing events for collector at index:', this.selectedCollectorIndex);
        sendMessageToCSharp('show_events', { idx: this.selectedCollectorIndex });
    }

    enableLog() {
        if (this.selectedCollectorIndex === null) {
            alert('Vui lòng chọn một log source để bật ghi log!');
            return;
        }
        const tag = this.collectors[this.selectedCollectorIndex].tag || this.collectors[this.selectedCollectorIndex].Tag;
        console.log('🔄 Enabling log for tag:', tag);
        sendMessageToCSharp('toggle', { tag, enable: true });
    }

    disableLog() {
        if (this.selectedCollectorIndex === null) {
            alert('Vui lòng chọn một log source để tắt ghi log!');
            return;
        }
        const tag = this.collectors[this.selectedCollectorIndex].tag || this.collectors[this.selectedCollectorIndex].Tag;
        console.log('🔄 Disabling log for tag:', tag);
        sendMessageToCSharp('toggle', { tag, enable: false });
    }

    filterCollectors() {
        const searchInput = document.getElementById('searchInput');
        if (!searchInput) return;
        
        const searchTerm = searchInput.value.toLowerCase();
        const rows = document.querySelectorAll('#collector-table-body tr');
        
        rows.forEach(row => {
            const nameCell = row.querySelector('td:nth-child(2)');
            const tagCell = row.querySelector('td:nth-child(3)');
            
            if (nameCell && tagCell) {
                const name = nameCell.textContent.toLowerCase();
                const tag = tagCell.textContent.toLowerCase();
                
                if (name.includes(searchTerm) || tag.includes(searchTerm)) {
                    row.style.display = '';
                } else {
                    row.style.display = 'none';
                }
            }
        });
    }

    loadParsers() {
        console.log('🔄 Loading parsers...');
        sendMessageToCSharp('get_parsers', {});
    }
}

// Tạo instance và expose globally
window.collectorManager = new CollectorManager();

// Hàm này sẽ được gọi từ C# qua WebView2 để cập nhật collector động
window.updateCollectorsFromCSharp = function(jsonData) {
    console.log('📊 updateCollectorsFromCSharp called with:', jsonData);
    console.log('📊 Type of jsonData:', typeof jsonData);
    console.log('📊 jsonData length:', jsonData ? jsonData.length : 'null');
    
    try {
        const data = typeof jsonData === 'string' ? JSON.parse(jsonData) : jsonData;
        console.log('📊 Parsed data:', data);
        console.log('📊 Data type:', Array.isArray(data) ? 'Array' : typeof data);
        console.log('📊 Data length:', Array.isArray(data) ? data.length : 'Not array');
        
        if (Array.isArray(data) && data.length > 0) {
            console.log('📊 First collector:', data[0]);
        }
        
        window.collectorManager.renderCollectors(data);
        console.log('✅ Collectors updated successfully');
    } catch (e) {
        console.error('❌ Dữ liệu collector không hợp lệ:', e);
        console.error('❌ Error details:', e.message);
        console.error('❌ Stack trace:', e.stack);
    }
};

// Expose function cho C# gọi trực tiếp
window.CollectorManager.prototype.updateCollectorsFromCSharp = window.updateCollectorsFromCSharp;
