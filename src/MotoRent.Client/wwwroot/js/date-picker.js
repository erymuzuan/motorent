// Flatpickr Date Picker - Force Gregorian Calendar (2026 not 2569)
export function initDatePicker(element, dotNetRef, options) {
    if (!element || element._flatpickr) return;

    // Convert Buddhist year to Gregorian
    const toGregorian = (year) => year > 2500 ? year - 543 : year;

    // Function to fix Buddhist year display everywhere
    function fixYearDisplay(fp) {
        if (!fp || !fp.calendarContainer) return;

        // Fix internal currentYear state
        if (fp.currentYear > 2500) {
            fp.currentYear = toGregorian(fp.currentYear);
        }

        // Fix all year inputs (try all possible selectors)
        fp.calendarContainer.querySelectorAll('input.cur-year, input.numInput, input[aria-label="Year"]').forEach(input => {
            const val = parseInt(input.value);
            if (val > 2500) {
                input.value = toGregorian(val);
            }
        });

        // Fix yearElements array if exists
        if (fp.yearElements) {
            fp.yearElements.forEach(el => {
                const val = parseInt(el.value);
                if (val > 2500) {
                    el.value = toGregorian(val);
                }
            });
        }

        // Fix any select dropdowns for year
        fp.calendarContainer.querySelectorAll('select').forEach(select => {
            Array.from(select.options).forEach(opt => {
                const val = parseInt(opt.value);
                if (val > 2500) {
                    const gregorian = toGregorian(val);
                    opt.value = gregorian;
                    opt.textContent = opt.textContent.replace(String(val), String(gregorian));
                }
            });
        });
    }

    const config = {
        dateFormat: "d/m/Y",
        disableMobile: true,
        locale: {
            firstDayOfWeek: 1,
            weekdays: {
                shorthand: ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"],
                longhand: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
            },
            months: {
                shorthand: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
                longhand: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"]
            }
        },
        formatDate: function(date, format) {
            const day = String(date.getDate()).padStart(2, '0');
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const year = date.getFullYear(); // Always Gregorian
            return `${day}/${month}/${year}`;
        },
        onReady: function(selectedDates, dateStr, instance) {
            fixYearDisplay(instance);
        },
        onOpen: function(selectedDates, dateStr, instance) {
            fixYearDisplay(instance);
        },
        onMonthChange: function(selectedDates, dateStr, instance) {
            fixYearDisplay(instance);
        },
        onYearChange: function(selectedDates, dateStr, instance) {
            fixYearDisplay(instance);
        },
        onChange: function(selectedDates, dateStr, instance) {
            fixYearDisplay(instance);
            if (dotNetRef && selectedDates.length > 0) {
                const d = selectedDates[0];
                const formatted = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
                dotNetRef.invokeMethodAsync('OnDateChanged', formatted);
            }
        }
    };

    // Parse date strings explicitly to avoid locale issues
    function parseYMD(dateStr) {
        if (!dateStr) return null;
        const parts = dateStr.split('-');
        if (parts.length === 3) {
            const year = parseInt(parts[0], 10);
            const month = parseInt(parts[1], 10) - 1; // JS months are 0-based
            const day = parseInt(parts[2], 10);
            return new Date(year, month, day);
        }
        return null;
    }

    if (options?.defaultDate) {
        config.defaultDate = parseYMD(options.defaultDate);
    }
    if (options?.minDate) {
        config.minDate = parseYMD(options.minDate);
    }
    if (options?.maxDate) {
        config.maxDate = parseYMD(options.maxDate);
    }

    const fp = flatpickr(element, config);
    element._flatpickr = fp;

    // Override the internal year setter to always use Gregorian
    const originalJumpToDate = fp.jumpToDate;
    fp.jumpToDate = function(date, triggerChange) {
        originalJumpToDate.call(this, date, triggerChange);
        fixYearDisplay(fp);
    };

    // Initial fix with multiple attempts at different timings
    fixYearDisplay(fp);
    setTimeout(() => fixYearDisplay(fp), 0);
    setTimeout(() => fixYearDisplay(fp), 50);
    setTimeout(() => fixYearDisplay(fp), 100);
    setTimeout(() => fixYearDisplay(fp), 200);
    requestAnimationFrame(() => {
        fixYearDisplay(fp);
        requestAnimationFrame(() => fixYearDisplay(fp));
    });

    // Watch for any DOM changes and fix year display
    if (fp.calendarContainer) {
        const observer = new MutationObserver((mutations) => {
            // Debounce to prevent infinite loops
            if (!fp._fixingYear) {
                fp._fixingYear = true;
                requestAnimationFrame(() => {
                    fixYearDisplay(fp);
                    fp._fixingYear = false;
                });
            }
        });
        observer.observe(fp.calendarContainer, {
            subtree: true,
            childList: true,
            characterData: true,
            attributes: true
        });
        fp._yearObserver = observer;
    }

    return fp;
}

export function setDate(element, dateStr) {
    if (element?._flatpickr && dateStr) {
        // Parse yyyy-MM-dd format explicitly to avoid locale issues
        const parts = dateStr.split('-');
        if (parts.length === 3) {
            const year = parseInt(parts[0], 10);
            const month = parseInt(parts[1], 10) - 1; // JS months are 0-based
            const day = parseInt(parts[2], 10);
            const dateObj = new Date(year, month, day);
            element._flatpickr.setDate(dateObj, false);
        }
    }
}

export function setMinDate(element, dateStr) {
    if (element?._flatpickr) {
        let dateObj = null;
        if (dateStr) {
            const parts = dateStr.split('-');
            if (parts.length === 3) {
                dateObj = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            }
        }
        element._flatpickr.set('minDate', dateObj);
    }
}

export function setMaxDate(element, dateStr) {
    if (element?._flatpickr) {
        let dateObj = null;
        if (dateStr) {
            const parts = dateStr.split('-');
            if (parts.length === 3) {
                dateObj = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            }
        }
        element._flatpickr.set('maxDate', dateObj);
    }
}

export function destroy(element) {
    if (element?._flatpickr) {
        if (element._flatpickr._yearObserver) {
            element._flatpickr._yearObserver.disconnect();
        }
        element._flatpickr.destroy();
        delete element._flatpickr;
    }
}
