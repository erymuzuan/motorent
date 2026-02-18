// Flatpickr Date Picker - Force Gregorian Calendar (2026 not 2569)
export function initDatePicker(element, dotNetRef, options) {
    if (!element || element._flatpickr) return;

    // Function to fix Buddhist year to Gregorian year
    function fixYearDisplay(fp) {
        if (!fp || !fp.calendarContainer) return;

        // Fix year in month selector
        const yearInput = fp.calendarContainer.querySelector('.numInput.cur-year');
        if (yearInput) {
            const buddhistYear = parseInt(yearInput.value);
            if (buddhistYear > 2500) {
                yearInput.value = buddhistYear - 543;
            }
        }

        // Fix year dropdown if exists
        const yearDropdown = fp.calendarContainer.querySelector('.flatpickr-monthDropdown-months');
        if (yearDropdown) {
            const options = yearDropdown.querySelectorAll('option');
            options.forEach(opt => {
                const year = parseInt(opt.value);
                if (year > 2500) {
                    opt.value = year - 543;
                    opt.textContent = opt.textContent.replace(year, year - 543);
                }
            });
        }
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

    // Initial fix
    setTimeout(() => fixYearDisplay(fp), 100);

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
        element._flatpickr.destroy();
        delete element._flatpickr;
    }
}
