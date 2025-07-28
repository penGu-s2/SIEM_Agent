// Đảm bảo chỉ khai báo parserTab một lần duy nhất
const parserTab = document.querySelector('.sidebar-item[data-tab="parser"]');
if (parserTab) {
    parserTab.addEventListener('click', function() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'get_parsers' });
        }
    });
}

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
    attachParserEvents(data);
}

function attachParserEvents(data) {
    document.getElementById('btnAddParser').onclick = function() {
        window.showParserPopup('add');
    };
    document.querySelectorAll('.btnEditParser').forEach(btn => {
        btn.onclick = function() {
            const idx = parseInt(this.getAttribute('data-idx'));
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ action: 'get_parser_by_idx', idx });
            }
        };
    });
    document.querySelectorAll('.btnDeleteParser').forEach(btn => {
        btn.onclick = function() {
            const idx = parseInt(this.getAttribute('data-idx'));
            if (confirm('Bạn có chắc muốn xóa parser này?')) {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage({ action: 'delete_parser', idx });
                    showBoard('Xóa parser thành công!', 'success');
                }
            }
        };
    });
}

// Đảm bảo không có popup cũ bị treo khi mở mới
function showParserPopup(mode, parser = {}, idx = null) {
    const oldPopup = document.getElementById('parser-popup');
    if (oldPopup) document.body.removeChild(oldPopup);
    const popup = document.createElement('div');
    popup.id = 'parser-popup';
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
    form.style.background = '#fff';
    form.style.padding = '28px 36px';
    form.style.borderRadius = '14px';
    form.style.minWidth = '370px';
    form.style.color = '#222';
    form.style.boxShadow = '0 8px 32px rgba(0,0,0,0.18)';
    form.onsubmit = function(e) { e.preventDefault(); };

    const title = document.createElement('h2');
    title.textContent = (mode==='add') ? 'Thêm mới Parser' : 'Chỉnh sửa Parser';
    title.style.marginTop = '0';
    title.style.marginBottom = '18px';
    title.style.color = '#222';
    form.appendChild(title);

    const fields = ['Name','Format','Regex','Time_Key','Time_Format','Time_Keep'];
    fields.forEach(field => {
        const label = document.createElement('label');
        label.textContent = field + ':';
        label.style.display = 'block';
        label.style.marginTop = '10px';
        label.style.fontWeight = 'bold';
        label.style.color = '#222';
        const input = document.createElement('input');
        input.type = 'text';
        input.name = field;
        input.value = parser[field] || '';
        input.style.width = '100%';
        input.style.marginBottom = '8px';
        input.style.padding = '7px 10px';
        input.style.border = '1px solid #bbb';
        input.style.borderRadius = '5px';
        input.style.fontSize = '15px';
        input.style.background = '#fff';
        input.style.color = '#222';
        form.appendChild(label);
        form.appendChild(input);
    });
    const errorDiv = document.createElement('div');
    errorDiv.style.color = '#d32f2f';
    errorDiv.style.margin = '8px 0 0 0';
    errorDiv.style.fontSize = '14px';
    form.appendChild(errorDiv);

    const btnOk = document.createElement('button');
    btnOk.type = 'button';
    btnOk.textContent = (mode==='add') ? 'Thêm' : 'Lưu';
    btnOk.style.marginRight = '16px';
    btnOk.style.background = '#1976d2';
    btnOk.style.color = '#fff';
    btnOk.style.border = 'none';
    btnOk.style.borderRadius = '5px';
    btnOk.style.padding = '8px 22px';
    btnOk.style.fontSize = '15px';
    btnOk.style.cursor = 'pointer';
    btnOk.style.boxShadow = '0 2px 8px rgba(25,118,210,0.08)';
    btnOk.onmouseover = function() { btnOk.style.background = '#1565c0'; };
    btnOk.onmouseout = function() { btnOk.style.background = '#1976d2'; };
    btnOk.onclick = function() {
        const data = {};
        let valid = true;
        fields.forEach(field => {
            const val = form.querySelector(`[name="${field}"]`).value.trim();
            if (!val) valid = false;
            data[field] = val;
        });
        if (!valid) {
            errorDiv.textContent = 'Vui lòng nhập đầy đủ các trường!';
            return;
        }
        errorDiv.textContent = '';
        if (window.chrome && window.chrome.webview) {
            if (mode==='add') {
                window.chrome.webview.postMessage({ action: 'add_parser', data });
                showBoard('Thêm parser thành công!');
            } else {
                window.chrome.webview.postMessage({ action: 'edit_parser', data, idx });
                showBoard('Cập nhật parser thành công!');
            }
        }
        document.body.removeChild(popup);
    };
    const btnCancel = document.createElement('button');
    btnCancel.type = 'button';
    btnCancel.textContent = 'Hủy';
    btnCancel.style.background = '#fff';
    btnCancel.style.color = '#222';
    btnCancel.style.border = '1px solid #bbb';
    btnCancel.style.borderRadius = '5px';
    btnCancel.style.padding = '8px 22px';
    btnCancel.style.fontSize = '15px';
    btnCancel.style.cursor = 'pointer';
    btnCancel.onmouseover = function() { btnCancel.style.background = '#f5f5f5'; };
    btnCancel.onmouseout = function() { btnCancel.style.background = '#fff'; };
    btnCancel.onclick = function() {
        document.body.removeChild(popup);
    };
    form.appendChild(btnOk);
    form.appendChild(btnCancel);
    form.style.marginBottom = '0';
    popup.appendChild(form);
    document.body.appendChild(popup);
}

function showBoard(message, type = 'success') {
    const board = document.createElement('div');
    board.textContent = message;
    board.style.position = 'fixed';
    board.style.top = '24px';
    board.style.right = '24px';
    board.style.background = (type === 'success') ? '#4caf50' : '#f44336';
    board.style.color = '#fff';
    board.style.padding = '12px 24px';
    board.style.borderRadius = '8px';
    board.style.fontSize = '16px';
    board.style.zIndex = 99999;
    document.body.appendChild(board);
    setTimeout(() => { document.body.removeChild(board); }, 2000);
}

// Đảm bảo các hàm này global để C# gọi được
window.updateParsersFromCSharp = updateParsersFromCSharp;
window.openEditParserPopup = function(parser, idx) {
    window.showParserPopup('edit', parser, idx);
};
window.showParserPopup = showParserPopup; 