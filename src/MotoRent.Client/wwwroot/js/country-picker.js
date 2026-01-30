const instances = {};

export function initCountryPicker(elementId, dotNetRef) {
    const el = document.getElementById(elementId);
    if (!el) return;

    const ts = new TomSelect(el, {
        render: {
            option: function (data, escape) {
                return '<div class="d-flex align-items-center">' +
                    (data.customProperties ? '<span class="me-2">' + data.customProperties + '</span>' : '') +
                    '<span>' + escape(data.text) + '</span>' +
                    '</div>';
            },
            item: function (data, escape) {
                return '<div class="d-flex align-items-center">' +
                    (data.customProperties ? '<span class="me-2">' + data.customProperties + '</span>' : '') +
                    '<span>' + escape(data.text) + '</span>' +
                    '</div>';
            }
        },
        onChange: function (value) {
            dotNetRef.invokeMethodAsync('OnValueChanged', value);
        }
    });

    instances[elementId] = ts;
}

export function destroyCountryPicker(elementId) {
    const ts = instances[elementId];
    if (ts) {
        ts.destroy();
        delete instances[elementId];
    }
}

export function setValue(elementId, value) {
    const ts = instances[elementId];
    if (ts) {
        ts.setValue(value, true);
    }
}
