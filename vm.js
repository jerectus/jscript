function Vm(form, opt) {
    var self = this;
    function forEach(list, fn) {
        if (typeof list.length == "number") {
            for (var i = 0; i < list.length; i++) {
                fn(list[i], i, list);
            }
        } else {
            for (var key in list) {
                fn(list[key], key, list);
            }
        }
    }
    function propertyFor(el) {
        function getRadioValue(el) {
            var a = el.form[el.name];
            if ("length" in a) {
                for (var i = 0; i < a.length; i++) {
                    if (a[i].checked) return a[i].value;
                }
                return "";
            } else {
                return a.checked ? a.value : "";
            }
        }
        function setRadioValue(el, value) {
            var a = el.form[el.name];
            if ("length" in a) {
                for (var i = 0; i < a.length; i++) {
                    a[i].checked = a[i].value == value;
                }
            } else {
                a.checked = a.value == value;
            }
        }
        if (el.type == "text" || el.type == "textarea") {
            return {
                get: function () {
                    return el.value;
                },
                set: function (value) {
                    el.value = value;
                },
                enumerable: true
            };
        } else if (el.type == "checkbox") {
            return {
                get: function () {
                    return el.checked;
                },
                set: function (value) {
                    el.checked = typeof value == "String" ? (el.value == value) : !!value;
                },
                enumerable: true
            };
        } else if (el.type == "radio") {
            return {
                get: function () {
                    return getRadioValue(el);
                },
                set: function (value) {
                    setRadioValue(el, value);
                },
                enumerable: true
            };
        } else {
            return {
                get: function () {
                    return el.textContent;
                },
                set: function (value) {
                    el.textContent = value;
                },
                enumerable: true
            };
        }
    }
    forEach(form.elements, function (el) {
        if (!(el.name in self)) {
            if (el.type != "button") {
                Object.defineProperty(self, el.name, propertyFor(el));
            }
            if (el.type == "radio") {
                var a = el.form[el.name];
                el = ("length" in a ? a : [a]);
            }
            Object.defineProperty(self, el.name + "$", { value: el, enumerable: false});
        }
    });
    forEach(form.querySelectorAll("[vm]"), function (el) {
        Object.defineProperty(self, el.getAttribute("vm"), propertyFor(el));
    });
    if (opt) {
        forEach(opt, function (value, key) {
            if (key.match(/^\w+$/)) {
                self[key] = value;
            } else if (key.match(/^on\s+(\w+)\s+(\w+)$/)) {
                var name = RegExp.$1, eventName = RegExp.$2;
                var el = self[name + "$"];
                if (el && typeof value == "function") {
                    el.addEventListener(eventName, function (event) {
                        value.call(self, event, el);
                    });
                }
            }
        });
    }
}

// new Vm(form)