// Execute the actual inline script with a minimal DOM; this does not claim browser/UI acceptance.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const assert = require('assert');
const folder = path.join(process.argv[2], 'Areas/Admin/Views/ProductVariants');
const fields = fs.readFileSync(path.join(folder, '_Fields.cshtml'), 'utf8');
for (const view of ['Create', 'Edit']) {
    assert(fs.readFileSync(path.join(folder, view + '.cshtml'), 'utf8').includes('enctype = "multipart/form-data"'));
}
const nodes = {};
for (const id of ['ImageUpload', 'RemoveImage', 'image-preview', 'image-preview-label']) {
    nodes[id] = { hidden: true, events: {}, addEventListener(name, callback) { this.events[name] = callback; }, removeAttribute(name) { delete this[name]; } };
}
const revoked = [];
const events = {};
const urlApi = { createObjectURL: () => 'blob:test', revokeObjectURL: url => revoked.push(url) };
vm.runInNewContext(fields.match(/<script>([\s\S]*?)<\/script>/)[1], {
    document: { getElementById: id => nodes[id] }, URL: urlApi,
    window: { URL: urlApi, addEventListener: (name, callback) => { events[name] = callback; } }
});
const input = nodes.ImageUpload;
const remove = nodes.RemoveImage;
const preview = nodes['image-preview'];
function change(file) { input.files = file ? [file] : []; input.events.change(); }
remove.checked = true;
change({ size: 100, type: 'image/png' });
assert(!preview.hidden && preview.src === 'blob:test' && !remove.checked);
change({ size: 2097153, type: 'image/png' });
assert(preview.hidden && revoked.length === 1);
change({ size: 1, type: 'text/html' }); assert(preview.hidden);
change(); assert(preview.hidden);
change({ size: 100, type: 'image/jpeg' });
preview.events.error(); assert(preview.hidden);
change({ size: 2097152, type: 'image/png' }); assert(!preview.hidden);
input.value = 'phone.png'; remove.checked = true; remove.events.change();
assert(input.value === '' && preview.hidden);
change({ size: 1, type: 'image/png' }); events.pagehide(); assert(preview.hidden);
console.log('PASS: multipart forms and preview script (selection, bounds, invalid type, clear, decode error, remove, URL cleanup); simulated DOM only.');
