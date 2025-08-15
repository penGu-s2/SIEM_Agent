// Popup forms cho dashboard
// Sử dụng global variables thay vì ES6 modules để tương thích với script loading

// Global variables - sẽ được set sau khi các script khác load
let INPUT_FIELD_MAP = window.INPUT_FIELD_MAP || {};
let createElement = window.createElement || function() {};
let sendMessageToCSharp = window.sendMessageToCSharp || function() {};

// Expose functions globally
window.PopupManager = class PopupManager {
    constructor() {
        this.currentPopup = null;
    }

    closeCurrentPopup() {
        if (this.currentPopup && this.currentPopup.parentNode) {
            this.currentPopup.parentNode.removeChild(this.currentPopup);
            this.currentPopup = null;
        }
    }
}

// Tạo instance và expose globally
window.popupManager = new PopupManager();

window.showAddCollectorPopup = function() {
    console.log('🔄 Creating add collector popup');
    
    // Tạo form popup
    const popup = document.createElement('div');
    popup.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background: rgba(0,0,0,0.4);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
    `;

    const form = document.createElement('form');
    form.style.cssText = `
        background: #222;
        padding: 24px 32px;
        border-radius: 10px;
        min-width: 350px;
        color: #fff;
    `;
    form.onsubmit = function(e) { e.preventDefault(); };

    // Chọn loại input
    const labelType = document.createElement('label');
    labelType.textContent = 'Loại log source:';
    labelType.style.cssText = 'display: block; margin-bottom: 8px;';
    
    const selectType = document.createElement('select');
    selectType.style.cssText = 'width: 100%; margin-bottom: 16px;';
    
    for (const type in INPUT_FIELD_MAP) {
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
        INPUT_FIELD_MAP[type].forEach(field => {
            const label = document.createElement('label');
            label.textContent = field + ':';
            label.style.cssText = 'display: block; margin-top: 8px;';
            
            if (field === 'Parser' && parserNames && parserNames.length > 0) {
                // Nếu có danh sách parser, render select box
                parserSelect = document.createElement('select');
                parserSelect.name = field;
                parserSelect.style.cssText = 'width: 100%; margin-bottom: 8px;';
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
                input.style.cssText = 'width: 100%; margin-bottom: 8px;';
                fieldsDiv.appendChild(label);
                fieldsDiv.appendChild(input);
            }
        });
    }

    // Khi chọn loại input, nếu có trường Parser thì lấy danh sách parser từ C#
    function handleTypeChange() {
        const type = selectType.value;
        if (INPUT_FIELD_MAP[type].includes('Parser')) {
            console.log('🔄 Requesting parser names for type:', type);
            sendMessageToCSharp('get_parser_names', {});
            // Khi nhận được danh sách parser, sẽ gọi window.setParserNameOptions
            window.setParserNameOptions = function(parserNames) {
                console.log('📊 Received parser names:', parserNames);
                renderFields(type, parserNames);
            };
            return;
        }
        renderFields(type);
    }
    
    selectType.onchange = handleTypeChange;
    // Lần đầu render
    handleTypeChange();

    // Nút OK và Cancel
    const btnOk = document.createElement('button');
    btnOk.type = 'button';
    btnOk.textContent = 'Thêm';
    btnOk.style.marginRight = '16px';
    btnOk.onclick = function() {
        const type = selectType.value;
        const data = { type };
        let valid = true;
        
        INPUT_FIELD_MAP[type].forEach(field => {
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
        
        console.log('🔄 Adding collector with data:', data);
        sendMessageToCSharp('add_collector', { data });
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
    
    console.log('✅ Add collector popup created');
}

window.showEditCollectorPopup = function(collector, idx) {
    console.log('🔄 Creating edit collector popup for:', collector, 'index:', idx);
    
    // Tạo form popup sửa collector
    const popup = document.createElement('div');
    popup.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background: rgba(0,0,0,0.4);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
    `;

    const form = document.createElement('form');
    form.style.cssText = `
        background: #222;
        padding: 24px 32px;
        border-radius: 10px;
        min-width: 350px;
        color: #fff;
    `;
    form.onsubmit = function(e) { e.preventDefault(); };

    // Chọn loại input
    const labelType = document.createElement('label');
    labelType.textContent = 'Loại log source:';
    labelType.style.cssText = 'display: block; margin-bottom: 8px;';
    
    const selectType = document.createElement('select');
    selectType.style.cssText = 'width: 100%; margin-bottom: 16px;';
    
    for (const type in INPUT_FIELD_MAP) {
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
        INPUT_FIELD_MAP[type].forEach(field => {
            const label = document.createElement('label');
            label.textContent = field + ':';
            label.style.cssText = 'display: block; margin-top: 8px;';
            
            if (field === 'Parser' && parserNames && parserNames.length > 0) {
                // Nếu có danh sách parser, render select box
                parserSelect = document.createElement('select');
                parserSelect.name = field;
                parserSelect.style.cssText = 'width: 100%; margin-bottom: 8px;';
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
                input.style.cssText = 'width: 100%; margin-bottom: 8px;';
                fieldsDiv.appendChild(label);
                fieldsDiv.appendChild(input);
            }
        });
    }

    // Khi chọn loại input, nếu có trường Parser thì lấy danh sách parser từ C#
    function handleTypeChange() {
        const type = selectType.value;
        if (INPUT_FIELD_MAP[type].includes('Parser')) {
            console.log('🔄 Requesting parser names for type:', type);
            sendMessageToCSharp('get_parser_names', {});
            // Khi nhận được danh sách parser, sẽ gọi window.setParserNameOptions
            window.setParserNameOptions = function(parserNames) {
                console.log('📊 Received parser names:', parserNames);
                renderFields(type, parserNames);
            };
            return;
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
        
        INPUT_FIELD_MAP[type].forEach(field => {
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
        
        console.log('🔄 Editing collector at index:', idx, 'with data:', data);
        sendMessageToCSharp('edit_collector', { data, idx });
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
    
    console.log('✅ Edit collector popup created');
}
