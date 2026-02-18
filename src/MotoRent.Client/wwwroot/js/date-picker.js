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

    if (options?.minDate) {
        config.minDate = options.minDate;
    }
    if (options?.maxDate) {
        config.maxDate = options.maxDate;
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

export function setDate(element, date) {
    if (element?._flatpickr && date) {
        element._flatpickr.setDate(date, false);
    }
}

export function setMinDate(element, date) {
    if (element?._flatpickr) {
        element._flatpickr.set('minDate', date || null);
    }
}

export function setMaxDate(element, date) {
    if (element?._flatpickr) {
        element._flatpickr.set('maxDate', date || null);
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
