let selectedCollectorIndex = null;
let collectors = []; // Lưu trữ dữ liệu collector

function renderCollectors(data) {
    collectors = data; // Lưu trữ dữ liệu
    const tbody = document.getElementById('collector-table-body');
    tbody.innerHTML = '';
    data.forEach((item, idx) => {
        const isRunning = item.status === 'running';
        const statusDot = `<span class="status-indicator status-${item.status}"></span>`;
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><input type="checkbox" class="row-checkbox" data-idx="${idx}"></td>
            <td>${item.name}</td>
            <td>${item.tag}</td>
            <td>${statusDot} ${isRunning ? 'Active' : 'Inactive'}</td>
            <td>
                <button class="btn-toggle" data-tag="${item.tag}" data-enable="${!isRunning}">
                    ${isRunning ? 'Tắt' : 'Bật'}
                </button>
            </td>
        `;
        
        // Gắn sự kiện cho checkbox
        const checkbox = tr.querySelector('.row-checkbox');
        checkbox.onclick = function(e) {
            e.stopPropagation();
            if (this.checked) {
                selectedCollectorIndex = idx;
                // Bỏ chọn các checkbox khác
                document.querySelectorAll('.row-checkbox').forEach(cb => {
                    if (cb !== this) cb.checked = false;
                });
                // Highlight dòng được chọn
                document.querySelectorAll('#collector-table-body tr').forEach(row => row.classList.remove('selected'));
                tr.classList.add('selected');
            } else {
                selectedCollectorIndex = null;
                tr.classList.remove('selected');
            }
        };
        
        // Gắn sự kiện cho nút bật/tắt
        const toggleBtn = tr.querySelector('.btn-toggle');
        toggleBtn.onclick = function(e) {
            e.stopPropagation();
            const tag = this.getAttribute('data-tag');
            const enable = this.getAttribute('data-enable') === 'true';
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ action: 'toggle', tag, enable });
            }
        };
        
        tbody.appendChild(tr);
    });

    // Gắn sự kiện cho checkbox "Chọn tất cả"
    const selectAllCheckbox = document.getElementById('selectAllCheckbox');
    selectAllCheckbox.onchange = function() {
        const checkboxes = document.querySelectorAll('.row-checkbox');
        checkboxes.forEach((checkbox, idx) => {
            checkbox.checked = this.checked;
            if (this.checked) {
                selectedCollectorIndex = idx;
                checkbox.closest('tr').classList.add('selected');
            } else {
                selectedCollectorIndex = null;
                checkbox.closest('tr').classList.remove('selected');
            }
        });
    };
}

// Hàm này sẽ được gọi từ C# qua WebView2 để cập nhật collector động
function updateCollectorsFromCSharp(jsonData) {
    try {
        const data = JSON.parse(jsonData);
        renderCollectors(data);
    } catch (e) {
        console.error('Dữ liệu collector không hợp lệ:', e);
    }
}

function showEditCollectorPopup(collector, idx) {
    // Tạo form popup sửa collector
    const popup = document.createElement('div');
    popup.style.position = 'fixed';
    popup.style.top = '0';
    popup.style.left = '0';
    popup.style.width = '100vw';
    popup.style.height = '100vh';
    popup.style.background = 'rgba(0,0,0,0.4)';
    popup.style.display = 'flex';
    popup.style.alignItems = 'center';
    popup.style.justifyContent = 'center';
    popup.style.zIndex = 9999;

    const form = document.createElement('form');
    form.style.background = '#222';
    form.style.padding = '24px 32px';
    form.style.borderRadius = '10px';
    form.style.minWidth = '350px';
    form.style.color = '#fff';
    form.onsubmit = function(e) { e.preventDefault(); };

    // Chọn loại input
    const labelType = document.createElement('label');
    labelType.textContent = 'Loại log source:';
    labelType.style.display = 'block';
    labelType.style.marginBottom = '8px';
    const selectType = document.createElement('select');
    selectType.style.width = '100%';
    selectType.style.marginBottom = '16px';
    for (const type in inputFieldMap) {
        const opt = document.createElement('option');
        opt.value = type;
        opt.textContent = type;
        if (type === collector.type) {
            opt.selected = true;
        }
        selectType.appendChild(opt);
    }
    form.appendChild(labelType);
    form.appendChild(selectType);

    // Container cho các trường động
    const fieldsDiv = document.createElement('div');
    form.appendChild(fieldsDiv);

    // Lưu lại select parser để cập nhật sau khi nhận danh sách
    let parserSelect = null;

    function renderFields(type, parserNames) {
        fieldsDiv.innerHTML = '';
        inputFieldMap[type].forEach(field => {
            const label = document.createElement('label');
            label.textContent = field + ':';
            label.style.display = 'block';
            label.style.marginTop = '8px';
            
            if (field === 'Parser' && parserNames && parserNames.length > 0) {
                // Nếu có danh sách parser, render select box
                parserSelect = document.createElement('select');
                parserSelect.name = field;
                parserSelect.style.width = '100%';
                parserSelect.style.marginBottom = '8px';
                parserNames.forEach(name => {
                    const opt = document.createElement('option');
                    opt.value = name;
                    opt.textContent = name;
                    if (name === collector[field]) {
                        opt.selected = true;
                    }
                    parserSelect.appendChild(opt);
                });
                fieldsDiv.appendChild(label);
                fieldsDiv.appendChild(parserSelect);
            } else {
                const input = document.createElement('input');
                input.type = 'text';
                input.name = field;
                input.value = collector[field] || '';
                input.style.width = '100%';
                input.style.marginBottom = '8px';
                fieldsDiv.appendChild(label);
                fieldsDiv.appendChild(input);
            }
        });
    }

    // Khi chọn loại input, nếu có trường Parser thì lấy danh sách parser từ C#
    function handleTypeChange() {
        const type = selectType.value;
        if (inputFieldMap[type].includes('Parser')) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ action: 'get_parser_names' });
                // Khi nhận được danh sách parser, sẽ gọi window.setParserNameOptions
                window.setParserNameOptions = function(parserNames) {
                    renderFields(type, parserNames);
                };
                return;
            }
        }
        renderFields(type);
    }
    
    selectType.onchange = handleTypeChange;
    // Lần đầu render với loại hiện tại
    handleTypeChange();

    const btnOk = document.createElement('button');
    btnOk.type = 'button';
    btnOk.textContent = 'Lưu';
    btnOk.style.marginRight = '16px';
    btnOk.onclick = function() {
        const type = selectType.value;
        const data = { type };
        let valid = true;
        inputFieldMap[type].forEach(field => {
            let val = '';
            if (field === 'Parser' && parserSelect) {
                val = parserSelect.value;
            } else {
                const inp = form.querySelector(`[name="${field}"]`);
                if (inp) val = inp.value.trim();
            }
            if (!val) valid = false;
            data[field] = val;
        });
        if (!valid) {
            alert('Vui lòng nhập đầy đủ các trường!');
            return;
        }
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'edit_collector', data, idx });
        }
        document.body.removeChild(popup);
    };
    
    const btnCancel = document.createElement('button');
    btnCancel.type = 'button';
    btnCancel.textContent = 'Hủy';
    btnCancel.onclick = function() {
        document.body.removeChild(popup);
    };
    
    form.appendChild(btnOk);
    form.appendChild(btnCancel);
    popup.appendChild(form);
    document.body.appendChild(popup);
}

// Khung hàm cho các nút chức năng

// Mapping loại input và các trường cần nhập
const inputFieldMap = {
    winlog: ["Tag", "Channels", "Interval_Sec", "DB"],
    syslog: ["Tag", "Listen", "Port", "Mode", "Parser"],
    http: ["Tag", "Host", "Port"],
    tail: ["Path", "Tag"],
    dummy: ["Tag", "Dummy", "Samples"],
    // Thêm các loại input khác nếu cần
};

function showAddCollectorPopup() {
    // Tạo form popup
    const popup = document.createElement('div');
    popup.style.position = 'fixed';
    popup.style.top = '0';
    popup.style.left = '0';
    popup.style.width = '100vw';
    popup.style.height = '100vh';
    popup.style.background = 'rgba(0,0,0,0.4)';
    popup.style.display = 'flex';
    popup.style.alignItems = 'center';
    popup.style.justifyContent = 'center';
    popup.style.zIndex = 9999;

    const form = document.createElement('form');
    form.style.background = '#222';
    form.style.padding = '24px 32px';
    form.style.borderRadius = '10px';
    form.style.minWidth = '350px';
    form.style.color = '#fff';
    form.onsubmit = function(e) { e.preventDefault(); };

    // Chọn loại input
    const labelType = document.createElement('label');
    labelType.textContent = 'Loại log source:';
    labelType.style.display = 'block';
    labelType.style.marginBottom = '8px';
    const selectType = document.createElement('select');
    selectType.style.width = '100%';
    selectType.style.marginBottom = '16px';
    for (const type in inputFieldMap) {
        const opt = document.createElement('option');
        opt.value = type;
        opt.textContent = type;
        selectType.appendChild(opt);
    }
    form.appendChild(labelType);
    form.appendChild(selectType);

    // Container cho các trường động
    const fieldsDiv = document.createElement('div');
    form.appendChild(fieldsDiv);

    // Lưu lại select parser để cập nhật sau khi nhận danh sách
    let parserSelect = null;

    function renderFields(type, parserNames) {
        fieldsDiv.innerHTML = '';
        inputFieldMap[type].forEach(field => {
            const label = document.createElement('label');
            label.textContent = field + ':';
            label.style.display = 'block';
            label.style.marginTop = '8px';
            if (field === 'Parser' && parserNames && parserNames.length > 0) {
                // Nếu có danh sách parser, render select box
                parserSelect = document.createElement('select');
                parserSelect.name = field;
                parserSelect.style.width = '100%';
                parserSelect.style.marginBottom = '8px';
                parserNames.forEach(name => {
                    const opt = document.createElement('option');
                    opt.value = name;
                    opt.textContent = name;
                    parserSelect.appendChild(opt);
                });
                fieldsDiv.appendChild(label);
                fieldsDiv.appendChild(parserSelect);
            } else {
                const input = document.createElement('input');
                input.type = 'text';
                input.name = field;
                input.style.width = '100%';
                input.style.marginBottom = '8px';
                fieldsDiv.appendChild(label);
                fieldsDiv.appendChild(input);
            }
        });
    }

    // Khi chọn loại input, nếu có trường Parser thì lấy danh sách parser từ C#
    function handleTypeChange() {
        const type = selectType.value;
        if (inputFieldMap[type].includes('Parser')) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ action: 'get_parser_names' });
                // Khi nhận được danh sách parser, sẽ gọi window.setParserNameOptions
                window.setParserNameOptions = function(parserNames) {
                    renderFields(type, parserNames);
                };
                return;
            }
        }
        renderFields(type);
    }
    selectType.onchange = handleTypeChange;
    // Lần đầu render
    handleTypeChange();

    // Nút OK và Cancel giữ nguyên
    const btnOk = document.createElement('button');
    btnOk.type = 'button';
    btnOk.textContent = 'Thêm';
    btnOk.style.marginRight = '16px';
    btnOk.onclick = function() {
        const type = selectType.value;
        const data = { type };
        let valid = true;
        inputFieldMap[type].forEach(field => {
            let val = '';
            if (field === 'Parser' && parserSelect) {
                val = parserSelect.value;
            } else {
                const inp = form.querySelector(`[name="${field}"]`);
                if (inp) val = inp.value.trim();
            }
            if (!val) valid = false;
            data[field] = val;
        });
        if (!valid) {
            alert('Vui lòng nhập đầy đủ các trường!');
            return;
        }
        // Gửi message sang C#
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'add_collector', data });
        }
        document.body.removeChild(popup);
    };
    const btnCancel = document.createElement('button');
    btnCancel.type = 'button';
    btnCancel.textContent = 'Hủy';
    btnCancel.onclick = function() {
        document.body.removeChild(popup);
    };
    form.appendChild(btnOk);
    form.appendChild(btnCancel);

    popup.appendChild(form);
    document.body.appendChild(popup);
}

document.getElementById('btnAdd').onclick = function() {
    showAddCollectorPopup();
};

document.getElementById('btnEdit').onclick = function() {
    if (selectedCollectorIndex === null) {
        alert('Vui lòng chọn một log source để sửa!');
        return;
    }
    showEditCollectorPopup(collectors[selectedCollectorIndex], selectedCollectorIndex);
};

document.getElementById('btnDelete').onclick = function() {
    if (selectedCollectorIndex === null) {
        alert('Vui lòng chọn một log source để xóa!');
        return;
    }
    if (confirm('Bạn có chắc muốn xóa log source này?')) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'delete_collector', idx: selectedCollectorIndex });
        }
    }
};

document.getElementById('btnShowEvents').onclick = function() {
    if (selectedCollectorIndex === null) {
        alert('Vui lòng chọn một log source để xem sự kiện!');
        return;
    }
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'show_events', idx: selectedCollectorIndex });
    }
};

document.getElementById('btnEnableLog').onclick = function() {
    if (selectedCollectorIndex === null) {
        alert('Vui lòng chọn một log source để bật ghi log!');
        return;
    }
    const tag = collectors[selectedCollectorIndex].tag;
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'toggle', tag, enable: true });
    }
};

document.getElementById('btnDisableLog').onclick = function() {
    if (selectedCollectorIndex === null) {
        alert('Vui lòng chọn một log source để tắt ghi log!');
        return;
    }
    const tag = collectors[selectedCollectorIndex].tag;
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'toggle', tag, enable: false });
    }
};

document.getElementById('searchInput').oninput = function() {
    // TODO: Lọc bảng collector theo tên
};

document.querySelectorAll('.sidebar-item').forEach(item => {
    item.onclick = function() {
        document.querySelectorAll('.sidebar-item').forEach(i => i.classList.remove('active'));
        this.classList.add('active');
        const tab = this.getAttribute('data-tab');
        document.querySelectorAll('.tab-section').forEach(sec => sec.style.display = 'none');
        document.getElementById('tab-' + tab).style.display = '';
    };
});

// Khi click vào tab Parser, gửi yêu cầu lấy danh sách parser
// const parserTab = document.querySelector('.sidebar-item[data-tab="parser"]');
// if (parserTab) {
//     parserTab.addEventListener('click', function() {
//         if (window.chrome && window.chrome.webview) {
//             window.chrome.webview.postMessage({ action: 'get_parsers' });
//         }
//     });
// }

// Hàm nhận danh sách parser từ C# và render bảng
function updateParsersFromCSharp(jsonData) {
    let data = [];
    try {
        data = JSON.parse(jsonData);
    } catch (e) {
        document.getElementById('parser-content').innerHTML = '<div style="color:red">Lỗi dữ liệu parser!</div>';
        return;
    }
    let html = '<button id="btnAddParser">Thêm mới Parser</button>';
    html += '<table class="parser-table"><thead><tr><th>Name</th><th>Format</th><th>Regex</th><th>Time_Key</th><th>Time_Format</th><th>Time_Keep</th><th>Action</th></tr></thead><tbody>';
    data.forEach((item, idx) => {
        html += `<tr><td>${item.Name||''}</td><td>${item.Format||''}</td><td>${item.Regex||''}</td><td>${item.Time_Key||''}</td><td>${item.Time_Format||''}</td><td>${item.Time_Keep||''}</td><td><button class="btnEditParser" data-idx="${idx}">Sửa</button> <button class="btnDeleteParser" data-idx="${idx}">Xóa</button></td></tr>`;
    });
    html += '</tbody></table>';
    document.getElementById('parser-content').innerHTML = html;
    // TODO: Gắn sự kiện cho nút Thêm/Sửa/Xóa
} 