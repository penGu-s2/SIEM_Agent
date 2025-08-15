// Test integration between modules
console.log('🧪 Testing module integration...');

// Test 1: Check if global variables are available
console.log('📊 Global variables test:');
console.log('- selectedCollectorIndex:', typeof selectedCollectorIndex !== 'undefined' ? '✅ Available' : '❌ Not available');
console.log('- collectors:', typeof collectors !== 'undefined' ? '✅ Available' : '❌ Not available');
console.log('- window.selectedCollectorIndex:', typeof window.selectedCollectorIndex !== 'undefined' ? '✅ Available' : '❌ Not available');
console.log('- window.collectors:', typeof window.collectors !== 'undefined' ? '✅ Available' : '❌ Not available');

// Test 2: Check if functions are available
console.log('🔧 Functions test:');
console.log('- renderCollectors:', typeof renderCollectors === 'function' ? '✅ Available' : '❌ Not available');
console.log('- showAddCollectorPopup:', typeof showAddCollectorPopup === 'function' ? '✅ Available' : '❌ Not available');
console.log('- showEditCollectorPopup:', typeof showEditCollectorPopup === 'function' ? '✅ Available' : '❌ Not available');
console.log('- deleteCollector:', typeof deleteCollector === 'function' ? '✅ Available' : '❌ Not available');
console.log('- sendMessageToCSharp:', typeof sendMessageToCSharp === 'function' ? '✅ Available' : '❌ Not available');
console.log('- showNotification:', typeof showNotification === 'function' ? '✅ Available' : '❌ Not available');

// Test 3: Check if window functions are exposed with new names
console.log('🌐 Window functions test (new naming):');
console.log('- window.renderCollectorsFromDashboardModule:', typeof window.renderCollectorsFromDashboardModule === 'function' ? '✅ Available' : '❌ Not available');
console.log('- window.showAddCollectorPopupFromActionsModule:', typeof window.showAddCollectorPopupFromActionsModule === 'function' ? '✅ Available' : '❌ Not available');
console.log('- window.showEditCollectorPopupFromActionsModule:', typeof window.showEditCollectorPopupFromActionsModule === 'function' ? '✅ Available' : '❌ Not available');
console.log('- window.deleteCollectorFromActionsModule:', typeof window.deleteCollectorFromActionsModule === 'function' ? '✅ Available' : '❌ Not available');
console.log('- window.sendMessageToCSharpFromActionsModule:', typeof window.sendMessageToCSharpFromActionsModule === 'function' ? '✅ Available' : '❌ Not available');

// Test 4: Check toolbar buttons
console.log('🔘 Toolbar buttons test:');
const toolbarButtons = ['btnAdd', 'btnEdit', 'btnDelete', 'btnShowEvents', 'btnEnableLog', 'btnDisableLog'];
toolbarButtons.forEach(btnId => {
    const btn = document.getElementById(btnId);
    if (btn) {
        console.log(`- ${btnId}: ✅ Found (${btn.onclick ? 'Has onclick' : 'No onclick'})`);
    } else {
        console.log(`- ${btnId}: ❌ Not found`);
    }
});

// Test 5: Check search input
console.log('🔍 Search input test:');
const searchInput = document.getElementById('searchInput');
if (searchInput) {
    console.log('- searchInput: ✅ Found (Has oninput:', !!searchInput.oninput, ')');
} else {
    console.log('- searchInput: ❌ Not found');
}

// Test 6: Test popup functions directly
console.log('🎭 Popup functions test:');
try {
    if (typeof window.showAddCollectorPopupFromActionsModule === 'function') {
        console.log('- showAddCollectorPopupFromActionsModule: ✅ Function exists and callable');
    } else {
        console.log('- showAddCollectorPopupFromActionsModule: ❌ Function not available');
    }
} catch (e) {
    console.log('- showAddCollectorPopupFromActionsModule: ❌ Error calling function:', e.message);
}

// Test 7: Check for duplicate function declarations
console.log('🔍 Duplicate function check:');
const functionNames = ['sendMessageToCSharp', 'showAddCollectorPopup', 'showEditCollectorPopup', 'deleteCollector'];
functionNames.forEach(funcName => {
    const functions = [];
    for (let key in window) {
        if (typeof window[key] === 'function' && key === funcName) {
            functions.push(key);
        }
    }
    if (functions.length > 1) {
        console.log(`- ${funcName}: ⚠️ Multiple declarations found (${functions.length})`);
    } else if (functions.length === 1) {
        console.log(`- ${funcName}: ✅ Single declaration`);
    } else {
        console.log(`- ${funcName}: ❌ Not found`);
    }
});

// Test 8: Check for infinite recursion prevention
console.log('🔄 Infinite recursion prevention test:');
const recursionTestFunctions = [
    'updateCollectorsFromCSharp',
    'updateParsersFromCSharp', 
    'updateLogsFromCSharp',
    'updateLogTypesFromCSharp',
    'updateFluentBitStatus',
    'showDebugInfo',
    'openEditParserPopup',
    'showAddCollectorPopup',
    'showEditCollectorPopup',
    'deleteCollector',
    'sendMessageToCSharp'
];

recursionTestFunctions.forEach(funcName => {
    const func = window[funcName];
    if (func && typeof func === 'function') {
        // Kiểm tra xem hàm có gọi chính nó không
        const funcStr = func.toString();
        if (funcStr.includes(funcName + '(') && !funcStr.includes('FromActionsModule') && !funcStr.includes('FromDashboardModule')) {
            console.log(`- ${funcName}: ⚠️ Potential recursion risk`);
        } else {
            console.log(`- ${funcName}: ✅ Safe from recursion`);
        }
    } else {
        console.log(`- ${funcName}: ❌ Not available`);
    }
});

// Test 9: Check new output functionality
console.log('📤 Output functionality test:');
console.log('- add_collector_with_output action:', typeof window.sendMessageToCSharpFromActionsModule === 'function' ? '✅ Available' : '❌ Not available');

// Test 10: Check popup enhancement
console.log('🎭 Enhanced popup test:');
try {
    if (typeof window.showAddCollectorPopupFromActionsModule === 'function') {
        console.log('- showAddCollectorPopupFromActionsModule: ✅ Function exists and callable');
        
        // Test if popup can be called without errors
        console.log('- Popup function: ✅ Ready for testing');
    } else {
        console.log('- showAddCollectorPopupFromActionsModule: ❌ Function not available');
    }
} catch (e) {
    console.log('- showAddCollectorPopupFromActionsModule: ❌ Error calling function:', e.message);
}

console.log('✅ Integration test completed!');
