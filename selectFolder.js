function selectFolder(path) {
    function addOption(parent, value, text) {
        var opt = document.createElement("option");
        opt.value = value;
        opt.text = typeof text == "undefined" ? value : text;
        parent.appendChild(opt);
    }
    function setSubFolders(select, defval) {
        select.innerHTML = "";
        var f = null;
        while (path.value != "" && f == null) {
            try {
                f = io.getFolder(path.value);
            } catch (e) {
                path.value = io.getParentFolderName(path.value);
            }
        }
        if (path.value == "") {
            ios.forEach(io.drives, function (drive) {
                addOption(select, drive.rootFolder);
            });
        } else {
            addOption(select, "..", "..");
            ios.forEach(f.subFolders, function (subFolder) {
                addOption(select, subFolder.name);
            });
        }
        if (defval) {
            select.value = defval;
        } else {
            select.selectedIndex = 0;
        }
        select.size = Math.min(Math.max(select.options.length, 2), 20);
    }
    var select = document.createElement("select");
    select.className = "select-folder-popup";
    select.style.position = "fixed";
    select.style.top = (path.offsetTop + path.clientHeight) + "px";
    select.style.minWidth = path.offsetWidth + "px";
    function changeFolder(event) {
        if (select.value == "..") {
            var defval = io.getFileName(path.value);
            path.value = io.getParentFolderName(path.value);
            setSubFolders(select, defval);
        } else {
            path.value = io.buildPath(path.value, select.value);
            setSubFolders(select);
        }
    }
    function closePopup() {
        var a = document.getElementsByClassName("select-folder-popup");
        for (var i = 0; i < a.length; i++) {
            a[i].parentNode.removeChild(a[i]);
        }
    }
    select.addEventListener("click", changeFolder);
    select.addEventListener("keydown", function (event) {
        if (event.keyCode == 13 || event.keyCode == 32 || event.keyCode == 39 && select.value != "..") {
            changeFolder(event);
            event.preventDefault();
            event.stopPropagation();
        } else if (event.keyCode == 37) {
            select.value = "..";
            changeFolder(event);
            event.preventDefault();
            event.stopPropagation();
        } else if (event.keyCode == 27 && !event.shiftKey && !event.ctrlKey && !event.altKey
            || event.keyCode == 38 && !event.shiftKey && event.ctrlKey && !event.altKey) {
            path.focus();
        }
    });
    select.addEventListener("blur", closePopup);
    setSubFolders(select);
    path.parentNode.insertBefore(select, path);
    select.focus();
}
selectFolder.shortcut = function (el) {
    el.addEventListener("keydown", function (event) {
        if (event.keyCode == 40 && !event.shiftKey && event.ctrlKey && !event.altKey) {
            selectFolder(el);
        }
    });
}
